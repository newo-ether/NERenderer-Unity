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
StructuredBuffer<Primitive> emissivePrimitives;

// Uniform Variable
int primitiveCount;

// Uniform Variable
int emissivePrimitiveCount;

int GetPrimitiveCount()
{
    return primitiveCount;
}

int GetEmissivePrimitiveCount()
{
    return emissivePrimitiveCount;
}

Primitive GetPrimitive(int index)
{
    return primitives[index];
}

Primitive GetEmissivePrimitive(int index)
{
    return emissivePrimitives[index];
}

#endif // __PRIMITIVE__