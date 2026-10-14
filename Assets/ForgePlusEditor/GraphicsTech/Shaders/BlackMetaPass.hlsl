#ifndef FORGEPLUS_BLACK_META_PASS_INCLUDED
#define FORGEPLUS_BLACK_META_PASS_INCLUDED

// A Meta pass with no albedo and no emission, for shaders whose renderers shouldn't add to or reflect Surface Cache Global
// Illumination's light (it needs every material it's given to have a Meta pass). Placed as URP's own Meta passes are
// (UniversalMetaPass.hlsl). Used as:
//
//     Pass
//     {
//         Name "Meta"
//         Tags { "LightMode" = "Meta" }
//
//         Cull Off
//
//         HLSLPROGRAM
//         #pragma vertex BlackMetaVertex
//         #pragma fragment BlackMetaFragment
//         #include "Assets/ForgePlusEditor/GraphicsTech/Shaders/BlackMetaPass.hlsl"
//         ENDHLSL
//     }

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

struct BlackMetaAttributes
{
    float4 positionOS : POSITION;
    float2 uv1 : TEXCOORD1;
    float2 uv2 : TEXCOORD2;
};

struct BlackMetaVaryings
{
    float4 positionCS : SV_POSITION;
};

BlackMetaVaryings BlackMetaVertex(BlackMetaAttributes input)
{
    BlackMetaVaryings output;
    output.positionCS = UnityMetaVertexPosition(input.positionOS.xyz, input.uv1, input.uv2);

    return output;
}

half4 BlackMetaFragment(BlackMetaVaryings input) : SV_Target
{
    MetaInput metaInput = (MetaInput)0;

    return UnityMetaFragment(metaInput);
}

#endif
