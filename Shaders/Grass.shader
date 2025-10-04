Shader "Rendering/Grass"
{
    Properties
    {
        _Color("Albedo", Color) = (0, 1, 0, 1)
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }
        
        LOD 300
        
        HLSLINCLUDE

        #define TAU 6.283185307
        #define PI 3.14
        #define HALF_PI 1.57
        
        #include "BufferTypes.hlsl"
        #include "Random.hlsl"

        StructuredBuffer<GrassArtisticParameters> _ArtisticBuffer;
        StructuredBuffer<GrassData> _GrassBuffer;

        void get_curve_controls(out float3 p0, out float3 p1, out float3 p2, float3 bitangent, float tilt, float bend)
        {
            float3x3 curveRotMatrix = float3x3(
                bitangent.z,  0, bitangent.x,
                0,         1, 0,
                -bitangent.x, 0, bitangent.z
            );
            
            p0 = 0;
            p2 = mul(float3(tilt, sqrt(1 - tilt * tilt), 0), curveRotMatrix);
            p1 = lerp(lerp(p0, p2, 0.3), float3(0, 1, 0), bend);
        }

        void get_curve_point(float3 p0, float3 p1, float3 p2, float t, out float3 pos, out float3 tan)
        {
            float omt = 1 - t, ts = t * t, omts = omt * omt;
                    
            pos = omts * p0 + 2 * omt * t * p1 + ts * p2;
            tan = 2 * (omt * (p1 - p0) + t * (p2 - p1));
        }

        void transform_by_bezier2(inout float3 position, inout float3 normal, inout float3 tangent, float tilt, float bend, float3 bitangent, float2 bladeBitangent, float t, float width)
        {
            float3 p0, p1, p2, bezierPos, bezierTan;
            get_curve_controls(p0, p1, p2, float3(bladeBitangent.x, 0, bladeBitangent.y), tilt, bend);
            get_curve_point(p0, p1, p2, t, bezierPos, bezierTan);

            position = bezierPos + position.x * bitangent * width;
            tangent = bezierTan;
            normal = -normalize(cross(bezierTan, float3(bladeBitangent.x, 0, bladeBitangent.y)));
        }

        void rotate_towards_camera(inout float3 bitangent, float3 positionOffset, float3 cameraPosition, float rotationStrength)
        {
            float2 viewDir = normalize((positionOffset - cameraPosition).xz);

            float3 cameraBitangent = float3(-viewDir.y, 0, viewDir.x);
            float VdotB = dot(bitangent, cameraBitangent);
            
            if (VdotB < 0)
            {
                cameraBitangent = -cameraBitangent;
            }
            
            bitangent = lerp(bitangent, cameraBitangent,  (1 - pow(1 - abs(VdotB), 3)) * rotationStrength);
        }

        float4 _WindConfig;

        void apply_wind_bobbing_octave(inout float3 position, float3 normal, float windIntensity, float phaseOffset, float curveOffset, float strength, float speed, float waveLength, float cutoff)
        {
            position += strength * normal * pow(curveOffset, cutoff) * sin(phaseOffset + _WindConfig.x * speed + curveOffset * PI / waveLength) * windIntensity;
        }

        void apply_wind_bobbing(inout float3 position, float curveOffset, float3 cameraPosition, float3 positionOffset, float3 normal, float windIntensity, float phaseOffset, float2 bobbingDistanceCutoff, float2 strength, float2 speed, float2 waveLength, float2 cutoff)
        {
            float dist = distance(cameraPosition, positionOffset);
            strength *= 1 - saturate((dist - bobbingDistanceCutoff.x) / (bobbingDistanceCutoff.y - bobbingDistanceCutoff.x));
            
            apply_wind_bobbing_octave(position, normal, windIntensity, phaseOffset, curveOffset, strength.x, speed.x, waveLength.x, cutoff.x);
            apply_wind_bobbing_octave(position, normal, windIntensity, phaseOffset, curveOffset, strength.y, speed.y, waveLength.y, cutoff.y);
        }

        void rotate_around_tan(inout float3 normal, float3 tan, float factor, float NdotV)
        {
            if (NdotV > 0)
            {
                factor = -factor;
            }

            float2 rot;
            sincos(factor * HALF_PI, rot.x, rot.y);

            normal = normal * rot.y + cross(tan, normal) * rot.x + tan * dot(tan, normal) * (1.0 - rot.y);
        }
        
        ENDHLSL
        
        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            ZTest LEqual
            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0

            #pragma vertex GrassVertex
            #pragma fragment GrassFragment

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _FORWARD_PLUS
            
            #include "GrassLighting.hlsl"

            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer

            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityIndirect.cginc"
            
            float4 _Color;

            struct Attributes
            {
                float4 positionOS           : POSITION;
                float3 normalOS             : NORMAL;
                float4 tangentOS            : TANGENT;
                float2 texcoord             : TEXCOORD0;
                float2 staticLightmapUV     : TEXCOORD1;
                float2 dynamicLightmapUV    : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv                   : TEXCOORD0;
                float4 positionCS           : SV_POSITION;
                float3 positionWS           : TEXCOORD1;
                float3 normalWS             : TEXCOORD2;
                float3 tangentWS            : TEXCOORD4;
                float4 shadowCoord          : TEXCOORD5;
                float2 staticLightmapUV     : TEXCOORD6;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings GrassVertex(Attributes input, uint svInstanceID : SV_InstanceID)
            {
                InitIndirectDrawArgs(0);
                Varyings output = (Varyings)0;
            
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                uint instanceID = GetIndirectInstanceID(svInstanceID);

                GrassData blade = _GrassBuffer[instanceID];
                GrassArtisticParameters parameters = _ArtisticBuffer[blade.type];

                float3 bitangent = float3(blade.bitangent.x, 0, blade.bitangent.y);

                rotate_towards_camera(bitangent, blade.position, _WorldSpaceCameraPos, parameters.rotateTowardsCameraStrength);
                transform_by_bezier2(input.positionOS.xyz, input.normalOS.xyz, input.tangentOS.xyz, blade.tilt, blade.bend, bitangent, blade.bitangent, input.texcoord.y, blade.width);
                apply_wind_bobbing(input.positionOS.xyz, input.texcoord.y, _WorldSpaceCameraPos, blade.position, input.normalOS.xyz, blade.windIntensity, blade.phaseOffset, parameters.bobbingDistanceCutoff, parameters.bobbingStrength, parameters.bobbingSpeed, parameters.bobbingWavelength, parameters.bobbingCutoff);

                input.positionOS.xyz *= blade.scale;
                input.positionOS.xyz += blade.position;
            
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                
                output.uv = input.texcoord;
                output.normalWS = normalInput.normalWS;
                output.tangentWS = normalInput.tangentWS;
                OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
                output.positionWS = vertexInput.positionWS;
                output.positionCS = vertexInput.positionCS;
            
                return output;
            }

            float4 GrassFragment(Varyings input) : SV_Target
            {
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

                float NdotV = dot(input.normalWS, viewDirWS);
                if (NdotV < 0)
                {
                    input.normalWS = -input.normalWS;
                }
                float rotFactor = pow(distance(input.uv.x, 0.5) * 2, 7);
                if (input.uv.x < 0.5) {
                    rotFactor = -rotFactor;
                }
                rotate_around_tan(input.normalWS, input.tangentWS, rotFactor, NdotV);
                
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = _Color.rgb;
                surfaceData.alpha = half(1.0);
                surfaceData.metallic = 0;
                surfaceData.smoothness = 0.6;
                surfaceData.occlusion = half(1.0);
                surfaceData.specular = 0;
                
                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = input.normalWS;
                inputData.viewDirectionWS = viewDirWS;
                inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, 0, inputData.normalWS);
                inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
                
                return UniversalFragmentPBR(inputData, surfaceData);
            }
            
            ENDHLSL
        }
    }
}
