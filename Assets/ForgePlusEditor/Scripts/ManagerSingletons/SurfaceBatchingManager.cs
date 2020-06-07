using ForgePlus.DataFileIO;
using ForgePlus.LevelManipulation;
using RuntimeCore.Entities;
using RuntimeCore.Entities.Geometry;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

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
            public static bool operator ==(BatchKey a, BatchKey b)
            {
                return !(a != b);
            }

            public static bool operator !=(BatchKey a, BatchKey b)
            {
                // Defining this as it's a slightly more efficient way to determine equality for this struct
                return a.sourceMaterial != b.sourceMaterial ||
                       a.sourceLight != b.sourceLight ||
                       a.sourceMedia != b.sourceMedia ||
                       a.layeredTransparentSideSourceMaterial != b.layeredTransparentSideSourceMaterial ||
                       a.layeredTransparentSideSourceLight != b.layeredTransparentSideSourceLight;
            }

            // Note: Using Material instead of ShapeDescriptor here,
            // as it accounts for unique shaders used for the same texture.
            // (such as a media texture applied to a wall vs actually applied to media)
            public Material sourceMaterial;
            public LevelEntity_Light sourceLight;
            public LevelEntity_Media sourceMedia;
            public Material layeredTransparentSideSourceMaterial;
            public LevelEntity_Light layeredTransparentSideSourceLight;
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
                get
                {
                    return mergeObject != null;
                }
            }

            public SurfaceBatch(Material[] sourceMaterials, LevelEntity_Media media)
            {
                this.sourceMaterials = sourceMaterials;
                surfaces = new List<Surface>();
                mergeObject = null;
                this.media = media;
            }

            public void AddSurface(RuntimeSurfaceGeometry surfaceGeometry)
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
                    Merge();
                }
                else if (media != null)
                {
                    media.SubscribeSurface(surfaceGeometry.transform);
                }
            }

            // Returns False if this StaticBatch is now empty, true otherwise
            public bool RemoveSurface(RuntimeSurfaceGeometry surfaceGeometry)
            {
                if (!surfaces.Any(surface => surface.SurfaceGeometry == surfaceGeometry))
                {
                    Debug.LogError("Attempted removing surface from batch that did not contain it, this attempt will be ignored.");
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
                    Merge();
                }
                else if (media != null)
                {
                    media.UnsubscribeSurface(surfaceGeometry.transform);
                }

                return surfaces.Count > 0;
            }

            public void Merge()
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

                mergeObject = new GameObject("Batched Surfaces");
                mergeObject.transform.SetParent(LevelEntity_Level.Instance.transform);

                var mergedVertices = new List<Vector3>();
                var mergedTriangles = new List<int>();
                var mergedUVs = new List<Vector2>();
                var mergedUV1s = new List<Vector2>();
                var mergedUV2s = new List<Vector2>();
                var mergedUV3s = new List<Vector2>();
                var mergedColors = new List<Color>();

                foreach (var surface in surfaces)
                {
                    var dynamicMesh = surface.MakeStaticAndGetMesh();
                    mergedTriangles.AddRange(dynamicMesh.triangles.Select(triangleIndex => triangleIndex + mergedVertices.Count));
                    mergedVertices.AddRange(dynamicMesh.vertices.Select(position => surface.SurfaceGeometry.transform.localToWorldMatrix.MultiplyPoint(position)));
                    mergedUVs.AddRange(dynamicMesh.uv);

                    if (sourceMaterials.Length > 1)
                    {
                        // Mesh.uv2 is uv1 in shaders/channels, because Unity is afraid to break old code and fix this
                        mergedUV1s.AddRange(dynamicMesh.uv2);
                        mergedUV2s.AddRange(dynamicMesh.uv3);
                        mergedUV3s.AddRange(dynamicMesh.uv4);
                    }

                    mergedColors.AddRange(dynamicMesh.colors);
                }

                var mergedMesh = new Mesh();
                mergedMesh.name = "Batched Mesh";
                mergedMesh.SetVertices(mergedVertices);
                mergedMesh.SetTriangles(mergedTriangles, submesh: 0);

                if (mergedVertices.Count != mergedUVs.Count)
                {
                    Debug.Log($"{mergedVertices.Count} : {mergedUVs.Count}");
                }
                mergedMesh.SetUVs(channel: 0, uvs: mergedUVs);

                if (sourceMaterials.Length > 1)
                {
                    mergedMesh.SetUVs(channel: 1, uvs: mergedUV1s);
                    mergedMesh.SetUVs(channel: 2, uvs: mergedUV2s);
                    mergedMesh.SetUVs(channel: 3, uvs: mergedUV3s);
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

        [SerializeField]
        private bool batchingEnabled = true;

        private readonly Dictionary<BatchKey, Material[]> SurfaceMaterials = new Dictionary<BatchKey, Material[]>();
        private readonly Dictionary<BatchKey, SurfaceBatch> StaticBatches = new Dictionary<BatchKey, SurfaceBatch>();

        public static bool BatchingEnabled => Instance.batchingEnabled;

        public Material[] GetUniqueMaterials(BatchKey key)
        {
            if (SurfaceMaterials.ContainsKey(key))
            {
                return SurfaceMaterials[key];
            }
            else
            {
                Material[] uniqueMaterials = key.layeredTransparentSideSourceMaterial ? new Material[2] : new Material[1];

                uniqueMaterials[0] = new Material(key.sourceMaterial);

                if (key.layeredTransparentSideSourceMaterial)
                {
                    uniqueMaterials[1] = new Material(key.layeredTransparentSideSourceMaterial);
                }

                SurfaceMaterials[key] = uniqueMaterials;

                key.sourceLight?.SubscribeMaterial(uniqueMaterials[0]);
                key.layeredTransparentSideSourceLight?.SubscribeMaterial(uniqueMaterials[1]);
                key.sourceMedia?.SubscribeMaterial(uniqueMaterials[0]);

                return uniqueMaterials;
            }
        }

        public void AddToBatches(BatchKey key, RuntimeSurfaceGeometry surfaceGeometry)
        {
            if (!StaticBatches.ContainsKey(key))
            {
                StaticBatches[key] = new SurfaceBatch(GetUniqueMaterials(key), key.sourceMedia);
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
            StaticBatches[key].Merge();
        }

        private bool GetBatchExists(BatchKey key)
        {
            return StaticBatches.ContainsKey(key);
        }

        private void OnLevelOpened(string levelName)
        {
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
        }
    }
}
