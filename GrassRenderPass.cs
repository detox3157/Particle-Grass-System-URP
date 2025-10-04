using System;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

using static Unity.Mathematics.math;
using static System.Runtime.InteropServices.Marshal;

namespace ParticleGrass
{
    internal class GrassRenderPass : ScriptableRenderPass
    {
        #region Shader ID
        
        private static readonly int WindMapID = Shader.PropertyToID("_WindMap");
        private static readonly int WindMapConfigID = Shader.PropertyToID("_WindMapConfig");
        private static readonly int WindConfigID = Shader.PropertyToID("_WindConfig");
        private static readonly int TerrainConfigID = Shader.PropertyToID("_TerrainConfig");
        private static readonly int IndexCountID = Shader.PropertyToID("_IndexCount");
        private static readonly int ArgsBufferID = Shader.PropertyToID("_ArgsBuffer");
        private static readonly int GrassBufferID = Shader.PropertyToID("_GrassBuffer");
        private static readonly int GrassConfigID = Shader.PropertyToID("_GrassConfig");
        private static readonly int ChunkConfigID = Shader.PropertyToID("_ChunkConfig");
        private static readonly int ArtisticBufferID = Shader.PropertyToID("_ArtisticBuffer");
        private static readonly int HeightmapConfigID = Shader.PropertyToID("_HeightmapConfig");
        private static readonly int HeightmapID = Shader.PropertyToID("_Heightmap");
        private static readonly int GrassMapID = Shader.PropertyToID("_GrassMap");
        private static readonly int GrassMapConfigID = Shader.PropertyToID("_GrassMapConfig");
        
        #endregion

        private const int BatchSize = 8;
        private const string PassName = "Grass Pass";

        private static readonly int ArgsSize = SizeOf<GraphicsBuffer.IndirectDrawIndexedArgs>();
        
        private GraphicsBuffer _argsBuffer;
        private GraphicsBuffer[] _grassBuffers;
        private GraphicsBuffer _artisticBuffer;

        private BufferHandle[] _grassBufferHandles;
        private BufferHandle _argsBufferHandle;
        private BufferHandle _artisticBufferHandle;
        
        private TextureHandle _heightmapHandle;
        private int2 _heightmapResolution;
        private TextureHandle _grassMapHandle;
        private int2 _grassMapResolution;
        private TextureHandle _windMapHandle;
        private int2 _windMapResolution;
        
        private float _windIntensity, _windDirection;

        private int _grassDensityReference;
        
        private struct GrassData
        {
            private float3 _position;
            private float2 _bitangent;
            private float _tilt;
            private float _bend;
            private float _scale;
            private float _width;
            private float _windIntensity;
            private float _phaseOffset;
            private float _type;
            private uint _hash;
        }
        
        private class PassData
        {
            internal float3 CameraPosition;
            
            internal Bounds TerrainBounds;
            
            internal BufferHandle ArgsBuffer;
            internal BufferHandle[] GrassBuffers;
            internal BufferHandle ArtisticBuffer;
            
            internal TextureHandle ActiveColorBuffer;
            internal TextureHandle ActiveDepthTexture;

            internal TextureHandle HeightmapTexture;
            internal int2 HeightmapResolution;
            internal TextureHandle GrassMapTexture;
            internal int2 GrassMapResolution;
            internal TextureHandle WindMapTexture;
            internal int2 WindMapResolution;
            
            internal float WindPhase;
            internal float WindIntensity;
            internal float WindDirection;
            internal float HeightmapHeight;
            
            internal GrassChunk[] Chunks;

            internal GrassArtisticParameters ArtisticParameters;
        }
        
        internal void Setup(GrassRendererSettings settings)
        {
            _windIntensity = settings.windIntensity;
            _windDirection = settings.windDirection;

            UpdateArtisticBuffer();

#if UNITY_EDITOR
            GrassType.OnGrassTypesChanged += UpdateArtisticBuffer;
#endif

            InitializeBuffers();
        }

        private void UpdateArtisticBuffer()
        {
            _artisticBuffer?.Release();
            _artisticBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, GrassResources.GrassTypes.Length, SizeOf<GrassArtisticParameters>());
            _artisticBuffer.SetData(GrassResources.GrassTypes.Select(type => type.ArtisticParameters).ToArray());
        }

        private void InitializeBuffers()
        {
            _grassDensityReference = GrassResources.Config.grassDensity;
            _argsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, BatchSize, ArgsSize);
            _grassBuffers = new GraphicsBuffer[BatchSize];

            var bufferSize = _grassDensityReference * _grassDensityReference;
            
            for (var bufferId = 0; bufferId < BatchSize; bufferId++)
            {
                _grassBuffers[bufferId] = new GraphicsBuffer(GraphicsBuffer.Target.Structured, bufferSize, SizeOf<GrassData>());
            }
        }

        private void ReleaseBuffers()
        {
            _argsBuffer?.Dispose();

            foreach (var grassBuffer in _grassBuffers)
            {
                grassBuffer?.Release();
            }
        }
        
        private void ValidateGrassBufferSize()
        {
            if (GrassResources.Config.grassDensity != _grassDensityReference)
            {
                ReleaseBuffers();
                InitializeBuffers();
            }
        }
        
        private void ImportBuffers(RenderGraph renderGraph)
        {
            _grassBufferHandles = new BufferHandle[BatchSize];
                
            for (var bufferId = 0; bufferId < _grassBufferHandles.Length; bufferId++)
            {
                _grassBufferHandles[bufferId] = renderGraph.ImportBuffer(_grassBuffers[bufferId]);
            }
                
            _argsBufferHandle = renderGraph.ImportBuffer(_argsBuffer);
            _artisticBufferHandle = renderGraph.ImportBuffer(_artisticBuffer);
        }

        private void ImportTextures(RenderGraph renderGraph, GrassSurface surface)
        {
            _heightmapHandle = renderGraph.ImportTexture(surface.Heightmap);
            _grassMapHandle = renderGraph.ImportTexture(surface.GrassMap);
            _windMapHandle = renderGraph.ImportTexture(surface.WindMap);
            
            _heightmapResolution = surface.HeightmapResolution;
            _grassMapResolution = surface.GrassMapResolution;
            _windMapResolution = surface.WindMapResolution;
        }

        private void SetupPassData(ref PassData passData, float3 cameraPosition, GrassChunk[] chunks, UniversalResourceData resourceData, Bounds terrainBounds, float heightmapHeight)
        {
            passData.CameraPosition = cameraPosition;
            
            passData.TerrainBounds = terrainBounds;
            
            passData.HeightmapTexture = _heightmapHandle;
            passData.HeightmapResolution = _heightmapResolution;
            passData.GrassMapTexture = _grassMapHandle;
            passData.GrassMapResolution = _grassMapResolution;
            passData.WindMapTexture = _windMapHandle;
            passData.WindMapResolution = _windMapResolution;
            
            passData.ArgsBuffer = _argsBufferHandle;
            passData.GrassBuffers = _grassBufferHandles;
            passData.ArtisticBuffer = _artisticBufferHandle;

            passData.WindIntensity = _windIntensity;
            passData.WindPhase = Time.time;
            passData.WindDirection = _windDirection;
            passData.HeightmapHeight = heightmapHeight;
            
            passData.Chunks = chunks;
            passData.ActiveColorBuffer = resourceData.activeColorTexture;
            passData.ActiveDepthTexture = resourceData.activeDepthTexture;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var cam = cameraData.camera;
            
            ValidateGrassBufferSize();
            ImportBuffers(renderGraph);
            
            foreach (var surface in GrassSurface.Instances)
            {
                var renderChunks = surface.GetRenderChunks(cam);
                
                if (renderChunks.Length == 0)
                {
                    continue;
                }
                
                ImportTextures(renderGraph, surface);
                
                using var builder = renderGraph.AddUnsafePass<PassData>($"{PassName} for {surface.Name}", out var data);
                
                builder.AllowPassCulling(false);
                
                SetupPassData(ref data, cam.transform.position, renderChunks, resourceData, surface.Bounds, surface.HeightmapHeight);
                
                builder.SetRenderFunc((PassData passData, UnsafeGraphContext graphContext) => ExecutePass(passData, graphContext));
            }
        }

        private static void SetupConfigs(CommandBuffer cmd, PassData passData)
        {
            var grassConfig = float4(GrassResources.Config.grassDensity, passData.CameraPosition);
            var windConfig = float4(passData.WindPhase, passData.WindIntensity, passData.WindDirection, 0);
            var windMapConfig = float4(passData.WindMapResolution.x, passData.WindMapResolution.y, 0, 0);
            var terrainConfig = float4(float3(passData.TerrainBounds.min).xz, float3(passData.TerrainBounds.size).xz);
            var heightmapConfig = float4(passData.HeightmapResolution.x, passData.HeightmapResolution.y, passData.TerrainBounds.min.y, passData.HeightmapHeight);
            var grassMapConfig = float4(passData.GrassMapResolution.x, passData.GrassMapResolution.y, 0, 0);
            
            cmd.SetGlobalVector(WindConfigID, windConfig);
            
            cmd.SetComputeVectorParam(GrassResources.Config.GrassArgsCompute, GrassConfigID, grassConfig);
            cmd.SetComputeVectorParam(GrassResources.Config.GrassDataCompute, GrassConfigID, grassConfig);
            cmd.SetComputeVectorParam(GrassResources.Config.GrassDataCompute, TerrainConfigID, terrainConfig);
            cmd.SetComputeVectorParam(GrassResources.Config.GrassWindCompute, TerrainConfigID, terrainConfig);
            cmd.SetComputeVectorParam(GrassResources.Config.GrassDataCompute, WindMapConfigID, windMapConfig);
            cmd.SetComputeVectorParam(GrassResources.Config.GrassWindCompute, WindMapConfigID, windMapConfig);
            cmd.SetComputeVectorParam(GrassResources.Config.GrassDataCompute, HeightmapConfigID, heightmapConfig);
            cmd.SetComputeVectorParam(GrassResources.Config.GrassDataCompute, GrassMapConfigID, grassMapConfig);
        }

        private static void ExecutePass(PassData passData, UnsafeGraphContext graphContext)
        {
            var cmd = CommandBufferHelpers.GetNativeCommandBuffer(graphContext.cmd);
            var batchCount = (passData.Chunks.Length + BatchSize - 1) / BatchSize;
            var batch = new GrassChunk[BatchSize];
            
            cmd.SetRenderTarget(passData.ActiveColorBuffer, passData.ActiveDepthTexture);
            
            SetupConfigs(cmd, passData);
            
            cmd.SetComputeBufferParam(GrassResources.Config.GrassArgsCompute, GrassResources.Config.CalculateGrassDataKernel, ArgsBufferID, passData.ArgsBuffer);
            cmd.SetComputeBufferParam(GrassResources.Config.GrassDataCompute, GrassResources.Config.CalculateGrassDataKernel, ArtisticBufferID, passData.ArtisticBuffer);
            
            cmd.SetComputeTextureParam(GrassResources.Config.GrassDataCompute, GrassResources.Config.CalculateGrassDataKernel, WindMapID, passData.WindMapTexture);
            cmd.SetComputeTextureParam(GrassResources.Config.GrassWindCompute, GrassResources.Config.CalculateGrassWindKernel, WindMapID, passData.WindMapTexture);
            
            cmd.DispatchCompute(GrassResources.Config.GrassWindCompute, GrassResources.Config.CalculateGrassWindKernel, (passData.WindMapResolution.x + 7) / 8, (passData.WindMapResolution.y + 7) / 8, 1);
            
            for (var batchId = 0; batchId < batchCount; batchId++)
            {
                var thisBatchSize = min(passData.Chunks.Length - batchId * BatchSize, BatchSize);
                    
                for (var chunkInBatch = 0; chunkInBatch < thisBatchSize; chunkInBatch++)
                {
                    batch[chunkInBatch] = passData.Chunks[batchId * BatchSize + chunkInBatch];
                }
                    
                ExecuteBatch(batch, thisBatchSize, passData, cmd);
            }
        }

        private static void ExecuteBatch(GrassChunk[] batch, int batchSize, PassData passData, CommandBuffer cmd)
        {
            for (var chunkId = 0; chunkId < batchSize; chunkId++)
            {
                var chunkConfig = float4(batch[chunkId].Center.xz, batch[chunkId].Size.xz);
                
                cmd.SetComputeIntParam(GrassResources.Config.GrassArgsCompute, IndexCountID, (int) GrassResources.Config.MeshLOD[0].GetIndexCount(0));
                cmd.SetComputeVectorParam(GrassResources.Config.GrassDataCompute, ChunkConfigID, chunkConfig);
                cmd.SetComputeBufferParam(GrassResources.Config.GrassDataCompute, GrassResources.Config.CalculateGrassDataKernel, GrassBufferID, passData.GrassBuffers[chunkId]);
                cmd.SetComputeTextureParam(GrassResources.Config.GrassDataCompute, GrassResources.Config.CalculateGrassDataKernel, HeightmapID, passData.HeightmapTexture);
                cmd.SetComputeTextureParam(GrassResources.Config.GrassDataCompute, GrassResources.Config.CalculateGrassDataKernel, GrassMapID, passData.GrassMapTexture);
                
                cmd.DispatchCompute(GrassResources.Config.GrassDataCompute, GrassResources.Config.CalculateGrassDataKernel, (GrassResources.Config.grassDensity + 7) / 8, (GrassResources.Config.grassDensity + 7) / 8, 1);
                cmd.DispatchCompute(GrassResources.Config.GrassArgsCompute, GrassResources.Config.CalculateGrassArgsKernel, BatchSize, 1, 1);
            }
            
            for (var chunkId = 0; chunkId < batchSize; chunkId++)
            {
                var propertyBlock = new MaterialPropertyBlock();
                propertyBlock.SetBuffer(GrassBufferID, passData.GrassBuffers[chunkId]);
                propertyBlock.SetBuffer(ArtisticBufferID, passData.ArtisticBuffer);
            
                cmd.DrawMeshInstancedIndirect(GrassResources.Config.MeshLOD[0], 0,  GrassResources.Config.GrassMaterial, 0, passData.ArgsBuffer, chunkId * ArgsSize, propertyBlock);
            }
        }
        
        internal void Dispose()
        {
            _artisticBuffer?.Release();
            
            ReleaseBuffers();
        }
    }
}
