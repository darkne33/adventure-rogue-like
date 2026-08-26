#ifndef LITTLE_RUSH_PROXIMITY_FADE_DITHER_INCLUDED
#define LITTLE_RUSH_PROXIMITY_FADE_DITHER_INCLUDED

// The URP Lit passes already call LODFadeCrossFade from their Forward,
// DepthOnly and DepthNormals fragments. Reuse those call sites with the
// proximity fade value so every pass writes the same dithered pixels.
#ifndef UNIVERSAL_PIPELINE_LODCROSSFADE_INCLUDED
#define UNIVERSAL_PIPELINE_LODCROSSFADE_INCLUDED

float ProximityFadeNoise(float2 pixelPosition)
{
    float2 pixel = floor(pixelPosition);
    return frac(52.9829189 * frac(dot(pixel, float2(0.06711056, 0.00583715))));
}

void LODFadeCrossFade(float4 positionCS)
{
    // URP/Lit already places _ClearCoatMask in UnityPerMaterial. This shader
    // never compiles a clear-coat variant, so the slot can carry fade coverage
    // without introducing a loose uniform that disables SRP batching.
    clip(saturate(_ClearCoatMask) - ProximityFadeNoise(positionCS.xy));
}

#endif

#ifndef LOD_FADE_CROSSFADE
#define LOD_FADE_CROSSFADE 1
#endif

#endif
