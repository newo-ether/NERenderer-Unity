// RayBuffer.hlsl

#include "Ray.hlsl"
#include "Film.hlsl"

struct RayBuffer
{
    Ray ray;
    int isEnd;
};

// Uniform Variable
RWStructuredBuffer<RayBuffer> rayBuffer;

RayBuffer RayBufferInit(Ray ray)
{
    RayBuffer rayBuf;
    rayBuf.ray = ray;
    rayBuf.isEnd = 0;
    return rayBuf;
}

uint RayBufferGetOffset(uint2 screenIndex, uint maxDepth)
{
    return (screenIndex.y * GetFilmWidth() + screenIndex.x) * maxDepth;
}

void RayBufferSetRay(Ray ray, uint offset, uint depth)
{
    rayBuffer[offset + depth] = RayBufferInit(ray);
}

void RayBufferSetEnd(uint offset, uint depth)
{
    rayBuffer[offset + depth].isEnd = 1;
}