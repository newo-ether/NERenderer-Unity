// Renderer.hlsl

#ifndef __RENDERER__
#define __RENDERER__

#include "Ray.hlsl"
#include "Scene.hlsl"
#include "IntersectInfo.hlsl"
#include "Material.hlsl"
#include "Random.hlsl"
#include "Light.hlsl"
#include "Constant.hlsl"

struct RenderOption
{
    int maxDepth;
    float russianRoulete;
};

// Uniform Variable
StructuredBuffer<RenderOption> renderOptionBuffer;

float3 PathTracing(Ray ray)
{
    RenderOption renderOption = renderOptionBuffer[0];
    float3 Lo = float3(0.0f, 0.0f, 0.0f);
    float3 decay = float3(1.0f, 1.0f, 1.0f);
    int maxDepth = renderOption.maxDepth;
    float rr = renderOption.russianRoulete;
    float invrr = 1.0f / rr;

    for (int depth = 0; depth < maxDepth; depth++)
    {
        IntersectInfo isect = SceneIntersect(ray);
        if (isect.isHit)
        {
            Material material = isect.hitMaterial;
            if (depth == 0 && MaterialIsEmissive(material) && dot(isect.incomeDir, isect.hitNormal) > 0.0f)
            {
                Lo = MaterialGetEmission(material);
            }
            else
            {
                Lo += decay * SampleOneLight(isect);
            }
            
            if (depth >= 2 && Random01() > rr)
            {
                break;
            }
            
            float3 newDir = RandomCosineWeightedHemisphereDir(isect.hitNormal);
            ray = RayInit(isect.hitPoint, newDir, EPSILON, INF);
            float invPdf = PI;
            decay *= MaterialBSDF(material, newDir, isect.incomeDir, isect.hitNormal)
                     * invPdf;
            
            if (depth >= 2)
            {
                decay *= invrr;
            }
        }
        else
        {
            break;
        }
    }
    return Lo;
}

#endif // __RENDERER__