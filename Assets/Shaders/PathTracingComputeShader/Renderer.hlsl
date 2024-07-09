// Renderer.hlsl

#ifndef __RENDERER__
#define __RENDERER__

#include "Ray.hlsl"
#include "RayInfo.hlsl"
#include "Scene.hlsl"
#include "IntersectInfo.hlsl"
#include "Material.hlsl"
#include "Random.hlsl"
#include "Direct.hlsl"
#include "Constant.hlsl"

struct RenderOption
{
    int maxDepth;
    float russianRoulete;
};

// Uniform Variable
StructuredBuffer<RenderOption> renderOptionBuffer;

float3 PathTracing(Ray ray, uint2 screenIndex)
{
    RenderOption renderOption = renderOptionBuffer[0];
    float3 Lo = float3(0.0f, 0.0f, 0.0f);
    float3 decay = float3(1.0f, 1.0f, 1.0f);
    int maxDepth = renderOption.maxDepth;
    float rr = renderOption.russianRoulete;
    float invrr = 1.0f / rr;
    
    const uint offset = RayInfoGetOffset(screenIndex, maxDepth);
    
    for (int depth = 0; depth < maxDepth; depth++)
    {
        RayInfoSetEmpty(offset, depth);
        IntersectInfo isect = SceneIntersect(ray);
        if (isect.isHit)
        {
            RayInfoSetRay(RayInit(ray.origin, ray.dir, ray.tMin, isect.tHit), offset, depth);
            RayInfoSetDecay(decay, offset, depth);
            Material material = isect.hitMaterial;
            if (depth == 0 && MaterialIsEmissive(material) && isect.isFront)
            {
                float3 emission = MaterialGetEmission(material);
                RayInfoSetRadiance(emission, offset, depth);
                RayInfoSetIsHitLight(true, offset, depth);
                Lo = emission;
            }
            else
            {
                float3 direct = EstimateDirect(isect, offset, depth, MaterialGetSampleStratrgy(material));
                RayInfoSetRadiance(direct, offset, depth);
                Lo += decay * direct;
            }
            
            if (depth >= 2 && Random01() > rr)
            {
                RayInfoSetEnd(offset, depth);
                break;
            }
            
            BxDFSample bsdfSample = MaterialSampleBSDF(material, isect.incomeDir, isect.hitNormal, isect.isFront);
            ray = RayInit(isect.hitPoint, bsdfSample.dir, EPSILON, INF);
            decay *= bsdfSample.f * bsdfSample.invPdf;
            
            if (depth >= 2)
            {
                decay *= invrr;
            }
        }
        else
        {
            RayInfoSetEnd(offset, max(depth - 1, 0));
            break;
        }
    }
    RayInfoSetEnd(offset, max(maxDepth - 1, 0));
    return Lo;
}

#endif // __RENDERER__