// BVH.hlsl

#ifndef __BVH__
#define __BVH__

#include "Bound.hlsl"
#include "Ray.hlsl"
#include "IntersectInfo.hlsl"
#include "Primitive.hlsl"

struct LinearBVHNode
{
    Bound bound;
    
    // Primitive Offset for Leaf Node, Second Child Offset for Interior Node
    int offset;
    
    // Zero for Interior Node
    int primtiveCount;

    // Zero for Leaf Node
    int splitAxis;
};

// Uniform Variable
StructuredBuffer<LinearBVHNode> bvhNodes;

// Uniform Variable
int bvhNodeCount;

static const int maxBVHDepth = 32;

LinearBVHNode BVHInteriorInit(Bound bound, int secondChildOffset, int splitAxis)
{
    LinearBVHNode node;
    node.bound = bound;
    node.offset = secondChildOffset;
    node.primtiveCount = 0;
    node.splitAxis = splitAxis;
    return node;
}

LinearBVHNode BVHLeafInit(Bound bound, int primitiveOffset, int primitiveCount)
{
    LinearBVHNode node;
    node.bound = bound;
    node.offset = primitiveOffset;
    node.primtiveCount = primitiveCount;
    node.splitAxis = 0;
    return node;
}

IntersectInfo BVHIntersect(Ray ray)
{
    IntersectInfo isect = IntersectInfoInitNone();
    int stack[maxBVHDepth];
    int offset = 0;
    float3 invRayDir = (float3) 1.0f / ray.dir;
    bool3 rayDirIsNeg = bool3(ray.dir.x < 0.0f, ray.dir.y < 0.0f, ray.dir.z < 0.0f);
    
    for (int i = 0; i < bvhNodeCount;)
    {
        LinearBVHNode node = bvhNodes[i];
        if (BoundIsIntersect(node.bound, ray, rayDirIsNeg, invRayDir))
        {
            if (node.primtiveCount == 0)
            {
                if (rayDirIsNeg[node.splitAxis])
                {
                    stack[offset++] = i + 1;
                    i = node.offset;
                }
                else
                {
                    stack[offset++] = node.offset;
                    i++;
                }
            }
            else
            {
                for (int n = node.offset; n < node.offset + node.primtiveCount; n++)
                {
                    Primitive primitive = GetPrimitive(n);
                    IntersectInfo tempIsect = ShapeIntersect(primitive.shape, ray);
                    if (tempIsect.isHit)
                    {
                        Material material = primitive.material;
                        ray.tMax = tempIsect.tHit;
                        isect = tempIsect;
                        isect.hitMaterial = material;
                    }
                }
                
                if (offset == 0)
                {
                    break;
                }
                i = stack[--offset];
            }
        }
        else
        {
            if (offset == 0)
            {
                break;
            }
            i = stack[--offset];
        }
    }
    
    return isect;
}

bool BVHIsIntersect(Ray ray)
{
    bool isHit;
    int stack[maxBVHDepth];
    int offset = 0;
    float3 invRayDir = (float3) 1.0f / ray.dir;
    bool3 rayDirIsNeg = bool3(ray.dir.x < 0.0f, ray.dir.y < 0.0f, ray.dir.z < 0.0f);
    
    for (int i = 0; i < bvhNodeCount;)
    {
        LinearBVHNode node = bvhNodes[i];
        if (BoundIsIntersect(node.bound, ray, rayDirIsNeg, invRayDir))
        {
            if (node.primtiveCount == 0)
            {
                if (rayDirIsNeg[node.splitAxis])
                {
                    stack[offset++] = i + 1;
                    i = node.offset;
                }
                else
                {
                    stack[offset++] = node.offset;
                    i++;
                }
            }
            else
            {
                for (int n = node.offset; n < node.offset + node.primtiveCount; n++)
                {
                    Primitive primitive = GetPrimitive(n);
                    if (ShapeIsIntersect(primitive.shape, ray))
                    {
                        isHit = true;
                        break;
                    }
                }
                
                if (isHit)
                {
                    break;
                }
                
                if (offset == 0)
                {
                    break;
                }
                i = stack[--offset];
            }
        }
        else
        {
            if (offset == 0)
            {
                break;
            }
            i = stack[--offset];
        }
    }
    
    return isHit;
}

#endif // __BVH__