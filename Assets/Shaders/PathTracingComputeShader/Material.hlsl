// Material.hlsl

#ifndef __MATERIAL__
#define __MATERIAL__

#include "Constant.hlsl"
#include "BxDF.hlsl"
#include "Random.hlsl"

struct Material
{
    int type;
    float3 albedo;
    float metallic;
    float roughness;
    float3 F0;
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
        (float3) 0.0f,
        1.0f,
        0.0f,
        (float3) 0.0f
    };
    return material;
}

int MaterialGetSampleStratrgy(Material material)
{
    return material.type;
}

bool MaterialIsEmissive(Material material)
{
    return (material.emission.r != 0.0f) || (material.emission.g != 0.0f) || (material.emission.b != 0.0f);
}

float3 MaterialGetEmission(Material material)
{
    return material.emission;
}

float3 MaterialBRDF(Material material, float3 wi, float3 wo, float3 normal)
{
    if (material.type == 0)
    {
        return LambertianBRDF(material.albedo, wi, wo, normal);
    }
    else
    {
        return (float3) 0.0f;
    }
}

BxDFSample MaterialSampleBRDF(Material material, float3 wo, float3 normal)
{
    if (material.type == 0)
    {
        return LambertianSampleBRDF(material.albedo, wo, normal);
    }
    else
    {
        return SpecularSampleBRDF(lerp(material.F0, material.albedo, material.metallic), wo, normal);
    }
}

BxDFSample MaterialSampleBSDF(Material material, float3 wo, float3 normal, bool isFront)
{
    if (material.type == 0)
    {
        BxDFSample bsdfSample;
        float threshold = 1.0f / (1.0f + material.transmission);
        if (Random01() < threshold)
        {
            bsdfSample = LambertianSampleBRDF(material.albedo, wo, normal);
        }
        else
        {
            bsdfSample = LambertianSampleBTDF(material.albedo, wo, normal);
        }
        
        bsdfSample.invPdf *= 1.0 + material.transmission;
        return bsdfSample;
    }
    else
    {
        BxDFSample bsdfSample;
        float threshold = 1.0f / (1.0f + material.transmission);
        if (Random01() < threshold)
        {
            bsdfSample = SpecularSampleBRDF(lerp(material.F0, material.albedo, material.metallic), wo, normal);
        }
        else
        {
            bsdfSample = SpecularSampleBTDF(lerp(material.F0, material.albedo, material.metallic), wo, normal, material.IOR, isFront);
        }
        
        bsdfSample.invPdf *= 1.0 + material.transmission;
        return bsdfSample;
    }
}

#endif // __MATERIAL__