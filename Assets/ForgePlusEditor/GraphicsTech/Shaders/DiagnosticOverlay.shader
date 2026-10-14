// A flat, see-through color drawn over a surface (sharing its mesh), for marking problems in the level view
// (DiagnosticVisuals). It's pulled toward the camera, so it's drawn over the surface it shares a mesh with.
Shader "ForgePlus/DiagnosticOverlay"
{
    Properties
    {
        _Color ("Color", Color) = (0.75, 0, 0, 0.75)
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
        Offset -1, -1
        Blend SrcAlpha OneMinusSrcAlpha

        // Shared by every pass, so they agree on the material's properties (as the SRP Batcher needs)
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _Color;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "Unlit"

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                return _Color;
            }
            ENDHLSL
        }

        // Black, so Surface Cache Global Illumination can take it without it adding or reflecting light
        Pass
        {
            Name "Meta"
            Tags { "LightMode" = "Meta" }

            Cull Off
            Blend Off
            Offset 0, 0

            HLSLPROGRAM
            #pragma vertex BlackMetaVertex
            #pragma fragment BlackMetaFragment
            #include "Assets/ForgePlusEditor/GraphicsTech/Shaders/BlackMetaPass.hlsl"
            ENDHLSL
        }
    }
}
