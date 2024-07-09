// BSDF.hlsl

#ifndef __BxDF__
#define __BxDF__

#include "Constant.hlsl"
#include "Random.hlsl"

struct BxDFSample
{
    float3 f;
    float3 dir;
    float invPdf;
};

BxDFSample BxDFSampleInit(float3 f, float3 dir, float invPdf)
{
    BxDFSample bxdfSample;
    bxdfSample.f = f;
    bxdfSample.dir = dir;
    bxdfSample.invPdf = invPdf;
    return bxdfSample;
}

float3 FresnelSchlick(float3 f0, float3 w, float3 normal)
{
    return f0 + (1.0 - f0) * pow(1.0 - abs(dot(w, normal)), 5.0f);
}

float3 LambertianBRDF(float3 albedo, float3 wi, float3 wo, float3 normal)
{
    return albedo * INVPI * abs(dot(wi, normal));
}

float3 LambertianBTDF(float3 albedo, float3 wi, float3 wo, float3 normal)
{
    return albedo * INVPI * abs(dot(wi, normal));
}

float3 SpecularBRDF(float3 f0, float3 wi, float3 wo, float3 normal)
{
    return FresnelSchlick(f0, wi, normal);
}

float3 SpecularBTDF(float3 f0, float3 wi, float3 wo, float3 normal)
{
    return 1.0f - FresnelSchlick(f0, wi, normal);
}

BxDFSample LambertianSampleBRDF(float3 albedo, float3 wo, float3 normal)
{
    float3 sampleDir = RandomCosineWeightedHemisphereDir(normal);
    return BxDFSampleInit(LambertianBRDF(albedo, sampleDir, wo, normal) / dot(sampleDir, normal), sampleDir, PI);
}

BxDFSample LambertianSampleBTDF(float3 albedo, float3 wo, float3 normal)
{
    float3 sampleDir = RandomCosineWeightedHemisphereDir(-normal);
    return BxDFSampleInit(LambertianBRDF(albedo, sampleDir, wo, -normal) / dot(sampleDir, normal), sampleDir, PI);
}

BxDFSample SpecularSampleBRDF(float3 f0, float3 wo, float3 normal)
{
    float3 sampleDir = -wo + normal * abs(dot(wo, normal)) * 2.0f;
    return BxDFSampleInit(SpecularBRDF(f0, sampleDir, wo, normal), sampleDir, 1.0f);
}

BxDFSample SpecularSampleBTDF(float3 f0, float3 wo, float3 normal, float IOR, bool isFront)
{
    float etaIoT = isFront ? 1.0f / IOR : IOR;
    float cosThetaI = dot(wo, normal);
    float sinThetaI = sqrt(saturate(1.0f - cosThetaI * cosThetaI));
    float sin2ThetaI = sinThetaI * sinThetaI;
    float sin2ThetaT = etaIoT * etaIoT * sin2ThetaI;
    bool hasRefract = sin2ThetaT < 1.0f;
    float cosThetaT = hasRefract ? sqrt(1.0f - sin2ThetaT) : 0.0f;
    float3 sampleDir = etaIoT * (-wo) + (etaIoT * cosThetaI - cosThetaT) * normal;
    
    return BxDFSampleInit(hasRefract ? SpecularBTDF(f0, sampleDir, wo, normal) : 0.0f, sampleDir, 1.0f);
}

#endif // __BxDF__