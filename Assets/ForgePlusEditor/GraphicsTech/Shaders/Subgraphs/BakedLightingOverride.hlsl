#ifndef FORGEPLUS_BAKED_LIGHTING_OVERRIDE_INCLUDED
#define FORGEPLUS_BAKED_LIGHTING_OVERRIDE_INCLUDED

// Replaces URP's baked GI (lightmaps, light probes, probe volumes or ambient) with a value from the graph, or adds it to
// Surface Cache Global Illumination's while that's on, so it lights Base Color as baked lighting would: diffusely, with
// occlusion and SSAO, alongside realtime lights and reflections.
//
// The graph's surface function runs before URP's Forward and GBuffer passes ask for baked GI (InitializeBakedGI, in
// PBRForwardPass.hlsl and PBRGBufferPass.hlsl, which are included after the graph's functions), so the value is kept
// in a static (per pixel) variable by SetBakedLighting, and InitializeBakedGI is redefined here to return it. This
// depends on URP's internal pass code (URP 17's InitializeBakedGI and GIParams), so check it after URP upgrades.

// Per pixel; zero (no baked light) unless SetBakedLighting sets it
static float3 ForgePlus_BakedLighting = float3(0.0, 0.0, 0.0);

#if defined(UNIVERSAL_GLOBAL_ILLUMINATION_INCLUDED) && defined(SHADERPASS) && \
    ((defined(SHADERPASS_FORWARD) && SHADERPASS == SHADERPASS_FORWARD) || (defined(SHADERPASS_GBUFFER) && SHADERPASS == SHADERPASS_GBUFFER))

// URP's own InitializeBakedGI still runs (for the shadow mask). Its baked GI (lightmaps, probes or ambient) is replaced,
// except while Surface Cache Global Illumination supplies it (as screen-space irradiance), whose bounced light is kept
// and added to.
void ForgePlus_InitializeBakedGI(GIParams giParams, out URP_LIGHT_ACCUM3 bakedGI, out half4 shadowMask)
{
    InitializeBakedGI(giParams, bakedGI, shadowMask);

#if defined(_SCREEN_SPACE_IRRADIANCE)
    bakedGI += (URP_LIGHT_ACCUM3) ForgePlus_BakedLighting;
#else
    bakedGI = (URP_LIGHT_ACCUM3) ForgePlus_BakedLighting;
#endif
}

// Defined after the function above, so only the passes' later calls are redirected
#define InitializeBakedGI(giParams, bakedGI, shadowMask) ForgePlus_InitializeBakedGI(giParams, bakedGI, shadowMask)

#endif

// Base Color passes through unchanged; Lighting becomes the surface's baked lighting
void SetBakedLighting_float(float3 BaseColor, float3 Lighting, out float3 Out)
{
    ForgePlus_BakedLighting = Lighting;
    Out = BaseColor;
}

void SetBakedLighting_half(half3 BaseColor, half3 Lighting, out half3 Out)
{
    ForgePlus_BakedLighting = Lighting;
    Out = BaseColor;
}

#endif
