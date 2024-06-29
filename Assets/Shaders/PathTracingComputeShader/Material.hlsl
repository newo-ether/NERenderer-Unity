// Material.hlsl

#ifndef __MATERIAL__
#define __MATERIAL__

#include "Constant.hlsl"

struct Material
{
    int type;
    float3 albedo;
    float metallic;
    float roughness;
    float IOR;
    float transmission;
    float3 emission;
};

Material MaterialInitEmpty()
{
    Material material =
    {
        0,
        (float3) 1.0f,
        0.0f,
        1.0f,
        1.0f,
        0.0f,
        (float3) 0.0f
    };
    return material;
}

bool MaterialIsEmissive(Material material)
{
    return (material.emission.r != 0.0f) || (material.emission.g != 0.0f) || (material.emission.b != 0.0f);
}

float3 MaterialGetEmission(Material material)
{
    return material.emission;
}

float3 MaterialBSDF(Material material, float3 wi, float3 wo, float3 normal)
{
    if (material.type == 0)
    {
        return material.albedo * INVPI;
    }
    else
    {
        return (float3) 0.0f;
    }
}

#endif // __MATERIAL__