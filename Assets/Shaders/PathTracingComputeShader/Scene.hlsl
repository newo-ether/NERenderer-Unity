// Scene.hlsl

#ifndef __SCENE__
#define __SCENE__

#include "IntersectInfo.hlsl"
#include "BVH.hlsl"
#include "Primitive.hlsl"
#include "Shape.hlsl"
#include "Material.hlsl"
#include "Ray.hlsl"

#define __USE_BVH__

IntersectInfo SceneIntersect(Ray ray)
{
#ifndef __USE_BVH__
    
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

#else
    
    return BVHIntersect(ray);
    
#endif // __USE_BVH__
}

bool SceneIsIntersect(Ray ray)
{
#ifndef __USE_BVH__
    
    for (int i = 0; i < GetPrimitiveCount(); i++)
    {
        Primitive primitive = GetPrimitive(i);
        if (ShapeIsIntersect(primitive.shape, ray))
        {
            return true;
        }
    }
    return false;

#else
    
    return BVHIsIntersect(ray);
    
#endif // __USE_BVH__
}

#endif // __SCENE__