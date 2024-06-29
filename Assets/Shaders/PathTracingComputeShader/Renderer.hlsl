// Renderer.hlsl

#ifndef __RENDERER__
#define __RENDERER__

#include "Ray.hlsl"
#include "Scene.hlsl"
#include "IntersectInfo.hlsl"
#include "Material.hlsl"
#include "Random.hlsl"
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
            if (MaterialIsEmissive(material))
            {
                float3 Li = MaterialGetEmission(material);
                Lo = Li * decay;
                break;
            }
            if (Random01() > rr)
            {
                Lo = float3(0.0f, 0.0f, 0.0f);
                break;
            }
            float3 newDir = RandomHemisphereDir(isect.hitNormal);
            ray = RayInit(isect.hitPoint, newDir, EPSILON, INF);
            float invPdf = TWOPI;
            decay *= MaterialBSDF(material, newDir, isect.incomeDir, isect.hitNormal)
                     * dot(newDir, isect.hitNormal) * invPdf * invrr;
        }
        else
        {
            Lo = float3(0.0f, 0.0f, 0.0f);
            break;
        }
    }
    return Lo;
}

#endif // __RENDERER__