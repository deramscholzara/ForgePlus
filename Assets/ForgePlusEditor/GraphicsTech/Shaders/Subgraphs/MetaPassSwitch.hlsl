#ifndef FORGEPLUS_META_PASS_SWITCH_INCLUDED
#define FORGEPLUS_META_PASS_SWITCH_INCLUDED

// Whether this is the Meta pass, where lighting is baked (it reads Base Color and Emission for lightmaps and Enlighten):
// URP's SHADERPASS_META, or HDRP's SHADERPASS_LIGHT_TRANSPORT
#if defined(SHADERPASS) && defined(SHADERPASS_META)
    #if SHADERPASS == SHADERPASS_META
        #define FORGEPLUS_IS_META_PASS
    #endif
#elif defined(SHADERPASS) && defined(SHADERPASS_LIGHT_TRANSPORT)
    #if SHADERPASS == SHADERPASS_LIGHT_TRANSPORT
        #define FORGEPLUS_IS_META_PASS
    #endif
#endif

// The Meta value in the Meta pass, and the main value in every other pass (Shader Graph's previews take the main value)
void MetaPassSwitch_float(float3 MainValue, float3 MetaValue, out float3 Out)
{
#ifdef FORGEPLUS_IS_META_PASS
    Out = MetaValue;
#else
    Out = MainValue;
#endif
}

void MetaPassSwitch_half(half3 MainValue, half3 MetaValue, out half3 Out)
{
#ifdef FORGEPLUS_IS_META_PASS
    Out = MetaValue;
#else
    Out = MainValue;
#endif
}

#endif
