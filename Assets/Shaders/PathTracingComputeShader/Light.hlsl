// Light.hlsl

#ifndef __LIGHT__
#define __LIGHT__

#include "Constant.hlsl"
#include "RayInfo.hlsl"
#include "IntersectInfo.hlsl"
#include "Random.hlsl"
#include "Primitive.hlsl"
#include "Scene.hlsl"

float3 SampleOneLight(IntersectInfo isect, uint offset, uint depth)
{
    int index = clamp((int) (Random01() * (float) GetEmissivePrimitiveCount()), 0, GetEmissivePrimitiveCount() - 1);
    Primitive samplePrimitive = GetEmissivePrimitive(index);
    
    float3 samplePoint = ShapeSamplePoint(samplePrimitive.shape);
    float3 wi = normalize(samplePoint - isect.hitPoint);
    float dist = length(samplePoint - isect.hitPoint);
    
    Ray ray = RayInit(isect.hitPoint, wi, 0.0f, dist * 0.999f);
    
    if (SceneIsIntersect(ray))
    {
        return (float3) 0.0f;
    }
    else
    {
        float3 normal = ShapeGetEmissionNormal(samplePrimitive.shape);
        if (dot(-wi, normal) <= 0.0f || dot(wi, isect.hitNormal) <= 0.0f)
        {
            return (float3) 0.0f;
        }
        else
        {
            float invPdf = dot(-wi, normal) * ShapeGetArea(samplePrimitive.shape) / (dist * dist);
            
            RayInfoSetShadowRay(ray, offset, depth);
    
            return MaterialGetEmission(samplePrimitive.material)
                   * MaterialBSDF(isect.hitMaterial, wi, isect.incomeDir, isect.hitNormal)
                   * dot(wi, isect.hitNormal)
                   * invPdf
                   * GetEmissivePrimitiveCount();
        }
    }
}

float3 SampleAllLights(IntersectInfo isect)
{
    float3 L = (float3) 0.0f;
    for (int i = 0; i < GetEmissivePrimitiveCount(); i++)
    {
        Primitive samplePrimitive = GetEmissivePrimitive(i);
        float3 samplePoint = ShapeSamplePoint(samplePrimitive.shape);
        float3 wi = normalize(samplePoint - isect.hitPoint);
        float dist = length(samplePoint - isect.hitPoint);
        if (SceneIsIntersect(RayInit(isect.hitPoint, wi, 0.0f, dist * 0.999f)))
        {
            continue;
        }
        float3 normal = ShapeGetEmissionNormal(samplePrimitive.shape);
        if (dot(-wi, normal) <= 0.0f || dot(wi, isect.hitNormal) <= 0.0f)
        {
            continue;
        }
        float invPdf = dot(-wi, normal) * ShapeGetArea(samplePrimitive.shape) / (dist * dist);
    
        L += MaterialGetEmission(samplePrimitive.material)
             * MaterialBSDF(isect.hitMaterial, wi, isect.incomeDir, isect.hitNormal)
             * dot(wi, isect.hitNormal)
             * invPdf;
    }
    return L;
}

#endif // __LIGHT__