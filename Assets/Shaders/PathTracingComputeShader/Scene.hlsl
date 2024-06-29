// Scene.hlsl

#ifndef __SCENE__
#define __SCENE__

#include "IntersectInfo.hlsl"
#include "Primitive.hlsl"
#include "Shape.hlsl"
#include "Material.hlsl"
#include "Ray.hlsl"

IntersectInfo SceneIntersect(Ray ray)
{
    IntersectInfo isect = IntersectInfoInitNone();
    
    for (int i = 0; i < GetPrimitiveCount(); i++)
    {
        Primitive primitive = GetPrimitive(i);
        IntersectInfo tempIsect = ShapeIntersect(primitive.shape, ray);
        if (tempIsect.isHit)
        {
            Material material = primitive.material;
            ray.tMax = tempIsect.tHit;
            isect = tempIsect;
            isect.hitMaterial = material;
        }
    }
    return isect;
}

bool SceneIsIntersect(Ray ray)
{
    for (int i = 0; i < GetPrimitiveCount(); i++)
    {
        Primitive primitive = GetPrimitive(i);
        if (ShapeIsIntersect(primitive.shape, ray))
        {
            return true;
        }
    }
    return false;
}

#endif // __SCENE__