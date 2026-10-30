// Fog filling the space under media (MediaFog): each volume is a convex prism, drawn by its inside faces (so it's drawn
// while the camera is in it, and whatever is in front of it), regardless of depth. Each pixel finds where its view ray
// enters and leaves the volume (from its planes), cut short by what's drawn behind (the camera's depth texture), and
// fogs by the distance between, so the fog is right from inside it, outside it, and above it through its surface.
// Drawn twice: before media surfaces for volumes the view is above the top of, and after them for those it's below the
// top of (_InFrontOfSurfaces), so a surface is fogged only where it's seen from under its media.
Shader "ForgePlus/MediaFog"
{
    Properties
    {
        [Toggle] _InFrontOfSurfaces ("In Front Of Surfaces", Float) = 0
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

        Cull Front
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Unlit"

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            // Each volume's top, bottom and sides (MediaFog.PlanesPerVolume), as (normal, distance): inside where
            // dot(normal, point) <= distance
            #define PLANES_PER_VOLUME 10

            CBUFFER_START(UnityPerMaterial)
                float _InFrontOfSurfaces;
            CBUFFER_END

            StructuredBuffer<float4> _MediaFogPlanes;

            // Each volume's color (rgb) and density per meter (a)
            StructuredBuffer<float4> _MediaFogVolumes;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 volumeIndex : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                nointerpolation uint volumeIndex : TEXCOORD1;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;

                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.volumeIndex = (uint) (input.volumeIndex.x + 0.5);

                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                uint firstPlane = input.volumeIndex * PLANES_PER_VOLUME;
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);

                // From the near plane, toward this pixel (for perspective and orthographic views alike)
                float3 origin = ComputeWorldSpacePosition(screenUV, UNITY_NEAR_CLIP_VALUE, UNITY_MATRIX_I_VP);
                float3 direction = normalize(input.positionWS - origin);

                // The first plane is the top, whose distance is its height
                bool viewIsBelowTop = origin.y < _MediaFogPlanes[firstPlane].w;
                if (viewIsBelowTop != (_InFrontOfSurfaces > 0.5))
                {
                    discard;
                }

                // What's drawn behind (opaque surfaces and objects)
                float3 scenePosition = ComputeWorldSpacePosition(screenUV, SampleSceneDepth(screenUV), UNITY_MATRIX_I_VP);
                float enter = 0.0;
                float leave = dot(scenePosition - origin, direction);

                for (uint i = 0; i < PLANES_PER_VOLUME; i++)
                {
                    float4 plane = _MediaFogPlanes[firstPlane + i];
                    float facing = dot(plane.xyz, direction);
                    float clearance = plane.w - dot(plane.xyz, origin);

                    if (abs(facing) < 1e-6)
                    {
                        // Parallel to the plane: wholly inside or outside it
                        if (clearance < 0.0)
                        {
                            discard;
                        }
                    }
                    else if (facing > 0.0)
                    {
                        leave = min(leave, clearance / facing);
                    }
                    else
                    {
                        enter = max(enter, clearance / facing);
                    }
                }

                float thickness = leave - enter;
                if (thickness <= 0.0)
                {
                    discard;
                }

                float4 volume = _MediaFogVolumes[input.volumeIndex];

                return half4(volume.rgb, 1.0 - exp(-volume.a * thickness));
            }
            ENDHLSL
        }
    }
}
