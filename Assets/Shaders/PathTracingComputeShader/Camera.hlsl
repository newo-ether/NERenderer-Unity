// Camera.hlsl

#ifndef __CAMERA__
#define __CAMERA__

#include "Ray.hlsl"
#include "Film.hlsl"
#include "Constant.hlsl"
#include "Random.hlsl"

struct Camera
{
    float3 pos;

    float3 look;
    float3 up;
    float3 right;

    float3 screenLowerLeftCorner;
    float screenPlaneWidth;
    float screenPlaneHeight;

    float fov;
    float focalLength;
    float lenRadius;
};

// Uniform Variable
StructuredBuffer<Camera> cameraBuffer;

Ray CameraGetSampleRay(uint2 screenIndex)
{
    Camera camera = cameraBuffer[0];
    float2 sampleOffset = ((float2) screenIndex + float2(Random01(), Random01())) / (float2) GetFilmGeometry();
    float3 samplePoint = camera.screenLowerLeftCorner
                        + camera.right * sampleOffset.x * camera.screenPlaneWidth
                        + camera.up * sampleOffset.y * camera.screenPlaneHeight;

    return RayInit(camera.pos, normalize(samplePoint - camera.pos), 0.0f, INF);
}

#endif // __CAMERA__