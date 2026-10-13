// The grid drawn under the level in the orthographic view (OrthographicGrid): the grid texture repeated once per world
// unit across the level (from the world's position, so it's aligned to world units wherever it's drawn), see-through by
// its opacity, and unlit.
Shader "ForgePlus/OrthographicGrid"
{
    Properties
    {
        _MainTex ("Grid", 2D) = "white" {}
        _Opacity ("Opacity", Range(0, 1)) = 0.4
        _MetersPerWorldUnit ("Meters per world unit", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Unlit"

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half _Opacity;
                float _MetersPerWorldUnit;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);

                // Along the world's X and Z, so the texture's up is the map's up (the top of the orthographic view)
                output.uv = float2(positionWS.x, positionWS.z) / _MetersPerWorldUnit;

                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                half4 grid = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                return half4(grid.rgb, grid.a * _Opacity);
            }
            ENDHLSL
        }
    }
}
