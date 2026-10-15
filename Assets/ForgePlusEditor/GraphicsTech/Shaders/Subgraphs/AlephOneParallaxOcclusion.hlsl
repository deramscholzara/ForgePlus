#ifndef FORGEPLUS_ALEPH_ONE_PARALLAX_OCCLUSION_INCLUDED
#define FORGEPLUS_ALEPH_ONE_PARALLAX_OCCLUSION_INCLUDED

// Shader Graph's Parallax Occlusion Mapping node (ParallaxOcclusionMappingNode, and the PerPixelDisplacement.hlsl it
// includes), reading height from an Aleph One bump map's opacity (MML's offset_image: its normal is in its color). It's
// written out here, rather than including PerPixelDisplacement.hlsl, so its names can't clash with a Parallax Occlusion
// Mapping node's in the same graph. The heightmap's own sampler is used.

// As the node's GetDisplacementObjectScale
float3 ForgePlus_AlephOneDisplacementObjectScale()
{
    float3 objectScale = float3(1.0, 1.0, 1.0);
    float4x4 worldTransform = GetWorldToObjectMatrix();

    objectScale.x = length(float3(worldTransform._m00, worldTransform._m01, worldTransform._m02));
    objectScale.z = length(float3(worldTransform._m20, worldTransform._m21, worldTransform._m22));

    return objectScale;
}

float ForgePlus_AlephOneHeight(UnityTexture2D heightmap, float2 uv, float lod)
{
    return SAMPLE_TEXTURE2D_LOD(heightmap.tex, heightmap.samplerstate, uv, lod).a;
}

// As PerPixelDisplacement.hlsl's ParallaxOcclusionMapping (with its secant method): the offset to apply to the UVs
float2 ForgePlus_AlephOneParallaxOcclusionMapping(float lod, float lodThreshold, int numSteps, float3 viewDirTS, float2 uv, UnityTexture2D heightmap, out float outHeight)
{
    // Convention: 1.0 is top, 0.0 is bottom - POM is always inward, no extrusion
    float stepSize = 1.0 / (float)numSteps;

    // View vector is from the point to the camera, but we want to raymarch from camera to point, so reverse the sign
    float2 parallaxMaxOffsetTS = (viewDirTS.xy / -viewDirTS.z);
    float2 texOffsetPerStep = stepSize * parallaxMaxOffsetTS;

    // Do a first step before the loop to init all value correctly
    float2 texOffsetCurrent = float2(0.0, 0.0);
    float prevHeight = ForgePlus_AlephOneHeight(heightmap, uv + texOffsetCurrent, lod);
    texOffsetCurrent += texOffsetPerStep;
    float currHeight = ForgePlus_AlephOneHeight(heightmap, uv + texOffsetCurrent, lod);
    float rayHeight = 1.0 - stepSize; // Start at top less one sample

    // Linear search
    for (int stepIndex = 0; stepIndex < numSteps; ++stepIndex)
    {
        // Have we found a height below our ray height ? then we have an intersection
        if (currHeight > rayHeight)
            break; // end the loop

        prevHeight = currHeight;
        rayHeight -= stepSize;
        texOffsetCurrent += texOffsetPerStep;

        currHeight = ForgePlus_AlephOneHeight(heightmap, uv + texOffsetCurrent, lod);
    }

    // Found below and above points, now perform line interesection (ray) with piecewise linear heightfield approximation,
    // refined with the secant method (Faster Relief Mapping Using the Secant Method - Eric Risser)
    float pt0 = rayHeight + stepSize;
    float pt1 = rayHeight;
    float delta0 = pt0 - prevHeight;
    float delta1 = pt1 - currHeight;

    float delta;
    float2 offset;

    for (int i = 0; i < 3; ++i)
    {
        // intersectionHeight is the height [0..1] for the intersection between view ray and heightfield line
        float intersectionHeight = (pt0 * delta1 - pt1 * delta0) / (delta1 - delta0);
        // Retrieve offset require to find this intersectionHeight
        offset = (1 - intersectionHeight) * texOffsetPerStep * numSteps;

        currHeight = ForgePlus_AlephOneHeight(heightmap, uv + offset, lod);

        delta = intersectionHeight - currHeight;

        if ((delta >= -0.01) && (delta <= 0.01))
            break;

        // intersectionHeight < currHeight => new lower bounds
        if (delta < 0.0)
        {
            delta1 = delta;
            pt1 = intersectionHeight;
        }
        else
        {
            delta0 = delta;
            pt0 = intersectionHeight;
        }
    }

    outHeight = currHeight;

    // Fade the effect with lod (allow to avoid pop when switching to a discrete LOD mesh)
    offset *= (1.0 - saturate(lod - lodThreshold));

    return offset;
}

// The node's inputs and outputs (its View Direction in tangent space, which the node reads itself, is an input here).
// Amplitude is in centimeters, as the node's is.
void AlephOneParallaxOcclusion_float(UnityTexture2D Heightmap, float Amplitude, float Steps, float2 UVs, float2 Tiling, float2 Offset, float2 PrimitiveSize, float LOD, float LODThreshold, float3 ViewDirTS, out float PixelDepthOffset, out float2 ParallaxUVs)
{
    float3 viewDir = ViewDirTS * ForgePlus_AlephOneDisplacementObjectScale().xzy;
    float NdotV = viewDir.z;
    float maxHeight = Amplitude * 0.01; // cm in the interface so we multiply by 0.01 in the shader to convert in meter
    maxHeight *= 2.0 / (abs(Tiling.x) + abs(Tiling.y)); // reduce height based on the tiling values

    float2 uvSpaceScale = maxHeight * Tiling / PrimitiveSize;

    // Transform the view vector into the UV space.
    float3 viewDirUV = normalize(float3(viewDir.xy * uvSpaceScale, viewDir.z));

    float2 uv = Heightmap.GetTransformedUV(UVs * Tiling + Offset);

    // to avoid crashes when steps gets too big, and to avoid divide by zero, it's clamped to the range [1, 256]
    float outHeight;
    ParallaxUVs = uv + ForgePlus_AlephOneParallaxOcclusionMapping(LOD, LODThreshold, (int)max(min(Steps, 256), 1), viewDirUV, uv, Heightmap, outHeight);

    PixelDepthOffset = (maxHeight - outHeight * maxHeight) / max(NdotV, 0.0001);
}

void AlephOneParallaxOcclusion_half(UnityTexture2D Heightmap, half Amplitude, half Steps, half2 UVs, half2 Tiling, half2 Offset, half2 PrimitiveSize, half LOD, half LODThreshold, half3 ViewDirTS, out half PixelDepthOffset, out half2 ParallaxUVs)
{
    float pixelDepthOffset;
    float2 parallaxUVs;
    AlephOneParallaxOcclusion_float(Heightmap, Amplitude, Steps, UVs, Tiling, Offset, PrimitiveSize, LOD, LODThreshold, ViewDirTS, pixelDepthOffset, parallaxUVs);

    PixelDepthOffset = pixelDepthOffset;
    ParallaxUVs = parallaxUVs;
}

#endif
