// Shape.hlsl

#ifndef __SHAPE__
#define __SHAPE__

#include "IntersectInfo.hlsl"
#include "Random.hlsl"
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
    float t = dot(shape.vertex[0] - ray.origin, normal) / dot(ray.dir, normal);
    
    if (t <= ray.tMin || t >= ray.tMax)
    {
        return IntersectInfoInitNone();
    }
    else
    {
        float3 p = RayAt(ray, t);
    
        float b0 = dot(cross(e0, p - shape.vertex[2]), normal);
        float b1 = dot(cross(e1, p - shape.vertex[0]), normal);
        float b2 = dot(cross(e2, p - shape.vertex[1]), normal);
    
        if ((b0 >= 0 && b1 >= 0 && b2 >= 0) || (b0 <= 0 && b1 <= 0 && b2 <= 0))
        {
            return IntersectInfoInit(t, p, normal, -ray.dir, MaterialInitEmpty());
        }
        else
        {
            return IntersectInfoInitNone();
        }
    }
}

bool ShapeIsIntersect(Shape shape, Ray ray)
{
    float3 e0 = shape.vertex[0] - shape.vertex[2];
    float3 e1 = shape.vertex[1] - shape.vertex[0];
    float3 e2 = shape.vertex[2] - shape.vertex[1];
    
    float3 normal = normalize(cross(e1, e2));
    float t = dot(shape.vertex[0] - ray.origin, normal) / dot(ray.dir, normal);
    
    if (t <= ray.tMin || t >= ray.tMax)
    {
        return false;
    }
    else
    {
        float3 p = RayAt(ray, t);
    
        float b0 = dot(cross(e0, p - shape.vertex[2]), normal);
        float b1 = dot(cross(e1, p - shape.vertex[0]), normal);
        float b2 = dot(cross(e2, p - shape.vertex[1]), normal);
    
        if ((b0 >= 0 && b1 >= 0 && b2 >= 0) || (b0 <= 0 && b1 <= 0 && b2 <= 0))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}

float3 ShapeSamplePoint(Shape shape)
{
    float2 rand = float2(Random01(), Random01());
    float u = 1.0f - sqrt(rand.x);
    float v = rand.y * sqrt(rand.x);
    return shape.vertex[0]
           + (shape.vertex[1] - shape.vertex[0]) * u
           + (shape.vertex[2] - shape.vertex[0]) * v;
}

float3 ShapeGetNormal(Shape shape)
{
    return normalize(cross(shape.vertex[1] - shape.vertex[0], shape.vertex[2] - shape.vertex[1]));
}

float ShapeGetArea(Shape shape)
{
    return 0.5 * length(cross(shape.vertex[1] - shape.vertex[0], shape.vertex[2] - shape.vertex[0]));
}

#endif // __SHAPE__