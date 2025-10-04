#ifndef _GRASS_LIGHTING_INCLUDED
#define _GRASS_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

half3 CalculateLightSubsurface(Light light, half3 normalWS, half3 viewDirectionWS, float thickness)
{
    half NdotL = saturate(-dot(normalWS, light.direction));
    
    half3 radiance = light.color * (light.distanceAttenuation * NdotL);

    return radiance;
}

half4 GrassSubsurfaceLighting(InputData inputData, SurfaceData surfaceData, float thickness)
{
    half4 shadowMask = CalculateShadowMask(inputData);
    AmbientOcclusionFactor aoFactor = CreateAmbientOcclusionFactor(inputData, surfaceData);
    Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, shadowMask);
    
    float3 color = 0;
    
    color += CalculateLightSubsurface(mainLight, inputData.normalWS, inputData.viewDirectionWS, thickness);
    
    return float4(color * surfaceData.albedo, 1);
}

#endif