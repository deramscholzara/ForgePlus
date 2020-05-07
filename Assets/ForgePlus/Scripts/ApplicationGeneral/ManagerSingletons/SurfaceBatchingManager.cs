using ForgePlus.DataFileIO;
using ForgePlus.LevelManipulation;
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
            public FPLight sourceLight;
            public FPMedia sourceMedia;
            public Material layeredTransparentSideSourceMaterial;
            public FPLight layeredTransparentSideSourceLight;
        }

        private class SurfaceMaterial
        {
            // TODO: make this, and use it in the dictionary below
        }

        private class SurfaceBatch
        {
            public class Surface
            {
                public GameObject Root { get; private set; }

                public Surface(GameObject root)
                {
                    Root = root;
                }

                public Mesh MakeStaticAndGetMesh()
                {
                    Root.GetComponent<Renderer>().enabled = false;

                    return Root.GetComponent<MeshFilter>().sharedMesh;
                }

                public void MakeDynamic()
                {
                    Root.GetComponent<Renderer>().enabled = true;
                }
            }

            private Material[] sourceMaterials;
            private List<Surface> surfaces;
            private GameObject mergeObject;

            public SurfaceBatch(Material[] sourceMaterials)
            {
                this.sourceMaterials = sourceMaterials;
                surfaces = new List<Surface>();
                mergeObject = null;
            }

            public void AddSurface(GameObject root)
            {
                var isMerged = mergeObject != null;
                if (isMerged)
                {
                    Unmerge();
                }

                surfaces.Add(new Surface(root));

                if (isMerged)
                {
                    Merge();
                }
            }

            // Returns False if this StaticBatch is now empty, true otherwise
            public bool RemoveSurface(GameObject root)
            {
                var isMerged = mergeObject != null;
                if (isMerged)
                {
                    Unmerge();
                }

                surfaces.Remove(surfaces.First(surface => surface.Root == root));

                if (isMerged)
                {
                    Merge();
                }

                return surfaces.Count > 0;
            }

            public void Merge()
            {
                if (mergeObject)
                {
                    // If it's already merged, unmerge so it can remerge
                    Unmerge();
                }

                mergeObject = new GameObject("Batched Surfaces");
                mergeObject.transform.SetParent(FPLevel.Instance.transform);

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
                    mergedVertices.AddRange(dynamicMesh.vertices.Select(position => surface.Root.transform.localToWorldMatrix.MultiplyPoint(position)));
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
            }

            public void Unmerge()
            {
                if (mergeObject)
                {
                    Destroy(mergeObject);

                    foreach (var surface in surfaces)
                    {
                        surface.MakeDynamic();
                    }
                }
            }
        }

        [SerializeField]
        private bool applyStaticBatchingOnLevelLoad = true;

        private readonly Dictionary<BatchKey, Material[]> SurfaceMaterials = new Dictionary<BatchKey, Material[]>();
        private readonly Dictionary<BatchKey, SurfaceBatch> StaticBatches = new Dictionary<BatchKey, SurfaceBatch>();

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

        public void AddToBatches(BatchKey key, GameObject surface)
        {
            if (!StaticBatches.ContainsKey(key))
            {
                StaticBatches[key] = new SurfaceBatch(GetUniqueMaterials(key));
            }

            StaticBatches[key].AddSurface(surface);
        }

        public void RemoveFromBatches(BatchKey key, GameObject surface)
        {
            var occupied = StaticBatches[key].RemoveSurface(surface);

            if (!occupied)
            {
                StaticBatches.Remove(key);
            }
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

        public void MergeBatch(BatchKey key)
        {
            StaticBatches[key].Merge();
        }

        public void UnmergeBatch(BatchKey key)
        {
            StaticBatches[key].Unmerge();
        }

        private void OnLevelOpened(string levelName)
        {
            if (applyStaticBatchingOnLevelLoad)
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
