// Primitive.hlsl

#ifndef __PRIMITIVE__
#define __PRIMITIVE__

#include "Shape.hlsl"
#include "Material.hlsl"

struct Primitive
{
    Shape shape;
    Material material;
};

// Uniform Variable
StructuredBuffer<Primitive> primitives;

// Uniform Variable
int primitiveCount;

int GetPrimitiveCount()
{
    return primitiveCount;
}

Primitive GetPrimitive(int index)
{
    return primitives[index];
}

#endif // __PRIMITIVE__