#ifndef FORGEPLUS_READ_ALEPH_ONE_BUMP_MAP_INCLUDED
#define FORGEPLUS_READ_ALEPH_ONE_BUMP_MAP_INCLUDED

// An Aleph One bump map (MML's offset_image): its tangent-space normal in its color, decoded as Aleph One's bump shader
// decodes it (bump.frag: (rgb - 0.5) * 2), and its height in its opacity (1 at the top). Once ForgePlus has loaded it (bottom
// row first, as Unity textures are), its normals follow Unity's convention (green toward +V), so they're used as they are.
// It's sampled as a plain texture: Unity's normal unpacking (UnpackNormal) would read the normal's X from the opacity.
void ReadAlephOneBumpMap_float(UnityTexture2D BumpMap, float2 UV, out float3 NormalTS, out float Height)
{
    float4 packed = SAMPLE_TEXTURE2D(BumpMap.tex, BumpMap.samplerstate, BumpMap.GetTransformedUV(UV));

    NormalTS = SafeNormalize(packed.rgb * 2.0 - 1.0);
    Height = packed.a;
}

void ReadAlephOneBumpMap_half(UnityTexture2D BumpMap, half2 UV, out half3 NormalTS, out half Height)
{
    half4 packed = SAMPLE_TEXTURE2D(BumpMap.tex, BumpMap.samplerstate, BumpMap.GetTransformedUV(UV));

    NormalTS = SafeNormalize(packed.rgb * 2.0 - 1.0);
    Height = packed.a;
}

#endif
