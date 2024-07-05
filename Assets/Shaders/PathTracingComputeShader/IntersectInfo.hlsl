// IntersectInfo.hlsl

#ifndef __INTERSECTINFO__
#define __INTERSECTINFO__

#include "Material.hlsl"
#include "Constant.hlsl"

struct IntersectInfo
{
    bool isHit;
    bool isFront;
    float tHit;
    float3 hitPoint;
    float3 hitNormal;
    float3 incomeDir;
    Material hitMaterial;
};

IntersectInfo IntersectInfoInit(bool isFront,
                                float tHit,
                                float3 hitPoint,
                                float3 hitNormal,
                                float3 incomeDir,
                                Material material)
{
    IntersectInfo isect =
    {
        true,
        isFront,
        tHit,
        hitPoint,
        hitNormal,
        incomeDir,
        material
    };
    return isect;
}

IntersectInfo IntersectInfoInitNone()
{
    IntersectInfo isect =
    {
        false,
        false,
        INF,
        (float3) 0.0f,
        (float3) 0.0f,
        (float3) 0.0f,
        MaterialInitEmpty()
    };
    return isect;
}

#endif // __INTERSECTINFO__