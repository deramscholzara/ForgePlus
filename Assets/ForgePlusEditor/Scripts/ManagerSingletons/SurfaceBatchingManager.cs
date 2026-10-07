using System;
using System.Collections.Generic;
using System.Linq;
using AlephOne;
using ForgePlus.DataFileIO;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using RuntimeCore.Materials;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ForgePlus.ApplicationGeneral
{
    [AutoStaticsCleanup]
    public partial class SurfaceBatchingManager : SingletonMonoBehaviour<SurfaceBatchingManager>
    {
        public struct BatchKey : IEquatable<BatchKey>
        {
            // Note: Using Material instead of just ShapeDescriptor here,
            // as it accounts for unique shaders used for the same texture.
            // (such as a media texture applied to a wall vs actually applied to media)
            public Material _sourceMaterial;

            public Material SourceMaterial
            {
                get => _sourceMaterial;
                set { _sourceMaterial = SeparateShaders ? value : null; }
            }

            public Material _layeredTransparentSideSourceMaterial;

            public Material LayeredTransparentSideSourceMaterial
            {
                get => _layeredTransparentSideSourceMaterial;
                set { _layeredTransparentSideSourceMaterial = SeparateShaders ? value : null; }
            }

            private LevelEntity_Light _sourceLight;

            public LevelEntity_Light SourceLight
            {
                get => _sourceLight;
                set { _sourceLight = SeparateLights ? value : null; }
            }

            private LevelEntity_Light _layeredTransparentSideSourceLight;

            public LevelEntity_Light LayeredTransparentSideSourceLight
            {
                get => _layeredTransparentSideSourceLight;
                set { _layeredTransparentSideSourceLight = SeparateLights ? value : null; }
            }

            public LevelEntity_Media SourceMedia;

#if USE_TEXTURE_ARRAYS
            // Note: textures are always separated by material (shader) when not using Texture2DArrays
            private ushort _sourceShapeDescriptor;

            public ushort SourceShapeDescriptor
            {
                get => _sourceShapeDescriptor;
                set { _sourceShapeDescriptor = SeparateTextures ? value : cstypes.UNONE; }
            }

            private ushort _layeredTransparentSideShapeDescriptor;
            public ushort LayeredTransparentSideShapeDescriptor
            {
                get => _layeredTransparentSideShapeDescriptor;
                set { _layeredTransparentSideShapeDescriptor = SeparateTextures ? value : cstypes.UNONE; }
            }
#endif

            // Batches are dictionary keys, so these avoid the reflection-based default struct equality
            public bool Equals(BatchKey other)
            {
                return _sourceMaterial == other._sourceMaterial &&
                       _layeredTransparentSideSourceMaterial == other._layeredTransparentSideSourceMaterial &&
                       _sourceLight == other._sourceLight &&
                       _layeredTransparentSideSourceLight == other._layeredTransparentSideSourceLight &&
#if USE_TEXTURE_ARRAYS
                       _sourceShapeDescriptor == other._sourceShapeDescriptor &&
                       _layeredTransparentSideShapeDescriptor == other._layeredTransparentSideShapeDescriptor &&
#endif
                       SourceMedia == other.SourceMedia;
            }

            public override bool Equals(object obj)
            {
                return obj is BatchKey other && Equals(other);
            }

            public override int GetHashCode()
            {
#if USE_TEXTURE_ARRAYS
                return HashCode.Combine(_sourceMaterial, _layeredTransparentSideSourceMaterial, _sourceLight, _layeredTransparentSideSourceLight, SourceMedia, _sourceShapeDescriptor, _layeredTransparentSideShapeDescriptor);
#else
                return HashCode.Combine(_sourceMaterial, _layeredTransparentSideSourceMaterial, _sourceLight, _layeredTransparentSideSourceLight, SourceMedia);
#endif
            }

            public static bool operator ==(BatchKey a, BatchKey b)
            {
                return a.Equals(b);
            }

            public static bool operator !=(BatchKey a, BatchKey b)
            {
                return !a.Equals(b);
            }
        }

        private class SurfaceBatch
        {
            public class Surface
            {
                public RuntimeSurfaceGeometry SurfaceGeometry { get; private set; }

                public Surface(RuntimeSurfaceGeometry surfaceGeometry)
                {
                    SurfaceGeometry = surfaceGeometry;
                }

                public Mesh MakeStaticAndGetMesh()
                {
                    SurfaceGeometry.SurfaceRenderer.enabled = false;

                    return SurfaceGeometry.SurfaceMesh;
                }

                public void MakeDynamic()
                {
                    SurfaceGeometry.SurfaceRenderer.enabled = true;
                }
            }

            private Material[] sourceMaterials;
            private List<Surface> surfaces;
            private Dictionary<RuntimeSurfaceGeometry, Surface> surfacesByGeometry;
            private GameObject mergeObject;
            private Mesh mergedMesh;

            private LevelEntity_Media media;

            public bool IsMerged
            {
                get { return mergeObject != null; }
            }

            public SurfaceBatch(Material[] sourceMaterials, LevelEntity_Media media)
            {
                this.sourceMaterials = sourceMaterials;
                surfaces = new List<Surface>();
                surfacesByGeometry = new Dictionary<RuntimeSurfaceGeometry, Surface>();
                mergeObject = null;
                this.media = media;
            }

            public void AddSurface(RuntimeSurfaceGeometry surfaceGeometry, bool deleteOriginalObjects = false)
            {
                if (surfacesByGeometry.ContainsKey(surfaceGeometry))
                {
                    Debug.LogError("Attempted adding surface to batch multiple times, this attempt will be ignored.");
                    return;
                }

                var isMerged = mergeObject != null;
                if (isMerged)
                {
                    Unmerge();
                }

                var surface = new Surface(surfaceGeometry);
                surfaces.Add(surface);
                surfacesByGeometry.Add(surfaceGeometry, surface);

                if (isMerged)
                {
                    Merge(deleteOriginalObjects);
                }
                else if (media != null)
                {
                    media.SubscribeSurface(surfaceGeometry.transform);
                }
            }

            // Returns False if this StaticBatch is now empty, true otherwise
            public bool RemoveSurface(RuntimeSurfaceGeometry surfaceGeometry, bool deleteOriginalObjects = false)
            {
                if (!surfacesByGeometry.TryGetValue(surfaceGeometry, out var surface))
                {
                    ////Debug.LogError("Attempted removing surface from batch that did not contain it, this attempt will be ignored.");
                    return surfaces.Count > 0;
                }

                var isMerged = mergeObject != null;
                if (isMerged)
                {
                    Unmerge();
                }

                surfaces.Remove(surface);
                surfacesByGeometry.Remove(surfaceGeometry);

                if (isMerged)
                {
                    // Re-Merge if it was merged prior to this removal
                    Merge(deleteOriginalObjects);
                }
                else if (media != null)
                {
                    media.UnsubscribeSurface(surfaceGeometry.transform);
                }

                return surfaces.Count > 0;
            }

            public void Merge(bool deleteOriginalObjects = false)
            {
                if (!BatchingEnabled)
                {
                    return;
                }

                if (mergeObject)
                {
                    // Already merged, so exit
                    return;
                }

                var objectDescriptiveName = $" - {String.Join(" - ", sourceMaterials.Select(entry => entry.name).ToArray())}";

                mergeObject = new GameObject($"Batched Surfaces{objectDescriptiveName}");
                mergeObject.transform.SetParent(LevelEntity_Level.Instance.transform);

                if (Instance.UseUnityStaticBatching && media == null)
                {
                    var objectsToBatch = surfaces.Select(surface => surface.SurfaceGeometry.gameObject).ToArray();
                    StaticBatchingUtility.Combine(objectsToBatch, mergeObject);
                }
                else
                {
                    mergedMesh = BuildMergedMesh();
                    mergedMesh.name = "Batched Mesh" + objectDescriptiveName;

                    mergeObject.AddComponent<MeshFilter>().sharedMesh = mergedMesh;

                    var mergedRenderer = mergeObject.AddComponent<MeshRenderer>();
                    mergedRenderer.sharedMaterials = sourceMaterials;
                    RuntimeSurfaceGeometry.ConfigureRenderer(mergedRenderer);

                    if (deleteOriginalObjects)
                    {
                        foreach (var surface in surfaces)
                        {
                            DestroyImmediate(surface.SurfaceGeometry.gameObject);
                        }
                    }

                    if (media != null)
                    {
                        media.SubscribeSurface(mergeObject.transform);
                    }
                }
            }

            // Appends each surface's mesh data to the shared scratch buffers, then uploads them to a new mesh.
            // Normals and tangents are carried over from the surfaces rather than recalculated,
            // as no vertices are shared between surfaces, so recalculating would only reproduce them.
            private Mesh BuildMergedMesh()
            {
                var hasLayeredTransparentSide = sourceMaterials.Length > 1;
                var defaultEffects = RuntimeSurfaceGeometryModule_Base.DefaultTransferModeEffects;
                var defaultTransferModeEffects = new Vector4(defaultEffects.x, defaultEffects.y, defaultEffects.x, defaultEffects.y);

                foreach (var surface in surfaces)
                {
                    var dynamicMesh = surface.MakeStaticAndGetMesh();
                    var vertexOffset = MergedVertices.Count;
                    var vertexCount = dynamicMesh.vertexCount;

                    dynamicMesh.GetTriangles(SurfaceTriangles, submesh: 0);
                    foreach (var triangleIndex in SurfaceTriangles)
                    {
                        MergedTriangles.Add(triangleIndex + vertexOffset);
                    }

                    dynamicMesh.GetVertices(SurfaceVertices);
                    dynamicMesh.GetNormals(SurfaceNormals);
                    dynamicMesh.GetTangents(SurfaceTangents);

                    if (media != null)
                    {
                        // Media surfaces stay in local space, as the media moves the merged object
                        MergedVertices.AddRange(SurfaceVertices);
                        MergedNormals.AddRange(SurfaceNormals);
                        MergedTangents.AddRange(SurfaceTangents);
                    }
                    else
                    {
                        var localToWorld = surface.SurfaceGeometry.transform.localToWorldMatrix;

                        foreach (var position in SurfaceVertices)
                        {
                            MergedVertices.Add(localToWorld.MultiplyPoint3x4(position));
                        }

                        foreach (var normal in SurfaceNormals)
                        {
                            MergedNormals.Add(localToWorld.MultiplyVector(normal).normalized);
                        }

                        foreach (var tangent in SurfaceTangents)
                        {
                            var direction = localToWorld.MultiplyVector(tangent).normalized;
                            MergedTangents.Add(new Vector4(direction.x, direction.y, direction.z, tangent.w));
                        }
                    }

                    dynamicMesh.GetUVs(0, SurfaceUVs);
                    MergedUV0s.AddRange(SurfaceUVs);

                    if (hasLayeredTransparentSide)
                    {
                        dynamicMesh.GetUVs(1, SurfaceUVs);
                        MergedUV1s.AddRange(SurfaceUVs);

                        dynamicMesh.GetUVs(2, SurfaceUVs);
                        MergedUV2s.AddRange(SurfaceUVs);
                    }

                    // Transfer mode effects, which media surfaces don't have
                    dynamicMesh.GetUVs(3, SurfaceUVs);
                    if (SurfaceUVs.Count == vertexCount)
                    {
                        MergedUV3s.AddRange(SurfaceUVs);
                    }
                    else
                    {
                        for (var i = 0; i < vertexCount; i++)
                        {
                            MergedUV3s.Add(defaultTransferModeEffects);
                        }
                    }

                    dynamicMesh.GetColors(SurfaceColors);
                    MergedColors.AddRange(SurfaceColors);
                }

                var builtMesh = new Mesh();

                // Large batches can exceed 16-bit indices
                builtMesh.indexFormat = MergedVertices.Count > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;

                builtMesh.SetVertices(MergedVertices);
                builtMesh.SetNormals(MergedNormals);
                builtMesh.SetTangents(MergedTangents);
                builtMesh.SetTriangles(MergedTriangles, submesh: 0);
                builtMesh.SetUVs(channel: 0, uvs: MergedUV0s);

                if (hasLayeredTransparentSide)
                {
                    builtMesh.SetUVs(channel: 1, uvs: MergedUV1s);
                    builtMesh.SetUVs(channel: 2, uvs: MergedUV2s);
                }

                builtMesh.SetUVs(channel: 3, uvs: MergedUV3s);
                builtMesh.SetColors(MergedColors);

                MergedVertices.Clear();
                MergedNormals.Clear();
                MergedTangents.Clear();
                MergedTriangles.Clear();
                MergedUV0s.Clear();
                MergedUV1s.Clear();
                MergedUV2s.Clear();
                MergedUV3s.Clear();
                MergedColors.Clear();

                return builtMesh;
            }

            public void Unmerge()
            {
                if (!mergeObject)
                {
                    // Not merged, so exit
                    return;
                }

                if (media != null)
                {
                    media.UnsubscribeSurface(mergeObject.transform);
                }

                Destroy(mergeObject);
                mergeObject = null;

                DestroyMergedMesh();

                foreach (var surface in surfaces)
                {
                    surface.MakeDynamic();

                    if (media != null)
                    {
                        media.SubscribeSurface(surface.SurfaceGeometry.transform);
                    }
                }
            }

            // The merge object is destroyed along with the level, but its mesh is not
            public void DestroyMergedMesh()
            {
                if (mergedMesh)
                {
                    Destroy(mergedMesh);
                }

                mergedMesh = null;
            }
        }

        // Scratch buffers reused by every merge, to avoid per-surface and per-merge allocations
        private static readonly List<int> SurfaceTriangles = new List<int>();
        private static readonly List<Vector3> SurfaceVertices = new List<Vector3>();
        private static readonly List<Vector3> SurfaceNormals = new List<Vector3>();
        private static readonly List<Vector4> SurfaceTangents = new List<Vector4>();
        private static readonly List<Vector4> SurfaceUVs = new List<Vector4>();
        private static readonly List<Color> SurfaceColors = new List<Color>();

        private static readonly List<int> MergedTriangles = new List<int>();
        private static readonly List<Vector3> MergedVertices = new List<Vector3>();
        private static readonly List<Vector3> MergedNormals = new List<Vector3>();
        private static readonly List<Vector4> MergedTangents = new List<Vector4>();
        private static readonly List<Vector4> MergedUV0s = new List<Vector4>();
        private static readonly List<Vector4> MergedUV1s = new List<Vector4>();
        private static readonly List<Vector4> MergedUV2s = new List<Vector4>();
        private static readonly List<Vector4> MergedUV3s = new List<Vector4>();
        private static readonly List<Color> MergedColors = new List<Color>();

        public static bool SeparateLights;
        public static bool SeparateTextures;
        public static bool SeparateShaders;

        [Tooltip("Geometry is combined using Unity's Static Batching utilities instead of the usually-better Forge+ method.")]
        public bool UseUnityStaticBatching = false;

        [Tooltip("Batching combines geometry with identical properties (lights, textures, shaders).")] [SerializeField]
        private bool batchingEnabled = true;

        [Tooltip("For batching purposes, determines whether lights should be treated as distinct (enabled) or identical (disabled).")] [SerializeField]
        private bool separateLights = false;
        // TODO: !!! - implement lights texture so that this being false by default makes sense with or without texture arrays.
        // TODO: !!! - UV0.z is the main texture index, so UV1.z should be the layered texture index
        // TODO: !!! - UV0.w and UV1.w should represent the lights for these layers, respectively.

#if USE_TEXTURE_ARRAYS
        [Tooltip("For batching purposes, determines whether textures (bitmaps) should be treated as distinct (enabled) or identical (disabled).")] [SerializeField]
        private bool separateTextures = false;
#endif

        [Tooltip("For batching purposes, determines whether shaders should be treated as distinct (enabled) or identical (disabled)." +
                 "\nDistinct shaders include: Opaque, Transparent, Media, Landscape, and Unassigned.")]
        [SerializeField]
        private bool separateShaders = true;

        [SerializeField] private bool deleteOriginalObjects = false;

        private readonly Dictionary<BatchKey, Material[]> SurfaceMaterials = new Dictionary<BatchKey, Material[]>();
        private readonly Dictionary<BatchKey, SurfaceBatch> StaticBatches = new Dictionary<BatchKey, SurfaceBatch>();

        public static bool BatchingEnabled => Instance.batchingEnabled;

        public Material[] GetUniqueMaterials(BatchKey key)
        {
            var materialCount = key.LayeredTransparentSideSourceMaterial ? 2 : 1;

            if (SurfaceMaterials.ContainsKey(key))
            {
                if (separateShaders)
                {
                    return SurfaceMaterials[key];
                }

                return materialCount == 2
                    ? new Material[]
                    {
                        SurfaceMaterials[key][0],
                        SurfaceMaterials[key][0]
                    }
                    : SurfaceMaterials[key];
            }

            Material[] uniqueMaterials = new Material[materialCount];

            uniqueMaterials[0] = new Material(key.SourceMaterial);
#if USE_TEXTURE_ARRAYS
            MaterialGeneration_Geometry.SubscribeUniqueMaterial(
                uniqueMaterials[0],
                key.SourceMaterial);
#endif

            if (key.SourceLight != null)
            {
                uniqueMaterials[0].name += $" Light({key.SourceLight.NativeIndex})";
            }

            if (key.LayeredTransparentSideSourceMaterial)
            {
                uniqueMaterials[1] = new Material(key.LayeredTransparentSideSourceMaterial);
#if USE_TEXTURE_ARRAYS
                MaterialGeneration_Geometry.SubscribeUniqueMaterial(
                    uniqueMaterials[1],
                    key.LayeredTransparentSideSourceMaterial);
#endif

                if (key.LayeredTransparentSideSourceLight != null)
                {
                    uniqueMaterials[1].name += $" Light({key.LayeredTransparentSideSourceLight.NativeIndex})";
                }
            }

            SurfaceMaterials[key] = uniqueMaterials;

            key.SourceMedia?.SubscribeMaterial(uniqueMaterials[0]);

            return uniqueMaterials;
        }

        public void AddToBatches(BatchKey key, RuntimeSurfaceGeometry surfaceGeometry)
        {
            if (!StaticBatches.ContainsKey(key))
            {
                StaticBatches[key] = new SurfaceBatch(GetUniqueMaterials(key), key.SourceMedia);
            }

            StaticBatches[key].AddSurface(surfaceGeometry);
        }

        public void RemoveFromBatches(BatchKey key, RuntimeSurfaceGeometry surfaceGeometry)
        {
            if (GetBatchExists(key))
            {
                var occupied = StaticBatches[key].RemoveSurface(surfaceGeometry);

                if (!occupied)
                {
#if USE_TEXTURE_ARRAYS
                    var uniqueMaterials = SurfaceMaterials[key];
                    foreach (var uniqueMaterial in uniqueMaterials)
                    {
                        MaterialGeneration_Geometry.UnsubscribeUniqueMaterial(uniqueMaterial);
                    }
#endif

                    StaticBatches.Remove(key);
                }
            }
        }

        public bool GetBatchIsMerged(BatchKey key)
        {
            return GetBatchExists(key) && StaticBatches[key].IsMerged;
        }

        public void MergeAllBatches()
        {
            foreach (var key in StaticBatches.Keys)
            {
                MergeBatch(key);
            }

            if (deleteOriginalObjects)
            {
                foreach (var line in LevelEntity_Level.Instance.Lines.Values)
                {
                    if (line.GetComponentsInChildren<Renderer>().Length == 0)
                    {
                        Destroy(line.gameObject);
                    }
                }

                foreach (var polygon in LevelEntity_Level.Instance.Polygons.Values)
                {
                    if (polygon.GetComponentsInChildren<Renderer>().Length == 0)
                    {
                        Destroy(polygon.gameObject);
                    }
                }
            }
        }

        public void UnmergeAllBatches()
        {
            foreach (var key in StaticBatches.Keys)
            {
                UnmergeBatch(key);
            }
        }

        public void UnmergeBatch(BatchKey key)
        {
            if (GetBatchExists(key))
            {
                StaticBatches[key].Unmerge();
            }
        }

        // Note: this is private, as it's best to just use MergeAllBatches
        //       there's no real advantage to being precise with merging,
        //       since MergeAllBatched only merges unmerged ones.
        private void MergeBatch(BatchKey key)
        {
            StaticBatches[key].Merge(deleteOriginalObjects);
        }

        private bool GetBatchExists(BatchKey key)
        {
            return StaticBatches.ContainsKey(key);
        }

        private void OnLevelOpened(string levelName)
        {
            if (string.IsNullOrEmpty(levelName))
            {
                return;
            }

            if (batchingEnabled)
            {
                MergeAllBatches();
            }
        }

        private void OnLevelClosed()
        {
            foreach (var batch in StaticBatches.Values)
            {
                batch.DestroyMergedMesh();
            }

            SurfaceMaterials.Clear();
            StaticBatches.Clear();
        }

        private void Start()
        {
            MapsLoading.Instance.OnLevelOpened += OnLevelOpened;
            MapsLoading.Instance.OnLevelClosed += OnLevelClosed;

            SeparateLights = separateLights;
#if USE_TEXTURE_ARRAYS
            SeparateTextures = separateTextures;
#endif
            SeparateShaders = separateShaders;
        }
    }
}