// Light.hlsl

#ifndef __DIRECT__
#define __DIRECT__

#include "Constant.hlsl"
#include "Material.hlsl"
#include "RayInfo.hlsl"
#include "IntersectInfo.hlsl"
#include "Random.hlsl"
#include "Primitive.hlsl"
#include "Scene.hlsl"

float3 EstimateDirect(IntersectInfo isect, uint offset, uint depth, int strategy)
{
    // Sampling the Light
    float3 L_light = (float3) 0.0f;
    float lightPdf = 0.0f;
    if (strategy == 0 || strategy == 2)
    {
        int index = clamp((int) (Random01() * (float) GetEmissivePrimitiveCount()), 0, GetEmissivePrimitiveCount() - 1);
        Primitive samplePrimitive = GetEmissivePrimitive(index);
    
        float3 samplePoint = ShapeSamplePoint(samplePrimitive.shape);
        float3 wi = normalize(samplePoint - isect.hitPoint);
        float dist = length(samplePoint - isect.hitPoint);
    
        Ray ray = RayInit(isect.hitPoint, wi, EPSILON, dist * 0.999f);
    
        if (!SceneIsIntersect(ray))
        {
            float3 f = MaterialBRDF(isect.hitMaterial, wi, isect.incomeDir, isect.hitNormal);
            if (f.r > 0.0f && f.g > 0.0f && f.b > 0.0f)
            {
                float3 normal = ShapeGetEmissionNormal(samplePrimitive.shape);
                if (dot(-wi, normal) > 0.0f && dot(wi, isect.hitNormal) > 0.0f)
                {
                    float invPdf = dot(-wi, normal) * ShapeGetArea(samplePrimitive.shape) / (dist * dist);
            
                    RayInfoSetShadowRay(ray, offset, depth);
    
                    L_light = MaterialGetEmission(samplePrimitive.material)
                              * f
                              * invPdf
                              * GetEmissivePrimitiveCount();
            
                    lightPdf = invPdf == 0.0f ? 0.0f : 1.0f / invPdf;
                }
            }
        }
    }
    
    // Sampling the BSDF
    float3 L_brdf = (float3) 0.0f;
    float brdfPdf = 0.0f;
    if (strategy == 1 || strategy == 2)
    {
        BxDFSample brdfSample = MaterialSampleBRDF(isect.hitMaterial, isect.incomeDir, isect.hitNormal);
        float3 wi = brdfSample.dir;
        Ray ray = RayInit(isect.hitPoint, wi, EPSILON, INF);
    
        IntersectInfo brdfIsect = SceneIntersect(ray);
        if (brdfIsect.isHit)
        {
            if (brdfIsect.isFront)
            {
                RayInfoSetShadowRay(RayInit(ray.origin, ray.dir, ray.tMin, brdfIsect.tHit), offset, depth);
                
                L_brdf = MaterialGetEmission(brdfIsect.hitMaterial)
                         * brdfSample.f
                         * brdfSample.invPdf;
            
                brdfPdf = brdfSample.invPdf == 0.0f ? 0.0f : 1.0f / brdfSample.invPdf;
            }
        }
    }
    
    float lightPdfSquared = lightPdf * lightPdf;
    float brdfPdfSquared = brdfPdf * brdfPdf;
    float weightLight = lightPdfSquared == 0.0f ? 0.0f : lightPdfSquared / (lightPdfSquared + brdfPdfSquared);
    float weightBRDF = brdfPdfSquared == 0.0f ? 0.0f : brdfPdfSquared / (lightPdfSquared + brdfPdfSquared);
    
    return L_light * weightLight + L_brdf * weightBRDF;
}

#endif // __DIRECT__