// Bound.hlsl

#ifndef __BOUND__
#define __BOUND__

#include "Constant.hlsl"
#include "Ray.hlsl"

struct Bound
{
    float3 pMin;
    float3 pMax;
};

struct BoundIntersectInfo
{
    bool isHit;
    float3 tMin;
    float3 tMax;
};

Bound BoundInit(float3 pMin, float3 pMax)
{
    Bound bound;
    bound.pMin = pMin;
    bound.pMax = pMax;
    return bound;
}

Bound BoundInitEmpty()
{
    Bound bound;
    bound.pMin = (float3) INF;
    bound.pMax = (float3) NEGINF;
    return bound;
}

bool BoundIsValid(Bound bound)
{
    return bound.pMax.x >= bound.pMin.x && bound.pMax.y >= bound.pMin.y && bound.pMax.z >= bound.pMin.z;
}

float3 BoundDiagnal(Bound bound)
{
    return bound.pMax - bound.pMin;
}

int BoundMaxExtentDimension(Bound bound)
{
    float3 diagnal = BoundDiagnal(bound);
    return diagnal.x > diagnal.y ? (diagnal.x > diagnal.z ? 0 : 2) : (diagnal.y > diagnal.z ? 1 : 2);
}

float3 BoundSurfaceArea(Bound bound)
{
    float3 diagnal = BoundDiagnal(bound);
    return 2.0f * (diagnal.x * diagnal.y + diagnal.y * diagnal.z + diagnal.x * diagnal.z);
}

float3 BoundOffset(Bound bound, float3 p)
{
    float3 pOffset = p - bound.pMin;
    float3 diagnal = bound.pMax - bound.pMin;
    return float3(pOffset.x / diagnal.x, pOffset.y / diagnal.y, pOffset.z / diagnal.z);
}

Bound BoundUnionBound(Bound boundA, Bound boundB)
{
    Bound bound;
    bound.pMin = min(boundA.pMin, boundB.pMin);
    bound.pMax = max(boundA.pMax, boundB.pMax);
    return bound;
}

Bound BoundUnionPoint(Bound bound, float3 p)
{
    bound.pMin = min(bound.pMin, p);
    bound.pMax = max(bound.pMax, p);
    return bound;
}

BoundIntersectInfo BoundIntersectInfoInit(float tMin, float tMax)
{
    BoundIntersectInfo boundIsect;
    boundIsect.isHit = tMin <= tMax;
    boundIsect.tMin = tMin;
    boundIsect.tMax = tMax;
    return boundIsect;
}

BoundIntersectInfo BoundIntersect(Bound bound, Ray ray, bool3 rayDirIsNeg, float3 invRayDir)
{
    float txMin = ((rayDirIsNeg.x ? bound.pMax : bound.pMin).x - ray.origin.x) * invRayDir.x;
    float txMax = ((rayDirIsNeg.x ? bound.pMin : bound.pMax).x - ray.origin.x) * invRayDir.x;
    
    float tyMin = ((rayDirIsNeg.y ? bound.pMax : bound.pMin).y - ray.origin.y) * invRayDir.y;
    float tyMax = ((rayDirIsNeg.y ? bound.pMin : bound.pMax).y - ray.origin.y) * invRayDir.y;
    
    float tzMin = ((rayDirIsNeg.z ? bound.pMax : bound.pMin).z - ray.origin.z) * invRayDir.z;
    float tzMax = ((rayDirIsNeg.z ? bound.pMin : bound.pMax).z - ray.origin.z) * invRayDir.z;
    
    float tMin = max(max(max(txMin, tyMin), tzMin), ray.tMin);
    float tMax = min(min(min(txMax, tyMax), tzMax), ray.tMax);
    
    return BoundIntersectInfoInit(tMin, tMax);
}

bool BoundIsIntersect(Bound bound, Ray ray, bool3 rayDirIsNeg, float3 invRayDir)
{
    float txMin = ((rayDirIsNeg.x ? bound.pMax : bound.pMin).x - ray.origin.x) * invRayDir.x;
    float txMax = ((rayDirIsNeg.x ? bound.pMin : bound.pMax).x - ray.origin.x) * invRayDir.x;
    
    float tyMin = ((rayDirIsNeg.y ? bound.pMax : bound.pMin).y - ray.origin.y) * invRayDir.y;
    float tyMax = ((rayDirIsNeg.y ? bound.pMin : bound.pMax).y - ray.origin.y) * invRayDir.y;
    
    float tzMin = ((rayDirIsNeg.z ? bound.pMax : bound.pMin).z - ray.origin.z) * invRayDir.z;
    float tzMax = ((rayDirIsNeg.z ? bound.pMin : bound.pMax).z - ray.origin.z) * invRayDir.z;
    
    float tMin = max(max(max(txMin, tyMin), tzMin), ray.tMin);
    float tMax = min(min(min(txMax, tyMax), tzMax), ray.tMax);
    
    return tMin <= tMax;
}

#endif // __BOUND__