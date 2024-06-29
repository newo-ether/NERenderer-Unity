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
    return (float) max(RandomUint() - 1, 0) / (float) (LCG_M - 1);
}

float Random11()
{
    return Random01() * 2.0f - 1.0f;
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

#endif // __RANDOM__