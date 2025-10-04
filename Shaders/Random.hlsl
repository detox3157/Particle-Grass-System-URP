#ifndef RANDOM_INCLUDED
#define RANDOM_INCLUDED

uint Hash(uint3 seed)
{
    const uint m = 1664525u;
    const uint c = 1013904223u;
    
    uint3 state = seed * m + c;
    state = state ^ state >> 16u;
    state *= m;
    state += c;
    state = state ^ state >> 16u;
    
    return state.x ^ state.y ^ state.z;
}

float4 HashToFloat4(uint hash)
{
    return float4(hash & 255u, hash >> 8 & 255u, hash >> 16 & 255u, hash >> 24 & 255u) / 255.0;
}

float3 HashToFloat3(uint hash)
{
    return float3(hash & 1023u, hash >> 8 & 1023u, hash >> 16 & 1023u) / 1023.0;
}

float2 HashToFloat2(uint hash)
{
    return float2(hash & 65535u, hash >> 16 & 65535u) / 65535.0;
}

#endif