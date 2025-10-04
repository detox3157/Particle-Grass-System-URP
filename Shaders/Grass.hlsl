#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"

#include "BufferTypes.hlsl"
#include "Random.hlsl"
#include "GrassLighting.hlsl"

UNITY_INSTANCING_BUFFER_START(instancingProperties)
UNITY_DEFINE_INSTANCED_PROP(StructuredBuffer<GrassArtisticParameters>, _ArtisticBuffer)
UNITY_DEFINE_INSTANCED_PROP(StructuredBuffer<GrassData>, _GrassBuffer)
UNITY_INSTANCING_BUFFER_END(instancingProperties)

float4 _WindConfig;

void apply_wind_bobbing_octave(inout float3 position, float3 normal, float windIntensity, float phaseOffset, float curveOffset, float strength, float speed, float waveLength, float cutoff)
{
    position += strength * normal * pow(curveOffset, cutoff) * sin(phaseOffset + _WindConfig.x * speed + curveOffset * PI / waveLength) * windIntensity;
}

void get_bezier2_controls(float3 bitangent, float tilt, float bend, float controlPointOffset, out float3 p0, out float3 p1, out float3 p2)
{
    float3x3 curveRotMatrix = float3x3(
        bitangent.z,  0, -bitangent.x,
        0,         1, 0,
        bitangent.x, 0, bitangent.z
    );

    p0 = 0;
    p2 = mul(float3(tilt, sqrt(1 - tilt * tilt), 0), curveRotMatrix);
    p1 = lerp(lerp(p0, p2, controlPointOffset), float3(0, 1, 0), bend);
}

void get_bezier2_point(float3 p0, float3 p1, float3 p2, float3 bitangent, float curvePosition, out float3 bezierPos, out float3 bezierNrm)
{
    float omt = 1 - curvePosition, ts = curvePosition * curvePosition, omts = omt * omt;
                            
    bezierPos = omts * p0 + 2 * omt * curvePosition * p1 + ts * p2;
    float3 bezierTan = normalize(2 * (omt * (p1 - p0) + curvePosition * (p2 - p1)));
    bezierNrm = -normalize(cross(bezierTan, bitangent));
}

void GetBladeData_float(in float instanceID, out float3 positionOffset, out float2 bitangent, out float tilt, out float bend, out float scale, out float width, out float windIntensity, out float phaseOffset, out float type, out float4 rand)
{
    GrassData blade = UNITY_ACCESS_INSTANCED_PROP(instancingProperties, _GrassBuffer)[instanceID];

    positionOffset = blade.position;
    bitangent = blade.bitangent;
    tilt = blade.tilt;
    bend = blade.bend;
    scale = blade.scale;
    width = blade.width;
    windIntensity = blade.windIntensity;
    phaseOffset = blade.phaseOffset;
    type = blade.type;
    rand = HashToFloat4(blade.hash);
}

void GetBladeArtisticParameters_float(in float type, out float size, out float2 widthRange, out float jitterStrength, out float clumpingStrength, out float clumpSize, out float2 sizeRange, out float2 tiltRange, out float2 bendRange, out float rotationRange, out float rotateTowardsCameraStrength, out float followWindDirectionStrength, out float2 movementDistanceCutoff, out float2 bobbingDistanceCutoff, out float2 bobbingStrength, out float2 bobbingSpeed, out float2 bobbingWavelength, out float2 bobbingCutoff, out float3 tintBottomColor, out float3 tintTopColor, out float3 tintVariationColorA, out float3 tintVariationColorB)
{
    GrassArtisticParameters params = UNITY_ACCESS_INSTANCED_PROP(instancingProperties, _ArtisticBuffer)[type];

    size = params.size;
    widthRange = params.widthRange;
    jitterStrength = params.jitterStrength;
    clumpingStrength = params.clumpingStrength;
    clumpSize = params.clumpSize;
    sizeRange = params.sizeRange;
    tiltRange = params.tiltRange;
    bendRange = params.bendRange;
    rotationRange = params.rotationRange;
    rotateTowardsCameraStrength = params.rotateTowardsCameraStrength;
    followWindDirectionStrength = params.followWindDirectionStrength;
    movementDistanceCutoff = params.movementDistanceCutoff;
    bobbingDistanceCutoff = params.bobbingDistanceCutoff;
    bobbingStrength = params.bobbingStrength;
    bobbingSpeed = params.bobbingSpeed;
    bobbingWavelength = params.bobbingWavelength;
    bobbingCutoff = params.bobbingCutoff;
    tintBottomColor = params.tintBottomColor;
    tintTopColor = params.tintTopColor;
    tintVariationColorA = params.tintVariationColorA;
    tintVariationColorB = params.tintVariationColorB;
}

void TransformByBezier2_float(in float posXOffset, in float curveOffset, in float2 bladeBitangent, in float2 bitangent, in float tilt, in float bend, in float width, in float controlPointOffset, out float3 position, out float3 normal)
{
    float3 p0, p1, p2;
    float3 bezierPos, bezierNrm;
    float3 bladeBitangentXYZ = float3(bladeBitangent.x, 0, bladeBitangent.y);
    
    get_bezier2_controls(bladeBitangentXYZ, tilt, bend, controlPointOffset, p0, p1, p2);
    get_bezier2_point(p0, p1, p2, bladeBitangentXYZ, curveOffset, bezierPos, bezierNrm);

    position = bezierPos + float3(bitangent.x, 0, bitangent.y) * posXOffset * width;
    normal = bezierNrm;
}

void ApplyWindBobbing_float(in float curveOffset, in float3 cameraPosition, in float3 positionOffset, in float3 positionIn, in float3 normal, in float windIntensity, in float phaseOffset, in float2 bobbingDistanceCutoff, in float2 strength, in float2 speed, in float2 waveLength, in float2 cutoff, out float3 positionOut)
{
    positionOut = positionIn;

    float dist = distance(cameraPosition, positionOffset);
    strength *= 1 - saturate((dist - bobbingDistanceCutoff.x) / (bobbingDistanceCutoff.y - bobbingDistanceCutoff.x));
    
    apply_wind_bobbing_octave(positionOut, normal, windIntensity, phaseOffset, curveOffset, strength.x, speed.x, waveLength.x, cutoff.x);
    apply_wind_bobbing_octave(positionOut, normal, windIntensity, phaseOffset, curveOffset, strength.y, speed.y, waveLength.y, cutoff.y);
}

void TransformToWorldPos_float(in float3 positionIn, in float3 positionOffset, in float scale, out float3 positionOut)
{
    positionOut = positionIn * scale + positionOffset;
}

void RotateTowardsCamera_float(in float2 bitangentIn, in float3 positionOffset, in float3 cameraPosition, in float rotationStrength, out float2 bitangentOut)
{
    float2 viewDir = normalize((positionOffset - cameraPosition).xz);

    float2 cameraBitangent = float2(-viewDir.y, viewDir.x);
    float VdotB = dot(bitangentIn, cameraBitangent);
    
    if (VdotB < 0)
    {
        cameraBitangent = -cameraBitangent;
    }
    
    bitangentOut = lerp(bitangentIn, cameraBitangent,  (1 - pow(1 - abs(VdotB), 3)) * rotationStrength);
}

void GrassLighting_float(in float4 mask, in float3 baseColor, in float3 positionWS, in float2 staticLightmapUV, out float3 color)
{
    color = GrassLighting(baseColor, positionWS, mask, staticLightmapUV).rgb;
}