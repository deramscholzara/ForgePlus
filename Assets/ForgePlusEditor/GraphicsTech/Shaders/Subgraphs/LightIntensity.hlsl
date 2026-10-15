#ifndef FORGEPLUS_LIGHT_INTENSITY_INCLUDED
#define FORGEPLUS_LIGHT_INTENSITY_INCLUDED

#include "Assets/ForgePlusEditor/GraphicsTech/Shaders/Subgraphs/MetaPassSwitch.hlsl"

// Every light's display intensity, set each frame its lights change (LevelEntity_Light: the length must match
// LightIntensitiesLength). The last entry is always full intensity, for surfaces with no light. Full everywhere outside
// Play mode, so previews are lit.
float _LightIntensities[1024];

// A surface's light intensity: by the _LightIndex its material holds, each frame, in the passes people see; and in the Meta
// pass, Surface Cache Global Illumination's _LightIntensity, which changes far less often (each change re-renders the
// material for it)
void LightIntensity_float(float LightIndex, float SurfaceCacheIntensity, out float Out)
{
#ifdef FORGEPLUS_IS_META_PASS
    Out = SurfaceCacheIntensity;
#else
    Out = _LightIntensities[min((uint) LightIndex, 1023u)];
#endif
}

void LightIntensity_half(half LightIndex, half SurfaceCacheIntensity, out half Out)
{
#ifdef FORGEPLUS_IS_META_PASS
    Out = SurfaceCacheIntensity;
#else
    Out = _LightIntensities[min((uint) LightIndex, 1023u)];
#endif
}

#endif
