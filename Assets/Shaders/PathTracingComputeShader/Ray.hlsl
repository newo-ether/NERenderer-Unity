// Ray.hlsl

#ifndef __RAY__
#define __RAY__

struct Ray
{
    float3 origin;
    float3 dir;
    float tMin;
    float tMax;
};

Ray RayInit(float3 origin, float3 dir, float tMin, float tMax)
{
    Ray ray;
    ray.origin = origin;
    ray.dir = dir;
    ray.tMin = tMin;
    ray.tMax = tMax;
    return ray;
}

Ray RayInitEmpty()
{
    Ray ray;
    ray.origin = (float3) 0.0f;
    ray.dir = (float3) 0.0f;
    ray.tMin = 0.0f;
    ray.tMax = 0.0f;
    return ray;
}

float3 RayAt(Ray ray, float t)
{
    return ray.origin + ray.dir * t;
}

#endif // __RAY__