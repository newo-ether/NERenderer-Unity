// PathTracingInvoker.cs

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;

public class PathTracingInvoker : MonoBehaviour
{
    private GameObject displayPlane;
    
    public ComputeShader pathTracingShader;

    private ComputeBuffer pathTracingCameraBuffer;
    private ComputeBuffer pathTracingRenderOptionBuffer;
    private ComputeBuffer pathTracingPrimitivesBuffer;
    private ComputeBuffer pathTracingRandomBuffer;

    public int textureWidth = 1920;
    public int textureHeight = 1080;

    public int maxDepth = 200;
    public float russianRoulete = 0.8f;

    public bool accumulate = true;

    private new Renderer renderer;
    private RenderTexture renderTexture;
    private System.Random random = new();

    private int kernelHandle;
    private int frameCount = 1;

    private PathTracingCamera pathTracingCamera;
    private PathTracingRenderOption pathTracingRenderOption;
    private List<PathTracingPrimitive> pathTracingPrimitives;
    private uint[] pathTracingRandom;

    [StructLayout(LayoutKind.Sequential)]
    private struct PathTracingCamera
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
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PathTracingRenderOption
    {
        public int maxDepth;
        public float russianRoulete;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PathTracingShape
    {
        public Vector3 v0;
        public Vector3 v1;
        public Vector3 v2;

        public Vector3 this[int index]
        {
            get {
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

    private enum BSDFType
    {
        Lambertian = 0,
        Specular   = 1,
        Microfacet = 2
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PathTracingMaterial
    {
        public BSDFType type;
        public Vector3 albedo;
        public float metallic;
        public float roughness;
        public float IOR;
        public float transmission;
        public Vector3 emission;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PathTracingPrimitive
    {
        public PathTracingShape shape;
        public PathTracingMaterial material;
    }

    private GameObject[] GetAllGameObjects()
    {
        return FindObjectsOfType(typeof(GameObject)) as GameObject[];
    }

    private PathTracingMaterial GetPathTracingMaterial(ref Material material)
    {
        return new PathTracingMaterial
        {
            type = (BSDFType)material.GetInt("_BSDFType"),
            albedo = material.GetVector("_Albedo"),
            metallic = material.GetFloat("_Metallic"),
            roughness = material.GetFloat("_Roughness"),
            IOR = material.GetFloat("_IOR"),
            transmission = material.GetFloat("_Transmission"),
            emission = material.GetVector("_Emission")
        };
    }

    private List<PathTracingPrimitive> GetAllPathTracingPrimitives()
    {
        GameObject[] gameObjects = GetAllGameObjects();
        List<PathTracingPrimitive> primitives = new();
        foreach (GameObject gameObject in gameObjects)
        {
            if (gameObject == displayPlane)
            {
                continue;
            }

            if (!gameObject.TryGetComponent<MeshFilter>(out MeshFilter meshFilter)
             || !gameObject.TryGetComponent<MeshRenderer>(out MeshRenderer meshRenderer))
            {
                continue;
            }

            Mesh mesh = meshFilter.sharedMesh;
            Material mat = meshRenderer.sharedMaterial;

            if (mesh == null || mat == null || mat.shader.name != "Custom/Path Tracing Shader")
            {
                continue;
            }

            PathTracingMaterial pathTracingMaterial = GetPathTracingMaterial(ref mat);
            Transform transform = gameObject.transform;
            Vector3[] vertices = mesh.vertices;
            int[] faces = mesh.triangles;

            for (int i = 0; i < faces.Length; i += 3)
            {
                primitives.Add(new PathTracingPrimitive
                {
                    shape = new PathTracingShape
                    {
                        [0] = transform.TransformPoint(vertices[faces[i]]),
                        [1] = transform.TransformPoint(vertices[faces[i + 1]]),
                        [2] = transform.TransformPoint(vertices[faces[i + 2]]),
                    },
                    material = pathTracingMaterial
                });
            }
        }
        return primitives;
    }

    private void UpdateDisplayPlane()
    {
        float aspect = GetComponent<Camera>().aspect;
        float fov = GetComponent<Camera>().fieldOfView;
        float near = GetComponent<Camera>().nearClipPlane * 1.0001f;
        float nearHeight = near * Mathf.Tan((fov * 0.5f) * Mathf.Deg2Rad) * 2.0f;
        float nearWidth = nearHeight * aspect;
        Matrix4x4 matrix = GetComponent<Camera>().transform.localToWorldMatrix
                           * Matrix4x4.Translate(new Vector3(0.0f, 0.0f, near));

        displayPlane.transform.SetLocalPositionAndRotation(matrix.GetPosition(), matrix.rotation);
        displayPlane.transform.localScale = new Vector3(nearWidth, nearHeight, 1.0f);
    }

    private PathTracingCamera UpdatePathTracingCamera()
    {
        Camera camera = Camera.main;
        float aspect = camera.aspect;

        Vector3 pos = camera.transform.position;
        Vector3 look = camera.transform.forward;
        Vector3 up = camera.transform.up;
        Vector3 right = camera.transform.right;

        float fovVertical = camera.fieldOfView;
        float focalLength = camera.focalLength;

        Vector3 screenPlaneOrigin = pos + look * focalLength;
        float screenPlaneHeight = 2.0f * Mathf.Tan(fovVertical * 0.5f * Mathf.Deg2Rad) * focalLength;
        float screenPlaneWidth = screenPlaneHeight * aspect;
        float fovHorizontal = Mathf.Atan(screenPlaneWidth * 0.5f / focalLength) * Mathf.Rad2Deg * 2.0f;
        Vector3 screenLowerLeftCorner = screenPlaneOrigin
                                        - 0.5f * screenPlaneWidth * right
                                        - 0.5f * screenPlaneHeight * up;

        return new PathTracingCamera
        {
            pos = pos,

            look = look,
            up = up,
            right = right,

            screenLowerLeftCorner = screenLowerLeftCorner,
            screenPlaneWidth = screenPlaneWidth,
            screenPlaneHeight = screenPlaneHeight,

            fov = fovHorizontal,
            focalLength = focalLength,
            lenRadius = 0.1f
        };
    }

    private PathTracingRenderOption UpdatePathTracingRenderOption()
    {
        return new PathTracingRenderOption
        {
            maxDepth = maxDepth,
            russianRoulete = russianRoulete
        };
    }

    private uint[] GenerateRandomNumbers()
    {
        uint[] randomNumbers = new uint[textureWidth * textureHeight];
        for (int i = 0; i < textureWidth; i++)
        {
            for (int j = 0; j < textureHeight; j++)
            {
                randomNumbers[j * textureWidth + i] = (uint) random.Next();
            }
        }
        return randomNumbers;
    }

    private RenderTexture CreateRenderTexture()
    {
        RenderTexture texture;
        texture = new RenderTexture(textureWidth, textureHeight, 0);
        texture.enableRandomWrite = true;
        texture.Create();

        return texture;
    }

    private void Start()
    {
        // Get Scene Primitives
        pathTracingPrimitives = GetAllPathTracingPrimitives();

        // Generate Ramdom Numbers
        pathTracingRandom = GenerateRandomNumbers();
        
        // Create Render Texture
        renderTexture = CreateRenderTexture();

        // Create Compute Buffers
        pathTracingCameraBuffer = new ComputeBuffer(1, UnsafeUtility.SizeOf<PathTracingCamera>());
        pathTracingRenderOptionBuffer = new ComputeBuffer(1, UnsafeUtility.SizeOf<PathTracingRenderOption>());
        pathTracingPrimitivesBuffer = new ComputeBuffer(pathTracingPrimitives.Count, UnsafeUtility.SizeOf<PathTracingPrimitive>());
        pathTracingRandomBuffer = new ComputeBuffer(textureWidth * textureHeight, sizeof(uint));

        // Create Display Plane
        displayPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);

        // Bind Render Texture to Compute Shader
        kernelHandle = pathTracingShader.FindKernel("Render");
        pathTracingShader.SetTexture(kernelHandle, "renderResult", renderTexture);

        // Bind Render Texture to Display Plane
        renderer = displayPlane.GetComponent<MeshRenderer>();
        renderer.material = new Material(Shader.Find("Unlit/Texture"));
        renderer.enabled = true;
        renderer.material.SetTexture("_MainTex", renderTexture);
    }

    private void Update()
    {
        // Update Display Plane
        UpdateDisplayPlane();

        // Update Camera Data
        pathTracingCamera = UpdatePathTracingCamera();

        // Update Render Option Data
        pathTracingRenderOption = UpdatePathTracingRenderOption();

        // Setup Camera Buffer
        pathTracingCameraBuffer.SetData(new PathTracingCamera[] { pathTracingCamera });
        pathTracingShader.SetBuffer(kernelHandle, "cameraBuffer", pathTracingCameraBuffer);

        // Setup RenderOption Buffer
        pathTracingRenderOptionBuffer.SetData(new PathTracingRenderOption[] { pathTracingRenderOption });
        pathTracingShader.SetBuffer(kernelHandle, "renderOptionBuffer", pathTracingRenderOptionBuffer);

        // Setup Primitives Buffer
        pathTracingPrimitivesBuffer.SetData(pathTracingPrimitives);
        pathTracingShader.SetBuffer(kernelHandle, "primitives", pathTracingPrimitivesBuffer);

        // Setup Random Buffer
        pathTracingRandomBuffer.SetData(pathTracingRandom);
        pathTracingShader.SetBuffer(kernelHandle, "randomBuffer", pathTracingRandomBuffer);

        // Setup Basic Variables
        pathTracingShader.SetBool("accumulate", accumulate);
        pathTracingShader.SetInt("frameCount", frameCount);
        pathTracingShader.SetInt("renderWidth", textureWidth);
        pathTracingShader.SetInt("renderHeight", textureHeight);
        pathTracingShader.SetInt("primitiveCount", pathTracingPrimitives.Count);
        pathTracingShader.SetInt("extraSeed", random.Next());
        
        // Execute Shader
        pathTracingShader.Dispatch(kernelHandle, textureWidth / 20, textureHeight / 20, 1);

        // Increase Frame Count
        frameCount++;
    }
}
