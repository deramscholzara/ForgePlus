#ifndef FORGEPLUS_LIGHT_INTENSITIES_INCLUDED
#define FORGEPLUS_LIGHT_INTENSITIES_INCLUDED

StructuredBuffer<float> _LightIntensities;

// The index arrives through a UV, which can hold slightly off its integer value (such as 10.9999),
// so it's rounded rather than truncated, or a surface's vertices can read different lights
void GetLightIntensity_float(float LightIndex, out float Intensity)
{
    Intensity = _LightIntensities[(uint)round(LightIndex)];
}

void GetLightIntensity_half(half LightIndex, out half Intensity)
{
    Intensity = _LightIntensities[(uint)round(LightIndex)];
}
#endif