using System;
using System.Collections.Generic;
using System.Linq;
using ForgePlus.DataFileIO;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using RuntimeCore.Materials;
using UnityEngine;
using UnityEngine.Rendering;
using Weland;

namespace ForgePlus.ApplicationGeneral
{
    public class SurfaceBatchingManager : SingletonMonoBehaviour<SurfaceBatchingManager>
    {
#pragma warning disable CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
        public struct BatchKey
#pragma warning restore CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
#pragma warning restore CS0660 // Type defines operator == or operator != but does not override Object.Equals(object o)
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
            private ShapeDescriptor _sourceShapeDescriptor;

            public ShapeDescriptor SourceShapeDescriptor
            {
                get => _sourceShapeDescriptor;
                set { _sourceShapeDescriptor = SeparateTextures ? value : ShapeDescriptor.Empty; }
            }

            private ShapeDescriptor _layeredTransparentSideShapeDescriptor;
            public ShapeDescriptor LayeredTransparentSideShapeDescriptor
            {
                get => _layeredTransparentSideShapeDescriptor;
                set { _layeredTransparentSideShapeDescriptor = SeparateTextures ? value : ShapeDescriptor.Empty; }
            }
#endif

            public static bool operator ==(BatchKey a, BatchKey b)
            {
                return !(a != b);
            }

            public static bool operator !=(BatchKey a, BatchKey b)
            {
                // Defining this as it's a slightly more efficient way to determine equality for this struct
                return a.SourceMaterial != b.SourceMaterial ||
                       a.SourceLight != b.SourceLight ||
                       a.SourceMedia != b.SourceMedia ||
                       a.LayeredTransparentSideSourceMaterial != b.LayeredTransparentSideSourceMaterial ||
                       a.LayeredTransparentSideSourceLight != b.LayeredTransparentSideSourceLight;
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
            private GameObject mergeObject;

            private LevelEntity_Media media;

            public bool IsMerged
            {
                get { return mergeObject != null; }
            }

            public SurfaceBatch(Material[] sourceMaterials, LevelEntity_Media media)
            {
                this.sourceMaterials = sourceMaterials;
                surfaces = new List<Surface>();
                mergeObject = null;
                this.media = media;
            }

            public void AddSurface(RuntimeSurfaceGeometry surfaceGeometry, bool deleteOriginalObjects = false)
            {
                if (surfaces.Any(surface => surface.SurfaceGeometry == surfaceGeometry))
                {
                    Debug.LogError("Attempted adding surface to batch multiple times, this attempt will be ignored.");
                    return;
                }

                var isMerged = mergeObject != null;
                if (isMerged)
                {
                    Unmerge();
                }

                surfaces.Add(new Surface(surfaceGeometry));

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
                if (!surfaces.Any(surface => surface.SurfaceGeometry == surfaceGeometry))
                {
                    ////Debug.LogError("Attempted removing surface from batch that did not contain it, this attempt will be ignored.");
                    return surfaces.Count > 0;
                }

                var isMerged = mergeObject != null;
                if (isMerged)
                {
                    Unmerge();
                }

                surfaces.Remove(surfaces.First(surface => surface.SurfaceGeometry == surfaceGeometry));

                if (isMerged)
                {
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
                    var mergedVertices = new List<Vector3>();
                    var mergedTriangles = new List<int>();
                    var mergedUVs = new List<Vector4>();
                    var mergedUV1s = new List<Vector4>();
                    var mergedUV2s = new List<Vector4>();
                    var mergedColors = new List<Color>();

                    foreach (var surface in surfaces)
                    {
                        var dynamicMesh = surface.MakeStaticAndGetMesh();
                        mergedTriangles.AddRange(dynamicMesh.triangles.Select(triangleIndex => triangleIndex + mergedVertices.Count));

                        if (media != null)
                        {
                            mergedVertices.AddRange(dynamicMesh.vertices);
                        }
                        else
                        {
                            mergedVertices.AddRange(dynamicMesh.vertices.Select(position => surface.SurfaceGeometry.transform.localToWorldMatrix.MultiplyPoint(position)));
                        }

                        if (deleteOriginalObjects)
                        {
                            DestroyImmediate(surface.SurfaceGeometry.gameObject);
                        }

                        var uv0s = new List<Vector4>();
                        dynamicMesh.GetUVs(0, uv0s);
                        mergedUVs.AddRange(uv0s);

                        if (sourceMaterials.Length > 1)
                        {
                            var uv1s = new List<Vector4>();
                            dynamicMesh.GetUVs(1, uv1s);
                            mergedUV1s.AddRange(uv1s);

                            var uv2s = new List<Vector4>();
                            dynamicMesh.GetUVs(2, uv2s);
                            mergedUV2s.AddRange(uv2s);
                        }

                        mergedColors.AddRange(dynamicMesh.colors);
                    }

                    var mergedMesh = new Mesh();
                    mergedMesh.name = "Batched Mesh" + objectDescriptiveName;
                    mergedMesh.SetVertices(mergedVertices);
                    mergedMesh.SetTriangles(mergedTriangles, submesh: 0);

                    if (mergedVertices.Count != mergedUVs.Count)
                    {
                        Debug.Log($"{mergedVertices.Count} : {mergedUVs.Count}");
                        foreach (var surface in surfaces)
                        {
                            Debug.Log(surface.SurfaceGeometry.gameObject, surface.SurfaceGeometry.gameObject);
                        }
                    }

                    mergedMesh.SetUVs(channel: 0, uvs: mergedUVs);

                    if (sourceMaterials.Length > 1)
                    {
                        mergedMesh.SetUVs(channel: 1, uvs: mergedUV1s);
                        mergedMesh.SetUVs(channel: 2, uvs: mergedUV2s);
                    }

                    mergedMesh.SetColors(mergedColors);
                    mergedMesh.RecalculateNormals(MeshUpdateFlags.DontNotifyMeshUsers |
                                                  MeshUpdateFlags.DontRecalculateBounds |
                                                  MeshUpdateFlags.DontResetBoneBounds);
                    mergedMesh.RecalculateTangents(MeshUpdateFlags.DontNotifyMeshUsers |
                                                   MeshUpdateFlags.DontRecalculateBounds |
                                                   MeshUpdateFlags.DontResetBoneBounds);

                    mergeObject.AddComponent<MeshFilter>().sharedMesh = mergedMesh;
                    mergeObject.AddComponent<MeshRenderer>().sharedMaterials = sourceMaterials;

                    if (media != null)
                    {
                        media.SubscribeSurface(mergeObject.transform);
                    }
                }
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

                foreach (var surface in surfaces)
                {
                    surface.MakeDynamic();

                    if (media != null)
                    {
                        media.SubscribeSurface(surface.SurfaceGeometry.transform);
                    }
                }
            }
        }

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
            SurfaceMaterials.Clear();
            StaticBatches.Clear();
        }

        private void Start()
        {
            MapsLoading.Instance.OnLevelOpened += OnLevelOpened;
            MapsLoading.Instance.OnLevelClosed += OnLevelClosed;

            SeparateLights = separateLights;
            SeparateTextures = separateTextures;
            SeparateShaders = separateShaders;
        }
    }
}