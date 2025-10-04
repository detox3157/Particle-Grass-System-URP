using UnityEngine;
using Unity.Mathematics;

using static Unity.Mathematics.math;
using float3 = Unity.Mathematics.float3;

namespace ParticleGrass
{
    internal struct GrassChunk
    {
        internal float3 Center;
        internal float3 Size;
    }

    internal static class GrassChunkExtensions
    {
        internal static GrassChunk[] Subdivide(this GrassChunk chunk)
        {
            var result = new GrassChunk[4];

            var offsetX = float3(chunk.Size.x * 0.25f, 0, 0);
            var offsetZ = float3(0, 0 , chunk.Size.z * 0.25f);
            
            var newSize = float3(chunk.Size.x * 0.5f, chunk.Size.y, chunk.Size.z * 0.5f);

            for (var x = 0; x <= 1; x++)
            {
                for (var y = 0; y <= 1; y++)
                {
                    result[x * 2 + y] = new GrassChunk
                    {
                        Center = chunk.Center + offsetX * (x > 0 ? 1 : -1) + offsetZ * (y > 0 ? 1 : -1),
                        Size = newSize
                    };
                }
            }

            return result;
        }
    }
}