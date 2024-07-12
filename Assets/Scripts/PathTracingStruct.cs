// PathTracingStruct.cs

using System;
using System.Runtime.InteropServices;

using UnityEngine;

namespace PathTracingStruct
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PathTracingCamera
    {
        public Vector3 pos;

        public Vector3 look;
        public Vector3 up;
        public Vector3 right;

        public Vector3 screenLowerLeftCorner;
        public float screenPlaneWidth;
        public float screenPlaneHeight;

        public float fov;
        public float focalLength;
        public float lenRadius;

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public bool Equals(PathTracingCamera cam)
        {
            return pos == cam.pos
                && look == cam.look
                && up == cam.up
                && right == cam.right
                && fov == cam.fov
                && focalLength == cam.focalLength
                && lenRadius == cam.lenRadius;
        }

        public override int GetHashCode() => (pos,
                                              look,
                                              up,
                                              right,
                                              fov,
                                              focalLength,
                                              lenRadius)
                                              .GetHashCode();

        public static bool operator ==(PathTracingCamera cameraA, PathTracingCamera cameraB)
        {
            return cameraA.pos == cameraB.pos
                   && cameraA.look == cameraB.look
                   && cameraA.up == cameraB.up
                   && cameraA.right == cameraB.right
                   && cameraA.fov == cameraB.fov
                   && cameraA.focalLength == cameraB.focalLength
                   && cameraA.lenRadius == cameraB.lenRadius;
        }

        public static bool operator !=(PathTracingCamera cameraA, PathTracingCamera cameraB)
        {
            return cameraA.pos != cameraB.pos
                   || cameraA.look != cameraB.look
                   || cameraA.up != cameraB.up
                   || cameraA.right != cameraB.right
                   || cameraA.fov != cameraB.fov
                   || cameraA.focalLength != cameraB.focalLength
                   || cameraA.lenRadius != cameraB.lenRadius;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PathTracingRenderOption
    {
        public int maxDepth;
        public float russianRoulete;

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public bool Equals(PathTracingRenderOption option)
        {
            return maxDepth == option.maxDepth && russianRoulete == option.russianRoulete;
        }

        public override int GetHashCode() => (maxDepth, russianRoulete).GetHashCode();

        public static bool operator ==(PathTracingRenderOption optionA, PathTracingRenderOption optionB)
        {
            return optionA.maxDepth == optionB.maxDepth && optionA.russianRoulete == optionB.russianRoulete;
        }

        public static bool operator !=(PathTracingRenderOption optionA, PathTracingRenderOption optionB)
        {
            return optionA.maxDepth != optionB.maxDepth || optionA.russianRoulete != optionB.russianRoulete;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PathTracingShape
    {
        public Vector3 v0;
        public Vector3 v1;
        public Vector3 v2;

        public Vector3 this[int index]
        {
            get
            {
                return index switch
                {
                    0 => v0,
                    1 => v1,
                    2 => v2,
                    _ => throw new IndexOutOfRangeException("PathTracingShape index out of range."),
                };
            }

            set
            {
                switch (index)
                {
                    case 0: v0 = value; break;
                    case 1: v1 = value; break;
                    case 2: v2 = value; break;
                    default: throw new IndexOutOfRangeException("PathTracingShape index out of range.");
                }
            }
        }
    }

    public enum BSDFType
    {
        Lambertian = 0,
        Specular = 1,
        Microfacet = 2
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PathTracingMaterial
    {
        public BSDFType type;
        public Vector3 albedo;
        public float metallic;
        public float roughness;
        public Vector3 F0;
        public float IOR;
        public float transmission;
        public Vector3 emission;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PathTracingPrimitive
    {
        public PathTracingShape shape;
        public PathTracingMaterial material;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PathTracingRay
    {
        public Vector3 origin;
        public Vector3 dir;
        public float tMin;
        public float tMax;

        public bool IsValid()
        {
            return origin != Vector3.zero || dir != Vector3.zero || tMin != 0.0f || tMax != 0.0f;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PathTracingRayInfo
    {
        public PathTracingRay ray;
        public PathTracingRay shadowRay;
        public Vector3 radiance;
        public Vector3 decay;
        public int isHitLight;
        public int isEnd;
    }
}