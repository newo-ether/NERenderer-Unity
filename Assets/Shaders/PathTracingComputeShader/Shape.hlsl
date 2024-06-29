// Shape.hlsl

#ifndef __SHAPE__
#define __SHAPE__

#include "IntersectInfo.hlsl"
#include "Material.hlsl"
#include "Ray.hlsl"

struct Shape
{
    float3 vertex[3];
};

IntersectInfo ShapeIntersect(Shape shape, Ray ray)
{
    float3 e0 = shape.vertex[0] - shape.vertex[2];
    float3 e1 = shape.vertex[1] - shape.vertex[0];
    float3 e2 = shape.vertex[2] - shape.vertex[1];
    
    float3 normal = normalize(cross(e1, e2));
    float3 barycenter = (shape.vertex[0] + shape.vertex[1] + shape.vertex[2]) / 3.0f;
    float t = dot(barycenter - ray.origin, normal) / dot(ray.dir, normal);
    
    if (t <= ray.tMin || t >= ray.tMax)
    {
        return IntersectInfoInitNone();
    }
    
    float3 p = RayAt(ray, t);
    
    float b0 = dot(cross(e0, p - shape.vertex[2]), normal);
    float b1 = dot(cross(e1, p - shape.vertex[0]), normal);
    float b2 = dot(cross(e2, p - shape.vertex[1]), normal);
    
    if ((b0 >= 0 && b1 >= 0 && b2 >= 0) || (b0 <= 0 && b1 <= 0 && b2 <= 0))
    {
        return IntersectInfoInit(t, p, normal, -ray.dir, MaterialInitEmpty());
    }

    return IntersectInfoInitNone();
}

bool ShapeIsIntersect(Shape shape, Ray ray)
{
    float3 e0 = shape.vertex[0] - shape.vertex[2];
    float3 e1 = shape.vertex[1] - shape.vertex[0];
    float3 e2 = shape.vertex[2] - shape.vertex[1];
    
    float3 normal = normalize(cross(e1, e2));
    float3 barycenter = (shape.vertex[0] + shape.vertex[1] + shape.vertex[2]) / 3.0f;
    float t = dot(barycenter - ray.origin, normal) / dot(ray.dir, normal);
    
    if (t <= ray.tMin || t >= ray.tMax)
    {
        return false;
    }
    
    float3 p = RayAt(ray, t);
    
    float b0 = dot(cross(e0, p - shape.vertex[2]), normal);
    float b1 = dot(cross(e1, p - shape.vertex[0]), normal);
    float b2 = dot(cross(e2, p - shape.vertex[1]), normal);
    
    if ((b0 >= 0 && b1 >= 0 && b2 >= 0) || (b0 <= 0 && b1 <= 0 && b2 <= 0))
    {
        return true;
    }
    
    return false;
}

#endif // __SHAPE__