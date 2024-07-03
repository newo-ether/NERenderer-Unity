// Random.hlsl

#ifndef __RANDOM__
#define __RANDOM__

#include "Film.hlsl"
#include "Constant.hlsl"

// Uniform Variable
StructuredBuffer<uint> randomBuffer;

// Uniform Variable
uint extraSeed;

static const uint LCG_A = 16807;
static const uint LCG_C = 0;
static const uint LCG_M = 2147483647;

static uint seed;

void RandomInit(uint2 screenIndex)
{
    seed = randomBuffer[screenIndex.y * GetFilmWidth() + screenIndex.x];
    seed = ((seed + extraSeed) * LCG_A + LCG_C) % LCG_M;
}

uint RandomUint()
{
    seed = (seed * LCG_A + LCG_C) % LCG_M;
    return seed;
}

float Random01()
{
    return (float) RandomUint() / (float) LCG_M;
}

float Random11()
{
    return Random01() * 2.0f - 1.0f;
}

float2 RandomDiskSample()
{
    float r = sqrt(Random01());
    float theta = 2.0f * PI * Random01();
    return float2(r * cos(theta), r * sin(theta));
}

float3 RandomSphereDir()
{
    float z = Random11();
    float r = sqrt(1.0f - z * z);
    float phi = Random01() * TWOPI;
    return float3(r * cos(phi), r * sin(phi), z);
}

float3 RandomHemisphereDir(float3 normal)
{
    float3 randDir = RandomSphereDir();
    if (dot(normal, randDir) < 0.0f)
    {
        randDir = -randDir;
    }
    return randDir;
}

float3 RandomCosineWeightedHemisphereDir(float3 normal)
{
    float3 xAxis;
    if (abs(normal.x) > abs(normal.y))
    {
        xAxis = normalize(float3(-normal.z, 0.0f, normal.x));
    }
    else
    {
        xAxis = normalize(float3(0.0f, normal.z, -normal.y));
    }
    float3 yAxis = cross(normal, xAxis);
    float3 zAxis = normal;
    
    float2 diskSample = RandomDiskSample();
    float x = diskSample.x;
    float y = diskSample.y;
    float z = 1.0f - x * x - y * y;
    return xAxis * x + yAxis * y + zAxis * z;
}

#endif // __RANDOM__