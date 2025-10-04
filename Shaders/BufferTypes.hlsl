struct GrassData
{
  float3 position;
  float2 bitangent;
  float tilt;
  float bend;
  float scale;
  float width;
  float windIntensity;
  float phaseOffset;
  uint type;
  uint hash;
};

struct GrassArtisticParameters
{
  float size;
  float2 widthRange;
  float jitterStrength;
  float clumpingStrength;
  float clumpSize;
  float2 sizeRange;
  float2 tiltRange;
  float2 bendRange;
  float rotationRange;
  float rotateTowardsCameraStrength;
  float followWindDirectionStrength;
  float2 movementDistanceCutoff;
  float2 bobbingDistanceCutoff;
  float2 bobbingStrength;
  float2 bobbingSpeed;
  float2 bobbingWavelength;
  float2 bobbingCutoff;

  float3 tintBottomColor;
  float3 tintTopColor;
  float3 tintVariationColorA;
  float3 tintVariationColorB;
};