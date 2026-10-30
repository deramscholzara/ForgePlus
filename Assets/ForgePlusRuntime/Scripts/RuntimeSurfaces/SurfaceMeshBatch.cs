using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace RuntimeCore.Entities.Geometry
{
    // Writes new surfaces' meshes: their corners, triangles and texture coordinates, with the normals and tangents worked out
    // from them (as Mesh.RecalculateNormals and RecalculateTangents do). While a batch is open (as a level is built), each
    // mesh waits for it to close, and they're all written together by a Burst job (writing thousands of tiny meshes one
    // Mesh call at a time costs far more than the work itself); otherwise each is written as it's added.
    [AutoStaticsCleanup]
    public static partial class SurfaceMeshBatch
    {
        private const int MeshesPerJobBatch = 64;

        // Fewer than this are written on the main thread, as handing them to worker threads would cost more
        private const int MinimumScheduledMeshes = 64;

        // Floats per vertex: position (3), normal (3), tangent (4), texture UV (2), the ambient delta UV (2), and a layered
        // transparent side's outer layer's UV (2)
        private const int VertexFloats = 14;
        private const int LayeredVertexFloats = 16;

        private struct MeshRange
        {
            public int VertexStart;
            public int VertexCount;
            public int TriangleStart;
            public int TriangleIndexCount;
            public float AmbientDelta;
            public bool IsLayered;

            // Rather than worked out from its triangles (a media surface's, which are drawn from both sides)
            public bool NormalsFaceUp;
        }

        // Not readonly, so AutoStaticsCleanup resets it each Play session
        private static int openBatches;

        private static readonly List<Mesh> meshes = new List<Mesh>();
        private static readonly List<MeshRange> ranges = new List<MeshRange>();
        private static readonly List<Vector3> positions = new List<Vector3>();
        private static readonly List<Vector2> textureUVs = new List<Vector2>();

        // Parallel to positions (zeros for meshes with no outer layer)
        private static readonly List<Vector2> outerLayerUVs = new List<Vector2>();
        private static readonly List<int> triangles = new List<int>();
        private static readonly List<Action> afterWriting = new List<Action>();

        public readonly struct Batch : IDisposable
        {
            public void Dispose()
            {
                Close();
            }
        }

        // Meshes added until it's disposed are written together then (batches can be nested, and the outermost writes them)
        public static Batch Open()
        {
            openBatches++;
            return new Batch();
        }

        // Its triangles index its positions, and outerLayerUVs (null for none) are a layered transparent side's outer layer's
        public static void Add(
            Mesh mesh,
            Vector3[] meshPositions,
            int[] meshTriangles,
            Vector2[] meshTextureUVs,
            Vector2[] meshOuterLayerUVs,
            float ambientDelta,
            bool normalsFaceUp)
        {
            meshes.Add(mesh);
            ranges.Add(new MeshRange
            {
                VertexStart = positions.Count,
                VertexCount = meshPositions.Length,
                TriangleStart = triangles.Count,
                TriangleIndexCount = meshTriangles.Length,
                AmbientDelta = ambientDelta,
                IsLayered = meshOuterLayerUVs != null,
                NormalsFaceUp = normalsFaceUp,
            });

            positions.AddRange(meshPositions);
            textureUVs.AddRange(meshTextureUVs);

            if (meshOuterLayerUVs != null)
            {
                outerLayerUVs.AddRange(meshOuterLayerUVs);
            }
            else
            {
                for (var i = 0; i < meshPositions.Length; i++)
                {
                    outerLayerUVs.Add(Vector2.zero);
                }
            }

            triangles.AddRange(meshTriangles);

            if (openBatches == 0)
            {
                Write();
            }
        }

        // Once the meshes added so far are written (now, if no batch is open), for what reads them (such as a collider)
        public static void AfterWriting(Action action)
        {
            if (openBatches == 0)
            {
                action();
            }
            else
            {
                afterWriting.Add(action);
            }
        }

        private static void Close()
        {
            if (openBatches <= 0)
            {
                throw new InvalidOperationException("Closed a surface mesh batch that wasn't open.");
            }

            openBatches--;

            if (openBatches == 0)
            {
                Write();
            }
        }

        private static void Write()
        {
            if (meshes.Count > 0)
            {
                WriteMeshes();
            }

            if (afterWriting.Count > 0)
            {
                // What runs may add meshes of its own (written as it adds them, as no batch is open)
                var actions = afterWriting.ToArray();
                afterWriting.Clear();

                foreach (var action in actions)
                {
                    action();
                }
            }
        }

        private static void WriteMeshes()
        {
            var meshData = Mesh.AllocateWritableMeshData(meshes.Count);

            var jobRanges = ToNativeArray(ranges);
            var jobPositions = ToNativeArray(positions);
            var jobTextureUVs = ToNativeArray(textureUVs);
            var jobOuterLayerUVs = ToNativeArray(outerLayerUVs);
            var jobTriangles = ToNativeArray(triangles);
            var layout = new NativeArray<VertexAttributeDescriptor>(
                new[]
                {
                    new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
                    new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
                    new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 4),
                    new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
                    new VertexAttributeDescriptor(VertexAttribute.TexCoord4, VertexAttributeFormat.Float32, 2),
                },
                Allocator.TempJob);
            var layeredLayout = new NativeArray<VertexAttributeDescriptor>(
                new[]
                {
                    new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
                    new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
                    new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 4),
                    new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
                    new VertexAttributeDescriptor(VertexAttribute.TexCoord1, VertexAttributeFormat.Float32, 2),
                    new VertexAttributeDescriptor(VertexAttribute.TexCoord4, VertexAttributeFormat.Float32, 2),
                },
                Allocator.TempJob);

            var meshBounds = new NativeArray<Bounds>(meshes.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            try
            {
                var job = new WriteMeshesJob
                {
                    Meshes = meshData,
                    Ranges = jobRanges,
                    Positions = jobPositions,
                    TextureUVs = jobTextureUVs,
                    OuterLayerUVs = jobOuterLayerUVs,
                    Triangles = jobTriangles,
                    Layout = layout,
                    LayeredLayout = layeredLayout,
                    MeshBounds = meshBounds,
                };

                if (meshes.Count < MinimumScheduledMeshes)
                {
                    job.Run(meshes.Count);
                }
                else
                {
                    job.Schedule(meshes.Count, MeshesPerJobBatch).Complete();
                }

                Mesh.ApplyAndDisposeWritableMeshData(meshData, meshes, MeshUpdateFlags.DontRecalculateBounds);

                // Applying mesh data gives the meshes no bounds of their own (only their submeshes')
                for (var i = 0; i < meshes.Count; i++)
                {
                    meshes[i].bounds = meshBounds[i];
                }
            }
            catch
            {
                meshData.Dispose();
                throw;
            }
            finally
            {
                jobRanges.Dispose();
                jobPositions.Dispose();
                jobTextureUVs.Dispose();
                jobOuterLayerUVs.Dispose();
                jobTriangles.Dispose();
                layout.Dispose();
                layeredLayout.Dispose();
                meshBounds.Dispose();

                meshes.Clear();
                ranges.Clear();
                positions.Clear();
                textureUVs.Clear();
                outerLayerUVs.Clear();
                triangles.Clear();
            }
        }

        private static NativeArray<T> ToNativeArray<T>(List<T> list) where T : struct
        {
            var array = new NativeArray<T>(list.Count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            for (var i = 0; i < list.Count; i++)
            {
                array[i] = list[i];
            }

            return array;
        }

        [BurstCompile]
        private struct WriteMeshesJob : IJobParallelFor
        {
            public Mesh.MeshDataArray Meshes;

            [ReadOnly] public NativeArray<MeshRange> Ranges;
            [ReadOnly] public NativeArray<Vector3> Positions;
            [ReadOnly] public NativeArray<Vector2> TextureUVs;
            [ReadOnly] public NativeArray<Vector2> OuterLayerUVs;
            [ReadOnly] public NativeArray<int> Triangles;
            [ReadOnly] public NativeArray<VertexAttributeDescriptor> Layout;
            [ReadOnly] public NativeArray<VertexAttributeDescriptor> LayeredLayout;

            [WriteOnly] public NativeArray<Bounds> MeshBounds;

            public void Execute(int meshIndex)
            {
                var range = Ranges[meshIndex];
                var mesh = Meshes[meshIndex];

                mesh.SetVertexBufferParams(range.VertexCount, range.IsLayered ? LayeredLayout : Layout);
                mesh.SetIndexBufferParams(range.TriangleIndexCount, IndexFormat.UInt16);

                var normals = new NativeArray<float3>(range.VertexCount, Allocator.Temp);
                var uDirections = new NativeArray<float3>(range.VertexCount, Allocator.Temp);
                var vDirections = new NativeArray<float3>(range.VertexCount, Allocator.Temp);

                for (var i = 0; i + 2 < range.TriangleIndexCount; i += 3)
                {
                    var a = Triangles[range.TriangleStart + i];
                    var b = Triangles[range.TriangleStart + i + 1];
                    var c = Triangles[range.TriangleStart + i + 2];

                    float3 positionA = Positions[range.VertexStart + a];
                    var edgeB = (float3)Positions[range.VertexStart + b] - positionA;
                    var edgeC = (float3)Positions[range.VertexStart + c] - positionA;

                    if (!range.NormalsFaceUp)
                    {
                        var faceNormal = math.normalizesafe(math.cross(edgeB, edgeC));
                        normals[a] += faceNormal;
                        normals[b] += faceNormal;
                        normals[c] += faceNormal;
                    }

                    // The directions the texture's U and V run across the triangle
                    float2 uvA = TextureUVs[range.VertexStart + a];
                    var uvEdgeB = (float2)TextureUVs[range.VertexStart + b] - uvA;
                    var uvEdgeC = (float2)TextureUVs[range.VertexStart + c] - uvA;
                    var determinant = uvEdgeB.x * uvEdgeC.y - uvEdgeC.x * uvEdgeB.y;
                    if (math.abs(determinant) > 1e-12f)
                    {
                        var inverse = 1f / determinant;
                        var uDirection = (edgeB * uvEdgeC.y - edgeC * uvEdgeB.y) * inverse;
                        var vDirection = (edgeC * uvEdgeB.x - edgeB * uvEdgeC.x) * inverse;

                        uDirections[a] += uDirection;
                        uDirections[b] += uDirection;
                        uDirections[c] += uDirection;
                        vDirections[a] += vDirection;
                        vDirections[b] += vDirection;
                        vDirections[c] += vDirection;
                    }
                }

                var vertices = mesh.GetVertexData<float>();
                var vertexFloats = range.IsLayered ? LayeredVertexFloats : VertexFloats;

                var boundsMin = new float3(float.MaxValue);
                var boundsMax = new float3(float.MinValue);

                for (var v = 0; v < range.VertexCount; v++)
                {
                    var normal = range.NormalsFaceUp ? new float3(0f, 1f, 0f) : math.normalizesafe(normals[v]);

                    // The texture's U direction, made perpendicular to the normal, and which way V runs from it. A vertex
                    // with neither (only on triangles with no area, or no texture) gets the axis furthest from the normal,
                    // as RecalculateTangents gives it.
                    var uDirection = uDirections[v];
                    var tangent = math.normalizesafe(uDirection - normal * math.dot(normal, uDirection));
                    if (math.lengthsq(tangent) == 0f)
                    {
                        var axisAlignment = math.abs(normal);
                        var axis = axisAlignment.x <= axisAlignment.y && axisAlignment.x <= axisAlignment.z ? new float3(1f, 0f, 0f) :
                                   axisAlignment.y <= axisAlignment.z ? new float3(0f, 1f, 0f) :
                                   new float3(0f, 0f, 1f);
                        tangent = math.normalizesafe(axis - normal * math.dot(normal, axis), axis);
                    }
                    var handedness = math.dot(math.cross(normal, uDirection), vDirections[v]) > 0f ? 1f : -1f;

                    var position = Positions[range.VertexStart + v];
                    boundsMin = math.min(boundsMin, position);
                    boundsMax = math.max(boundsMax, position);
                    var textureUV = TextureUVs[range.VertexStart + v];

                    var o = v * vertexFloats;
                    vertices[o++] = position.x;
                    vertices[o++] = position.y;
                    vertices[o++] = position.z;
                    vertices[o++] = normal.x;
                    vertices[o++] = normal.y;
                    vertices[o++] = normal.z;
                    vertices[o++] = tangent.x;
                    vertices[o++] = tangent.y;
                    vertices[o++] = tangent.z;
                    vertices[o++] = handedness;
                    vertices[o++] = textureUV.x;
                    vertices[o++] = textureUV.y;

                    if (range.IsLayered)
                    {
                        var outerLayerUV = OuterLayerUVs[range.VertexStart + v];
                        vertices[o++] = outerLayerUV.x;
                        vertices[o++] = outerLayerUV.y;
                    }

                    vertices[o++] = range.AmbientDelta;
                    vertices[o] = 0f;
                }

                var indices = mesh.GetIndexData<ushort>();
                for (var i = 0; i < range.TriangleIndexCount; i++)
                {
                    indices[i] = (ushort)Triangles[range.TriangleStart + i];
                }

                var bounds = range.VertexCount > 0 ?
                             new Bounds((boundsMin + boundsMax) * 0.5f, boundsMax - boundsMin) :
                             new Bounds();
                MeshBounds[meshIndex] = bounds;

                mesh.subMeshCount = 1;
                mesh.SetSubMesh(0,
                                new SubMeshDescriptor(0, range.TriangleIndexCount)
                                {
                                    bounds = bounds,
                                    vertexCount = range.VertexCount,
                                },
                                MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds);
            }
        }
    }
}
