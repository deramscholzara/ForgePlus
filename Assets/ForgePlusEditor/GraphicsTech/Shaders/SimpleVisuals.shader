// Every surface drawn with the grid texture (SimpleVisuals), lit as the textured surfaces are: its light's intensity
// (UV0.z indexes _LightIntensities) plus a side's ambient delta (UV4.x), raised toward full by _GlobalMinimumLight (which
// Show Lighting being off sets to full). Texture coordinates are the surface's own (UV0.xy), without its transfer mode's
// motion.
Shader "ForgePlus/SimpleVisuals"
{
    Properties
    {
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Cull Back

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_ForgePlusGridTexture);
        SAMPLER(sampler_ForgePlusGridTexture);

        StructuredBuffer<float> _LightIntensities;
        float _GlobalMinimumLight;

        struct Attributes
        {
            float4 positionOS : POSITION;
            float4 uv0 : TEXCOORD0;
            float4 uv4 : TEXCOORD4;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            float lightIntensity : TEXCOORD1;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        Varyings Vertex(Attributes input)
        {
            Varyings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            output.uv = input.uv0.xy;

            // The index arrives through a UV, which can hold slightly off its integer value, so it's rounded
            float intensity = saturate(_LightIntensities[(uint) round(input.uv0.z)] + input.uv4.x);
            output.lightIntensity = lerp(_GlobalMinimumLight, 1.0, intensity);

            return output;
        }
        ENDHLSL

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing

            half4 Fragment(Varyings input) : SV_Target
            {
                half3 color = SAMPLE_TEXTURE2D(_ForgePlusGridTexture, sampler_ForgePlusGridTexture, input.uv).rgb;

                return half4(color * input.lightIntensity, 1.0);
            }
            ENDHLSL
        }

        // So effects that read the depth (such as ambient occlusion) see the surfaces
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ColorMask R

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment DepthFragment
            #pragma multi_compile_instancing

            half DepthFragment(Varyings input) : SV_Target
            {
                return input.positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            HLSLPROGRAM
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_instancing

            struct NormalAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct NormalVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            NormalVaryings DepthNormalsVertex(NormalAttributes input)
            {
                NormalVaryings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);

                return output;
            }

            half4 DepthNormalsFragment(NormalVaryings input) : SV_Target
            {
                return half4(NormalizeNormalPerPixel(input.normalWS), 0.0);
            }
            ENDHLSL
        }
    }
}
