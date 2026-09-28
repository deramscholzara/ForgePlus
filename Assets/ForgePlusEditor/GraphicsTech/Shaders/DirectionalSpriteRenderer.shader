// A Marathon directional sprite, billboarded around the world Y axis. The object's forward axis is its facing,
// which picks the view as Aleph One's get_object_shape_and_transfer_mode() does (FACING4/FACING5/FACING8).
Shader "ForgePlus/DirectionalSpriteRenderer"
{
    Properties
    {
        [NoScaleOffset] _SpriteViews ("Sprite Views", 2DArray) = "" {}
        _ViewCount ("View Count", Float) = 1
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "DisableBatching" = "True"
        }

        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D_ARRAY(_SpriteViews);
        SAMPLER(sampler_SpriteViews);

        CBUFFER_START(UnityPerMaterial)
            float _ViewCount;
            float _Cutoff;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float2 uv : TEXCOORD0;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float2 uv : TEXCOORD0;
            nointerpolation float view : TEXCOORD1;
            float fogFactor : TEXCOORD2;
            float3 normalWS : TEXCOORD3;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };

        float NormalizeDegrees(float degrees)
        {
            return degrees - 360.0 * floor(degrees / 360.0);
        }

        // Marathon (x, y) is Unity (x, -z)
        float MarathonAngle(float3 directionWS)
        {
            return NormalizeDegrees(atan2(-directionWS.z, directionWS.x) * (180.0 / PI));
        }

        float SelectView(float3 originWS)
        {
            int viewCount = (int)round(_ViewCount);

            if (viewCount <= 1)
            {
                return 0.0;
            }

            float3 toObjectWS = originWS - GetCameraPositionWS();
            float3 facingWS = mul((float3x3)UNITY_MATRIX_M, float3(0.0, 0.0, 1.0));

            // theta = arctangent(object - camera) - facing
            float theta = NormalizeDegrees(MarathonAngle(toObjectWS) - MarathonAngle(facingWS));

            if (viewCount == 4)
            {
                int sector = (int)floor(NormalizeDegrees(theta - 45.0) / 90.0) % 4;
                return (float)((sector + 3) % 4);
            }

            if (viewCount == 5)
            {
                // FACING5 divides by (NUMBER_OF_ANGLES / 5) + 1 = 103 of 512 angle units
                int sector = min((int)floor(NormalizeDegrees(theta + 180.0 - 36.0) / (103.0 * 360.0 / 512.0)), 4);
                return (float)(4 - sector);
            }

            // 8 views
            int sector = (int)floor(NormalizeDegrees(theta - 22.5) / 45.0) % 8;
            return (float)((11 - sector) % 8);
        }

        Varyings BillboardVertex(Attributes input)
        {
            Varyings output = (Varyings)0;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_TRANSFER_INSTANCE_ID(input, output);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

            float3 originWS = TransformObjectToWorld(float3(0.0, 0.0, 0.0));

            // The camera's right vector, flattened so sprites stay upright
            float3 rightWS = UNITY_MATRIX_I_V._m00_m10_m20;
            rightWS.y = 0.0;
            rightWS = dot(rightWS, rightWS) > 1e-6 ? normalize(rightWS) : float3(1.0, 0.0, 0.0);

            float3 positionWS = originWS + rightWS * input.positionOS.x + float3(0.0, input.positionOS.y, 0.0);

            output.positionCS = TransformWorldToHClip(positionWS);
            output.uv = input.uv;
            output.view = SelectView(originWS);
            output.fogFactor = ComputeFogFactor(output.positionCS.z);

            output.normalWS = cross(float3(0.0, 1.0, 0.0), rightWS);

            return output;
        }

        half4 SampleSprite(Varyings input)
        {
            half4 color = SAMPLE_TEXTURE2D_ARRAY(_SpriteViews, sampler_SpriteViews, input.uv, input.view);
            clip(color.a - _Cutoff);
            return color;
        }
        ENDHLSL

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On

            HLSLPROGRAM
            #pragma vertex BillboardVertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                half4 color = SampleSprite(input);
                color.rgb = MixFog(color.rgb, input.fogFactor);
                return half4(color.rgb, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex BillboardVertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing

            half Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                SampleSprite(input);
                return input.positionCS.z;
            }
            ENDHLSL
        }

        // As URP's Unlit DepthNormalsOnly pass (UnlitDepthNormalsPass.hlsl), for SSAO
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On

            HLSLPROGRAM
            #pragma vertex BillboardVertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/PackNormalsTexture.hlsl"

            void Fragment(
                Varyings input
                , out half4 outNormalWS : SV_Target0
            #ifdef _WRITE_RENDERING_LAYERS
                , out uint outRenderingLayers : SV_Target1
            #endif
            )
            {
                UNITY_SETUP_INSTANCE_ID(input);

                SampleSprite(input);

                outNormalWS = half4(PackNormalWSToTexture(normalize(input.normalWS)), 0.0);

            #ifdef _WRITE_RENDERING_LAYERS
                outRenderingLayers = EncodeMeshRenderingLayer();
            #endif
            }
            ENDHLSL
        }
    }

    FallBack Off
}
