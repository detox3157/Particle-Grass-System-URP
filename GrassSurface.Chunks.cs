using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Mathematics;

using static Unity.Mathematics.math;
using static UnityEngine.Mathf;

namespace ParticleGrass
{
    public partial class GrassSurface
    {
        private GrassChunk[] _chunks = Array.Empty<GrassChunk>();
        private int2 _chunksResolution;
        private float3 _chunkSize;

        private void InitializeChunks()
        {
            _chunksResolution = int2(
                CeilToInt(Bounds.size.x / GrassResources.Config.maxChunkSize), 
                CeilToInt(Bounds.size.z / GrassResources.Config.maxChunkSize)
            );

            _chunkSize = float3(
                Bounds.size.x / _chunksResolution.x,
                Bounds.size.y + GrassResources.Config.chunkHeightThreshold,
                Bounds.size.z / _chunksResolution.y
            );
            
            _chunks = new GrassChunk[_chunksResolution.x * _chunksResolution.y];

            FillChunkBounds();
        }

        private void FillChunkBounds()
        {
            for (var x = 0; x < _chunksResolution.x; x++)
            {
                for (var y = 0; y < _chunksResolution.y; y++)
                {
                    var chunkId = x * _chunksResolution.y + y;

                    _chunks[chunkId].Center = float3(
                        (x + 0.5f) * _chunkSize.x + transform.position.x,
                        Bounds.size.y * 0.5f + transform.position.y,
                        (y + 0.5f) * _chunkSize.z + transform.position.z
                    );
                    
                    _chunks[chunkId].Size = _chunkSize;
                }
            }
        }
        
        internal GrassChunk[] GetRenderChunks(Camera cam)
        {
            var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(cam);
            
            var chunkPosition = GetChunkPosition(cam.transform.position);
            
            var chunks = GetChunksInChunkRange(chunkPosition, 
                int2(CeilToInt(GrassResources.Config.renderDistance / _chunkSize.x), CeilToInt(GrassResources.Config.renderDistance / _chunkSize.z))
            );
            
            FrustumCullChunks(ref chunks, frustumPlanes);

            var changed = false;
            changed |= GrassResources.Config.subdivisionDistances.Aggregate(false, (flag, dist) => 
                flag | SubdivideGrassChunks(ref chunks, cam.transform.position, dist));

            if (changed)
            {
                FrustumCullChunks(ref chunks, frustumPlanes);
            }
            
            return chunks.ToArray();
        }

        private int2 GetChunkPosition(float3 position)
        {
            return int2(float2(position.x - transform.position.x, position.z - transform.position.z) / _chunkSize.xz);
        }

        private List<GrassChunk> GetChunksInChunkRange(int2 chunkPosition, int2 chunkRange)
        {
            var rangeMin = int2(chunkPosition.x - chunkRange.x, chunkPosition.y - chunkRange.y);
            var rangeMax = int2(chunkPosition.x + chunkRange.x, chunkPosition.y + chunkRange.y);

            rangeMin.x = max(rangeMin.x, 0);
            rangeMin.y = max(rangeMin.y, 0);
            rangeMax.x = min(rangeMax.x, _chunksResolution.x - 1);
            rangeMax.y = min(rangeMax.y, _chunksResolution.y - 1);

            var result = new List<GrassChunk>(max(0, (rangeMax.x - rangeMin.y) * (rangeMax.y - rangeMin.y)));
            
            for (var x = rangeMin.x; x <= rangeMax.x; x++)
            {
                for (var y = rangeMin.y; y <= rangeMax.y; y++)
                {
                    result.Add(_chunks[x * _chunksResolution.y + y]);
                }
            }

            return result;
        }
        
        private static bool SubdivideGrassChunks(ref List<GrassChunk> chunks, float3 point, float subdivisionDistance)
        {
            var count = chunks.Count;
            var changed = false;
            
            for (var chunkId = 0; chunkId < count; chunkId++)
            {
                var chunk = chunks[chunkId];

                if (distance(point, chunk.Center) > subdivisionDistance)
                {
                    continue;
                }

                changed = true;

                var subdivisionResult = chunk.Subdivide();
                chunks[chunkId] = subdivisionResult[0];
                    
                for (var i = 1; i < subdivisionResult.Length; i++)
                {
                    chunks.Add(subdivisionResult[i]);
                }
            }
            
            return changed;
        }
        
        private static void FrustumCullChunks(ref List<GrassChunk> chunks, Plane[] frustumPlanes)
        {
            chunks = chunks.Where(chunk => GeometryUtility.TestPlanesAABB(frustumPlanes, new Bounds(chunk.Center, chunk.Size))).ToList();
        }
    }
}
