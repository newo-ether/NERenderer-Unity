// RayInfo.hlsl

#ifndef __RAYINFO__
#define __RAYINFO__

#include "Ray.hlsl"
#include "Film.hlsl"

struct RayInfo
{
    Ray ray;
    Ray shadowRay;
    float3 radiance;
    float3 decay;
    int isHitLight;
    int isEnd;
};

// Uniform Variable
RWStructuredBuffer<RayInfo> rayInfoBuffer;

RayInfo RayInfoInit(Ray ray, Ray shadowRay, float3 radiance, float3 decay, int isHitLight)
{
    RayInfo rayInfo;
    rayInfo.ray = ray;
    rayInfo.shadowRay = shadowRay;
    rayInfo.radiance = radiance;
    rayInfo.decay = decay;
    rayInfo.isHitLight = isHitLight;
    rayInfo.isEnd = 0;
    return rayInfo;
}

uint RayInfoGetOffset(uint2 screenIndex, uint maxDepth)
{
    return (screenIndex.y * GetFilmWidth() + screenIndex.x) * maxDepth;
}

void RayInfoSetEmpty(uint offset, uint depth)
{
    rayInfoBuffer[offset + depth] = RayInfoInit(RayInitEmpty(), RayInitEmpty(), (float3) 0.0f, (float3) 0.0f, 0);
}

void RayInfoSetRay(Ray ray, uint offset, uint depth)
{
    rayInfoBuffer[offset + depth].ray = ray;
}

void RayInfoSetShadowRay(Ray shadowRay, uint offset, uint depth)
{
    rayInfoBuffer[offset + depth].shadowRay = shadowRay;
}

void RayInfoSetRadiance(float3 radiance, uint offset, int depth)
{
    rayInfoBuffer[offset + depth].radiance = radiance;
}

void RayInfoSetDecay(float3 decay, uint offset, int depth)
{
    rayInfoBuffer[offset + depth].decay = decay;
}

void RayInfoSetIsHitLight(bool isHitLight, uint offset, int depth)
{
    rayInfoBuffer[offset + depth].isHitLight = isHitLight ? 1 : 0;
}

void RayInfoUnsetEnd(uint offset, uint depth)
{
    rayInfoBuffer[offset + depth].isEnd = 0;
}

void RayInfoSetEnd(uint offset, uint depth)
{
    rayInfoBuffer[offset + depth].isEnd = 1;
}

#endif // __RAYINFO__