using ForgePlus.DataFileIO;
using ForgePlus.LevelManipulation;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace ForgePlus.ApplicationGeneral
{
    public class SurfaceBatchingManager : SingletonMonoBehaviour<SurfaceBatchingManager>
    {
        public struct RuntimeSurfaceMaterialKey
        {
            // Note: Using Material instead of ShapeDescriptor here,
            // as it accounts for unique shaders used for the same texture.
            // (such as a media texture applied to a wall vs actually applied to media)
            public Material sourceMaterial;
            public FPLight sourceLight;
            public FPMedia sourceMedia;
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

            private Material sourceMaterial;
            private List<Surface> surfaces;
            private GameObject mergeObject;

            public SurfaceBatch(Material sourceMaterial)
            {
                this.sourceMaterial = sourceMaterial;
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
                var mergedColors = new List<Color>();

                foreach (var surface in surfaces)
                {
                    var dynamicMesh = surface.MakeStaticAndGetMesh();
                    mergedTriangles.AddRange(dynamicMesh.triangles.Select(triangleIndex => triangleIndex + mergedVertices.Count));
                    mergedVertices.AddRange(dynamicMesh.vertices.Select(position => surface.Root.transform.localToWorldMatrix.MultiplyPoint(position)));
                    mergedUVs.AddRange(dynamicMesh.uv);
                    mergedColors.AddRange(dynamicMesh.colors);
                }

                var mergedMesh = new Mesh();
                mergedMesh.name = "Batched Mesh";
                mergedMesh.SetVertices(mergedVertices);
                mergedMesh.SetTriangles(mergedTriangles, submesh: 0);
                mergedMesh.SetUVs(channel: 0, uvs: mergedUVs);
                mergedMesh.SetColors(mergedColors);
                mergedMesh.RecalculateNormals(MeshUpdateFlags.DontNotifyMeshUsers |
                                              MeshUpdateFlags.DontRecalculateBounds |
                                              MeshUpdateFlags.DontResetBoneBounds);
                mergedMesh.RecalculateTangents(MeshUpdateFlags.DontNotifyMeshUsers |
                                               MeshUpdateFlags.DontRecalculateBounds |
                                               MeshUpdateFlags.DontResetBoneBounds);

                mergeObject.AddComponent<MeshFilter>().sharedMesh = mergedMesh;
                mergeObject.AddComponent<MeshRenderer>().sharedMaterial = sourceMaterial;
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

        private readonly Dictionary<RuntimeSurfaceMaterialKey, Material> SurfaceMaterials = new Dictionary<RuntimeSurfaceMaterialKey, Material>();
        private readonly Dictionary<RuntimeSurfaceMaterialKey, SurfaceBatch> StaticBatches = new Dictionary<RuntimeSurfaceMaterialKey, SurfaceBatch>();

        public Material GetUniqueMaterial(RuntimeSurfaceMaterialKey key)
        {
            if (SurfaceMaterials.ContainsKey(key))
            {
                return SurfaceMaterials[key];
            }
            else
            {
                var uniqueMaterial = new Material(key.sourceMaterial);

                SurfaceMaterials[key] = uniqueMaterial;

                if (key.sourceLight != null)
                {
                    key.sourceLight.SubscribeMaterial(uniqueMaterial);
                }

                if (key.sourceMedia != null)
                {
                    key.sourceMedia.SubscribeMaterial(uniqueMaterial);
                }

                return uniqueMaterial;
            }
        }

        public void AddToBatches(RuntimeSurfaceMaterialKey key, GameObject surface)
        {
            if (!StaticBatches.ContainsKey(key))
            {
                StaticBatches[key] = new SurfaceBatch(GetUniqueMaterial(key));
            }

            StaticBatches[key].AddSurface(surface);
        }

        public void RemoveFromBatches(RuntimeSurfaceMaterialKey key, GameObject surface)
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

        public void MergeBatch(RuntimeSurfaceMaterialKey key)
        {
            StaticBatches[key].Merge();
        }

        public void UnmergeBatch(RuntimeSurfaceMaterialKey key)
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
            LevelData.OnLevelOpened += OnLevelOpened;
            LevelData.OnLevelClosed += OnLevelClosed;
        }
    }
}
