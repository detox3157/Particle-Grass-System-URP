using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

using static Unity.Mathematics.math;

namespace ParticleGrass
{
    [ExecuteInEditMode]
    public abstract partial class GrassSurface : MonoBehaviour
    {
        [SerializeField] private bool debugChunkBounds = false;
        
        [SerializeField] internal Texture2D grassMapAsset;
        
#if UNITY_EDITOR
        private RenderTexture _heightmapRT;
#endif

        private string _name = "Unnamed Grass Surface";

        internal string Name => _name;
        
        public RTHandle Heightmap { get; private set; }
        
        public int2 HeightmapResolution { get; private set; }
        
        public RTHandle GrassMap { get; private set; }
        
        public int2 GrassMapResolution { get; private set; }
        
        public RTHandle WindMap {get; private set;}
        
        public int2 WindMapResolution {get; private set;}
        
        public float HeightmapHeight { get; private set; }
        
        public Bounds Bounds { get; private set; }
        
        public float3 BoundsMin => Bounds.min;
        
        public float3 BoundsMax => Bounds.max;
        
        public float3 BoundsSize => Bounds.size;
        
        #region Instance Management
        
        private static readonly HashSet<GrassSurface> InstancesSet = new HashSet<GrassSurface>();
        
        internal static GrassSurface[] Instances { get; private set; } = Array.Empty<GrassSurface>();

        public static LayerMask SurfaceLayers { get; private set; } = 0;

        private static void RegisterInstance(GrassSurface instance)
        {
            InstancesSet.Add(instance);

            UpdateInstances();
            UpdateInstanceLayers();
        }
        
        private static void UnregisterInstance(GrassSurface instance)
        {
            InstancesSet.Remove(instance);
            
            UpdateInstances();
        }

        private static void UpdateInstances()
        {
            Instances = InstancesSet.ToArray();
        }

        private static void UpdateInstanceLayers()
        {
            SurfaceLayers = 0;
            
            foreach (var surface in Instances)
            {
                SurfaceLayers |= 1 << surface.gameObject.layer;
                
            }
        }
        
        #endregion

        protected abstract void Initialize(ref string surfaceName);

        protected abstract void Dispose();
        
        protected abstract RenderTexture GetHeightmap(out float heightmapHeight);

        protected abstract Bounds GetBounds();

        protected void ForceUpdateBounds(Bounds bounds)
        {
            Bounds = bounds;
            
            InitializeChunks();
        }
        
        private void OnEnable()
        {
            Initialize(ref _name);
            
            Bounds = GetBounds();

            AllocateHeightmap();
            AllocateGrassMap();
            AllocateWindMap();
            
            InitializeChunks();

            RegisterInstance(this);
        }

        private void OnDisable()
        {
            Dispose();
            
            WindMap?.Release();
            GrassMap?.Release();
            Heightmap?.Release();
            
            UnregisterInstance(this);
        }
        
        private void Update()
        {
#if UNITY_EDITOR
            if (transform.hasChanged)
            {
                Bounds = GetBounds();
                FillChunkBounds();
                transform.hasChanged = false;
            }
            
            ValidateHeightmapRT();
#endif
        }

        private void OnDrawGizmos()
        {
#if UNITY_EDITOR
            if (debugChunkBounds)
            {
                var renderChunks = GetRenderChunks(Camera.current);
                
                foreach (var chunk in renderChunks)
                {
                    Gizmos.DrawWireCube(chunk.Center, chunk.Size);
                }
            }
#endif
        }

#if UNITY_EDITOR

        private void OnValidate()
        {
            OnDisable();
            OnEnable();
        }

        private void ValidateHeightmapRT()
        {
            if (!_heightmapRT.IsDestroyed() && _heightmapRT.IsCreated())
            {
                return;
            }
            
            AllocateHeightmap();
            InitializeChunks();
        }
        
#endif
        
        private void AllocateHeightmap()
        {
            var heightmapRT = GetHeightmap(out var heightmapHeight);
            
#if UNITY_EDITOR
            _heightmapRT = heightmapRT;
#endif
            
            HeightmapHeight = heightmapHeight;
            HeightmapResolution = int2(heightmapRT.width, heightmapRT.height);
            Heightmap = RTHandles.Alloc(heightmapRT);
        }

        private void AllocateGrassMap()
        {
#if UNITY_EDITOR
            if (grassMapAsset == null)
            {
                grassMapAsset = GrassResources.CreateGrassMapAsset(GetGrassMapResolutionBySize(BoundsSize.xz), Name);
            }
#endif

            var grassMapRT = GrassMapAssetToRT(grassMapAsset);
            
            GrassMapResolution = int2(grassMapRT.width, grassMapRT.height);
            
            GrassMap = RTHandles.Alloc(grassMapRT);
        }

        private RenderTexture GrassMapAssetToRT(Texture2D asset)
        {
            var rt = new RenderTexture(new RenderTextureDescriptor
            {
                width = asset.width,
                height = asset.height,
                depthBufferBits = 0,
                volumeDepth = 1,
                msaaSamples = 1,
                dimension = TextureDimension.Tex2D,
                colorFormat = RenderTextureFormat.ARGB32,
                enableRandomWrite = true,
            });
            
            Graphics.Blit(asset, rt);
            return rt;
        }

#if  UNITY_EDITOR

        [ContextMenu("Save Grass Map")]
        private void SaveGrassMapAsset()
        {
            if (grassMapAsset == null)
            {
                grassMapAsset = GrassResources.CreateGrassMapAsset(GetGrassMapResolutionBySize(BoundsSize.xz), Name);
            }
            
            var previousActiveRT = RenderTexture.active;
            RenderTexture.active = GrassMap.rt;
            grassMapAsset.ReadPixels(new Rect(0, 0, GrassMapResolution.x, GrassMapResolution.y), 0, 0);
            RenderTexture.active = previousActiveRT;
            grassMapAsset.Apply();

            UnityEditor.EditorUtility.SetDirty(grassMapAsset);
            UnityEditor.AssetDatabase.SaveAssets();
        }
        
#endif

        private void AllocateWindMap()
        {
            WindMapResolution = GetWindMapResolutionBySize(BoundsSize.xz);
            WindMap = RTHandles.Alloc(WindMapResolution.x, WindMapResolution.y, enableRandomWrite: true);
        }

        private static int2 GetWindMapResolutionBySize(float2 size)
        {
            return int2(
                (int) ceil(min(GrassResources.MaxTextureResolution, size.x * GrassResources.Config.windMapTexelPerUnit)),
                (int) ceil(min(GrassResources.MaxTextureResolution, size.y * GrassResources.Config.windMapTexelPerUnit))
            );
        }
        
        private static int GetGrassMapResolutionBySize(float2 size)
        {
            return min(
                GrassResources.MaxTextureResolution, 
                Mathf.NextPowerOfTwo(Mathf.CeilToInt(max(size.x, size.y) * GrassResources.Config.grassMapTexelPerUnit))
            );
        }
    }
}
