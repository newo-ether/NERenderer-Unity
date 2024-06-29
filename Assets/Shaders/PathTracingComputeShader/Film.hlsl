// Film.hlsl

#ifndef __FILM__
#define __FILM__

// Uniform Variable
RWTexture2D<float4> renderResult;

// Uniform Variable
uint renderWidth;

// Uniform Variable
uint renderHeight;

uint GetFilmWidth()
{
    return renderWidth;
}

uint GetFilmHeight()
{
    return renderHeight;
}

uint2 GetFilmGeometry()
{
    return uint2(renderWidth, renderHeight);
}

void AccumulateFilmPixel(uint2 index, float4 value, int frameCount)
{
    renderResult[index] = float4(lerp(renderResult[index], value, 1.0f / (float) frameCount));
}

void SetFilmPixel(uint2 index, float4 value)
{
    renderResult[index] = value;
}

#endif // __FILM__