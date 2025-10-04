using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ParticleGrass
{
    [CreateAssetMenu(fileName = "GrassConfig", menuName = "ParticleGrass/Config")]
    public class GrassConfig : ScriptableObject
    {
        
#if UNITY_EDITOR
        private const string GrassDataComputeGUID = "350a1a92b316145ce817f37835c78f0e";
        private const string GrassArgsComputeGUID = "4e45d7afcb94d49c59d2cd3f5e5b82ef";
        private const string GrassWindComputeGUID = "81cf92f95dc0146069df6e4a479a6d2b";
        private const string GrassModelGUID = "1419e4b61e7f943a597dffd8ea03e679";
        private const string GrassMaterialGUID = "8c7fe73f265664be48bebe863a544d90";
#endif

        [Header("General Settings"), Space(10)]
        [SerializeField, Min(10f)] public float maxChunkSize = 50f;
        [SerializeField, Min(0f)] public float chunkHeightThreshold = 3f;
        [SerializeField, Min(0.1f)] public float windMapTexelPerUnit = 1f;
        [SerializeField, Min(0.1f)] public float grassMapTexelPerUnit = 1f;
        [SerializeField, Range(1, 256)] public int grassDensity = 100;
        [SerializeField, Min(0.1f)] public float renderDistance = 250f;
        [SerializeField] public float[] subdivisionDistances = {50f, 100f, 150f};
        
        [field: SerializeField] internal ComputeShader GrassDataCompute { get; private set; }
        [field: SerializeField] internal ComputeShader GrassArgsCompute { get; private set; }
        [field: SerializeField] internal ComputeShader GrassWindCompute { get; private set; }
        [field: SerializeField] internal Material GrassMaterial { get; private set; }
        [field: SerializeField] internal Mesh[] MeshLOD { get; private set; }
        
        internal int CalculateGrassArgsKernel { get; private set; }
        internal int CalculateGrassDataKernel { get; private set; }
        internal int CalculateGrassWindKernel { get; private set; }

        internal void InitializeComputeKernels()
        {
            CalculateGrassArgsKernel = GrassArgsCompute.FindKernel("CalculateGrassArgs");
            CalculateGrassDataKernel = GrassDataCompute.FindKernel("CalculateGrassData");
            CalculateGrassWindKernel = GrassWindCompute.FindKernel("CalculateGrassWind");
        }
        
#if UNITY_EDITOR
        internal void Validate()
        {
            GrassDataCompute = ValidateResource(GrassDataCompute, GrassDataComputeGUID);
            GrassArgsCompute = ValidateResource(GrassArgsCompute, GrassArgsComputeGUID);
            GrassWindCompute = ValidateResource(GrassWindCompute, GrassWindComputeGUID);
            GrassMaterial = ValidateResource(GrassMaterial, GrassMaterialGUID);

            if (MeshLOD == null || MeshLOD.Length == 0)
            {
                MeshLOD = new Mesh[3];
                
                var bladeModelMeshes = AssetDatabase
                    .LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(GrassModelGUID)).OfType<Mesh>().ToArray();

                MeshLOD[0] = bladeModelMeshes.First(mesh => mesh.name.EndsWith("LOD0"));
                MeshLOD[1] = bladeModelMeshes.First(mesh => mesh.name.EndsWith("LOD1"));
                MeshLOD[2] = bladeModelMeshes.First(mesh => mesh.name.EndsWith("LOD2"));
            }
        }

        private static T ValidateResource<T>(T resource, string guid) where T : Object
        {
            if (resource == null)
            {
                resource = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            }
            
            return resource;
        }
        
#endif
    }
}
