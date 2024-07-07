// PathTracingInvoker.cs

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;

using Drawing;
using UnityEditor;

public class PathTracingInvoker : MonoBehaviour
{
    private GameObject displayPlane;
    public Shader unlitShader;
    
    public ComputeShader pathTracingShader;

    private ComputeBuffer pathTracingCameraBuffer;
    private ComputeBuffer pathTracingRenderOptionBuffer;
    private ComputeBuffer pathTracingPrimitivesBuffer;
    private ComputeBuffer pathTracingEmissivePrimitivesBuffer;
    private ComputeBuffer pathTracingRandomBuffer;
    private ComputeBuffer pathTracingRayInfoBuffer;

    public int textureWidth = 1920;
    public int textureHeight = 1080;

    public int maxDepth = 10;
    public float russianRoulete = 0.9f;

    private string maxDepthString;
    private string russianRouleteString;

    [Range(0.0f, 1.0f)]
    public float opacity = 1.0f;
    private string opacityString;

    public bool accumulate = true;
    private bool lastAccumulate = true;

    private new Renderer renderer;
    private RenderTexture renderTexture;
    private Texture2D textureCache;
    private System.Random random = new();

    private int kernelHandle;
    private int frameCount = 1;

    public bool pauseOnStart = false;

    public bool showDebugWindow = true;
    private Rect debugWindowRect = new Rect(10, 10, 500, 0);
    private FrameState frameState;
    private bool debuggingRay = false;
    private int invalidCount = 0;
    private Vector2 selectedPixelIndex;
    private Vector2 selectedPixelOffset;
    private bool selectingPixel = false;
    private bool isPixelSelected = false;
    private Vector2 scrollPosition = new Vector2(0.0f, 0.0f);
    private Vector3 cameraPosition;
    private Quaternion cameraRotation;

    private PathTracingCamera pathTracingCamera;
    private PathTracingRenderOption pathTracingRenderOption;
    private List<PathTracingPrimitive> pathTracingPrimitives;
    private List<PathTracingPrimitive> pathTracingEmissivePrimitives;
    private PathTracingRayInfo[] pathTracingRayInfo;
    private uint[] pathTracingRandom;

    private enum FrameState
    {
        Run = 0,
        Pause = 1,
        Next = 2
    }

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

        public static bool operator==(PathTracingCamera cameraA, PathTracingCamera cameraB)
        {
            return cameraA.pos == cameraB.pos
                   && cameraA.look == cameraB.look
                   && cameraA.up == cameraB.up
                   && cameraA.right == cameraB.right
                   && cameraA.fov == cameraB.fov
                   && cameraA.focalLength == cameraB.focalLength
                   && cameraA.lenRadius == cameraB.lenRadius;
        }

        public static bool operator!=(PathTracingCamera cameraA, PathTracingCamera cameraB)
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
    private struct PathTracingRenderOption
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

        public static bool operator==(PathTracingRenderOption optionA, PathTracingRenderOption optionB)
        {
            return optionA.maxDepth == optionB.maxDepth && optionA.russianRoulete == optionB.russianRoulete;
        }

        public static bool operator!=(PathTracingRenderOption optionA, PathTracingRenderOption optionB)
        {
            return optionA.maxDepth != optionB.maxDepth || optionA.russianRoulete != optionB.russianRoulete;
        }
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

    [StructLayout(LayoutKind.Sequential)]
    private struct PathTracingRay
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
    private struct PathTracingRayInfo
    {
        public PathTracingRay ray;
        public PathTracingRay shadowRay;
        public Vector3 radiance;
        public Vector3 decay;
        public int isHitLight;
        public int isEnd;
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

    List<PathTracingPrimitive> PickAllEmissivePrimitives(List<PathTracingPrimitive> allPrimitives)
    {
        List<PathTracingPrimitive> emissivePrimitives = new List<PathTracingPrimitive>();
        foreach (PathTracingPrimitive primitive in allPrimitives)
        {
            if (primitive.material.emission != Vector3.zero)
            {
                emissivePrimitives.Add(primitive);
            }
        }
        return emissivePrimitives;
    }

    private void UpdateDisplayPlane()
    {
        float aspect = GetComponent<Camera>().aspect;
        float fov = GetComponent<Camera>().fieldOfView;
        float near = GetComponent<Camera>().nearClipPlane * 1.001f;
        float nearHeight = near * Mathf.Tan((fov * 0.5f) * Mathf.Deg2Rad) * 2.0f;
        float nearWidth = nearHeight * aspect;
        Matrix4x4 matrix = GetComponent<Camera>().transform.localToWorldMatrix
                           * Matrix4x4.Translate(new Vector3(0.0f, 0.0f, near));

        displayPlane.transform.SetLocalPositionAndRotation(matrix.GetPosition(), matrix.rotation);
        displayPlane.transform.localScale = new Vector3(nearWidth, nearHeight, 1.0f);
    }

    private PathTracingCamera UpdatePathTracingCamera()
    {
        float aspect = GetComponent<Camera>().aspect;

        Vector3 pos = GetComponent<Camera>().transform.position;

        Vector3 look = GetComponent<Camera>().transform.forward;
        Vector3 up = GetComponent<Camera>().transform.up;
        Vector3 right = GetComponent<Camera>().transform.right;

        float fovVertical = GetComponent<Camera>().fieldOfView;
        float focalLength = GetComponent<Camera>().focalLength;

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
        texture.format = RenderTextureFormat.ARGBFloat;
        texture.filterMode = FilterMode.Point;
        texture.Create();

        return texture;
    }

    private Vector2 GetScreenGeometry()
    {
        return new Vector2(Screen.width, Screen.height);
    }

    private Vector2 GetMouseScreenOffset()
    {
        Vector2 pixel = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
        Vector2 screen = GetScreenGeometry();
        pixel = new Vector2(Mathf.Clamp(pixel.x, 0.0f, screen.x), screen.y - Mathf.Clamp(pixel.y, 0.0f, screen.y));
        return pixel / screen;
    }

    private Vector2 GetMousePositionTextureIndex()
    {
        Vector2 index = GetMouseScreenOffset() * new Vector2(textureWidth, textureHeight);
        return new Vector2(Mathf.Clamp(Mathf.Floor(index.x), 0.0f, textureWidth - 1.0f),
                           Mathf.Clamp(Mathf.Floor(index.y), 0.0f, textureHeight - 1.0f));
    }

    private void Start()
    {
        // Get Camera Data
        pathTracingCamera = UpdatePathTracingCamera();

        // Get Camera Transform
        cameraPosition = GetComponent<Camera>().transform.position;
        cameraRotation = GetComponent<Camera>().transform.rotation;

        // Get Render Option Data
        pathTracingRenderOption = UpdatePathTracingRenderOption();

        // Setup GUI Strings
        maxDepthString = maxDepth.ToString();
        russianRouleteString = russianRoulete.ToString();
        opacityString = opacity.ToString();

        // Setup Frame State
        frameState = pauseOnStart ? FrameState.Pause : FrameState.Run;

        // Get Scene Primitives
        pathTracingPrimitives = GetAllPathTracingPrimitives();

        // Pick Emissive Primitives
        pathTracingEmissivePrimitives = PickAllEmissivePrimitives(pathTracingPrimitives);

        // Generate Ramdom Numbers
        pathTracingRandom = GenerateRandomNumbers();

        // Create Ray Array
        pathTracingRayInfo = new PathTracingRayInfo[textureWidth * textureHeight * maxDepth];
        
        // Create Render Texture
        renderTexture = CreateRenderTexture();

        // Create Compute Buffers
        pathTracingCameraBuffer = new ComputeBuffer(1, UnsafeUtility.SizeOf<PathTracingCamera>());
        pathTracingRenderOptionBuffer = new ComputeBuffer(1, UnsafeUtility.SizeOf<PathTracingRenderOption>());
        pathTracingPrimitivesBuffer = new ComputeBuffer(pathTracingPrimitives.Count, UnsafeUtility.SizeOf<PathTracingPrimitive>());
        pathTracingEmissivePrimitivesBuffer = new ComputeBuffer(pathTracingEmissivePrimitives.Count, UnsafeUtility.SizeOf<PathTracingPrimitive>());
        pathTracingRandomBuffer = new ComputeBuffer(textureWidth * textureHeight, sizeof(uint));
        pathTracingRayInfoBuffer = new ComputeBuffer(textureWidth * textureHeight * maxDepth, UnsafeUtility.SizeOf<PathTracingRayInfo>());

        // Setup Camera Buffer
        pathTracingCameraBuffer.SetData(new PathTracingCamera[] { pathTracingCamera });
        pathTracingShader.SetBuffer(kernelHandle, "cameraBuffer", pathTracingCameraBuffer);

        // Setup Render Option Buffer
        pathTracingRenderOptionBuffer.SetData(new PathTracingRenderOption[] { pathTracingRenderOption });
        pathTracingShader.SetBuffer(kernelHandle, "renderOptionBuffer", pathTracingRenderOptionBuffer);

        // Setup Primitives Buffer
        pathTracingPrimitivesBuffer.SetData(pathTracingPrimitives);
        pathTracingShader.SetBuffer(kernelHandle, "primitives", pathTracingPrimitivesBuffer);

        // Setup Emissive Primitives Buffer
        pathTracingEmissivePrimitivesBuffer.SetData(pathTracingEmissivePrimitives);
        pathTracingShader.SetBuffer(kernelHandle, "emissivePrimitives", pathTracingEmissivePrimitivesBuffer);

        // Setup Ray Buffer
        pathTracingShader.SetBuffer(kernelHandle, "rayInfoBuffer", pathTracingRayInfoBuffer);

        // Setup Basic Variables
        pathTracingShader.SetBool("accumulate", accumulate);
        pathTracingShader.SetInt("renderWidth", textureWidth);
        pathTracingShader.SetInt("renderHeight", textureHeight);
        pathTracingShader.SetInt("primitiveCount", pathTracingPrimitives.Count);
        pathTracingShader.SetInt("emissivePrimitiveCount", pathTracingEmissivePrimitives.Count);

        // Setup Random Buffer
        pathTracingRandomBuffer.SetData(pathTracingRandom);
        pathTracingShader.SetBuffer(kernelHandle, "randomBuffer", pathTracingRandomBuffer);

        // Create Texture Cache
        textureCache = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBAFloat, false);

        // Create Display Plane
        displayPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);

        // Bind Render Texture to Compute Shader
        kernelHandle = pathTracingShader.FindKernel("Render");
        pathTracingShader.SetTexture(kernelHandle, "renderResult", renderTexture);

        // Bind Render Texture to Display Plane
        renderer = displayPlane.GetComponent<MeshRenderer>();
        renderer.material = new Material(unlitShader);
        renderer.enabled = true;
        renderer.material.SetTexture("_MainTex", renderTexture);
        renderer.material.SetFloat("_Opacity", opacity);
    }

    private void Update()
    {
        // Update Display Plane
        UpdateDisplayPlane();

        // Update Display Plane Opacity
        renderer.material.SetFloat("_Opacity", opacity);

        // Draw Debug Rays
        if (frameState == FrameState.Pause)
        {
            if (debuggingRay && isPixelSelected)
            {
                float aspect = GetComponent<Camera>().aspect;
                float fov = GetComponent<Camera>().fieldOfView;
                float near = GetComponent<Camera>().nearClipPlane * 1.001f;
                float nearHeight = near * Mathf.Tan((fov * 0.5f) * Mathf.Deg2Rad) * 2.0f;
                float nearWidth = nearHeight * aspect;
                Matrix4x4 cameraToWorld = GetComponent<Camera>().transform.localToWorldMatrix;
                Vector2 mouseOffset = selectedPixelOffset - new Vector2(0.5f, 0.5f);
                Vector2 pixelGeometry = new Vector2(nearWidth / textureWidth, nearHeight / textureHeight);

                Vector3 center = pixelGeometry
                                 * new Vector2(Mathf.Floor(mouseOffset.x * textureWidth) + 0.5f,
                                               Mathf.Floor(mouseOffset.y * textureHeight) + 0.5f);

                center = cameraToWorld.MultiplyPoint(new Vector3(center.x, center.y, near));
                Vector3 normal = cameraToWorld.MultiplyVector(new Vector3(0.0f, 0.0f, -1.0f));

                Draw.ingame.WirePlane(center,
                                      normal,
                                      pixelGeometry,
                                      new Color(0.8f, 0.8f, 0.2f));

                int offset = ((int)selectedPixelIndex.y * textureWidth + (int)selectedPixelIndex.x) * maxDepth;
                for (int i = 0; i < maxDepth; i++)
                {
                    PathTracingRayInfo rayInfo = pathTracingRayInfo[offset + i];
                    PathTracingRay ray = rayInfo.ray;
                    PathTracingRay shadowRay = rayInfo.shadowRay;

                    // Draw Ray
                    if (ray.IsValid())
                    {
                        Vector3 begin = ray.origin + ray.dir * ray.tMin;
                        Vector3 end = ray.origin + ray.dir * ray.tMax;
                        Draw.ingame.Line(begin, end, new Color(0.8f, 0.8f, 0.2f));
                    }

                    // Draw Shadow Ray
                    if (shadowRay.IsValid())
                    {
                        Vector3 begin = shadowRay.origin + shadowRay.dir * shadowRay.tMin;
                        Vector3 end = shadowRay.origin + shadowRay.dir * shadowRay.tMax;
                        Draw.ingame.Line(begin, end, new Color(0.8f, 0.8f, 0.8f));
                    }

                    if (rayInfo.isEnd == 1)
                    {
                        break;
                    }
                }
            }
        }
        else
        {
            // Update Camera Data
            PathTracingCamera newPathTracingCamera = UpdatePathTracingCamera();
            if (newPathTracingCamera != pathTracingCamera)
            {
                pathTracingCamera = newPathTracingCamera;
                pathTracingCameraBuffer.SetData(new PathTracingCamera[] { pathTracingCamera });
                frameCount = 1;
            }

            // Update Render Option Data
            PathTracingRenderOption newPathTracingRenderOption = UpdatePathTracingRenderOption();
            if (newPathTracingRenderOption != pathTracingRenderOption)
            {
                pathTracingRenderOption = newPathTracingRenderOption;
                pathTracingRenderOptionBuffer.SetData(new PathTracingRenderOption[] { pathTracingRenderOption });

                if (newPathTracingRenderOption.maxDepth != pathTracingRenderOption.maxDepth)
                {
                    pathTracingRayInfo = new PathTracingRayInfo[textureWidth * textureHeight * maxDepth];
                    pathTracingRayInfoBuffer.Dispose();
                    pathTracingRayInfoBuffer = new ComputeBuffer(textureWidth * textureHeight * maxDepth, UnsafeUtility.SizeOf<PathTracingRayInfo>());
                    pathTracingShader.SetBuffer(kernelHandle, "rayInfoBuffer", pathTracingRayInfoBuffer);
                }

                frameCount = 1;
            }

            // Update Accumulate
            if (accumulate != lastAccumulate)
            {
                lastAccumulate = accumulate;
                pathTracingShader.SetBool("accumulate", accumulate);
                frameCount = 1;
            }

            // Setup Basic Variables
            pathTracingShader.SetInt("frameCount", frameCount);
            pathTracingShader.SetInt("extraSeed", random.Next());

            // Execute Shader
            pathTracingShader.Dispatch(kernelHandle, textureWidth / 20, textureHeight / 20, 1);

            // Save Texture Cache
            StartCoroutine(SaveTextureCache());

            // Save Camera Transform
            cameraPosition = GetComponent<Camera>().transform.position;
            cameraRotation = GetComponent<Camera>().transform.rotation;

            // Increase Frame Count
            frameCount += 1;

            // Pause If FrameState is Set to Next
            if (frameState == FrameState.Next)
            {
                frameState = FrameState.Pause;
                invalidCount = CountInvalidPixel();
                DebugInit();
            }
        }
    }

    private IEnumerator SaveTextureCache()
    {
        yield return new WaitForEndOfFrame();

        RenderTexture.active = renderTexture;

        textureCache.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);
        textureCache.Apply();
    }

    void OnDestroy()
    {
        pathTracingCameraBuffer.Dispose();
        pathTracingRenderOptionBuffer.Dispose();
        pathTracingPrimitivesBuffer.Dispose();
        pathTracingEmissivePrimitivesBuffer.Dispose();
        pathTracingRandomBuffer.Dispose();
        pathTracingRayInfoBuffer.Dispose();
    }

    void OnGUI()
    {
        if (showDebugWindow)
        {
            debugWindowRect = GUILayout.Window(0, debugWindowRect, DebugWindow, "Debug Window", GUILayout.Width(500), GUILayout.Height(0));
        }
    }

    void DebugInit()
    {
        pathTracingRayInfoBuffer.GetData(pathTracingRayInfo);
    }

    int CountInvalidPixel()
    {
        int count = 0;

        for (int i = 0; i < textureWidth; i++)
        {
            for (int j = 0; j < textureHeight; j++)
            {
                Color color = textureCache.GetPixel(i, j);
                if (color.r < 0.0f || color.g < 0.0f || color.b < 0.0f
                 || float.IsInfinity(color.r) || float.IsInfinity(color.g) || float.IsInfinity(color.b)
                 || float.IsNaN(color.r) || float.IsNaN(color.g) || float.IsNaN(color.b))
                {
                    count++;
                }
            }
        }

        return count;
    }

    void DebugWindow(int windowID)
    {
        // Begin Accumulate
        GUILayout.BeginHorizontal();

        GUILayout.Label("Accumulate");

        GUI.enabled = frameState == FrameState.Run;

        accumulate = GUILayout.Toggle(accumulate, " Toggle Frame Accumulation", GUILayout.Width(226));
        
        GUI.enabled = true;

        GUILayout.EndHorizontal();
        // End Accumulate


        // Begin Max Depth
        GUILayout.BeginHorizontal();
        
        GUILayout.Label("Max Depth");

        GUI.enabled = frameState == FrameState.Run;

        if (GUILayout.Button("+10", GUILayout.Width(40)))
        {
            maxDepth = Math.Max(maxDepth + 10, 1);
            maxDepthString = maxDepth.ToString();
        }
        if (GUILayout.Button("+1", GUILayout.Width(40)))
        {
            maxDepth = Math.Max(maxDepth + 1, 1);
            maxDepthString = maxDepth.ToString();
        }
        
        maxDepthString = GUILayout.TextArea(maxDepthString, GUILayout.Width(50));
        int.TryParse(maxDepthString, out maxDepth);
        maxDepth = Math.Max(1, maxDepth);

        if (GUILayout.Button("-1", GUILayout.Width(40)))
        {
            maxDepth = Math.Max(maxDepth - 1, 1);
            maxDepthString = maxDepth.ToString();
        }
        if (GUILayout.Button("-10", GUILayout.Width(40)))
        {
            maxDepth = Math.Max(maxDepth - 10, 1);
            maxDepthString = maxDepth.ToString();
        }

        GUI.enabled = true;

        GUILayout.EndHorizontal();
        // End Max Depth


        // Begin Russian Roulete
        GUILayout.BeginHorizontal();

        GUILayout.Label("Russian Roulete");

        GUI.enabled = frameState == FrameState.Run;

        if (GUILayout.Button("+.01", GUILayout.Width(40)))
        {
            russianRoulete = Mathf.Clamp01(russianRoulete + 0.01f);
            russianRouleteString = russianRoulete.ToString();
        }
        if (GUILayout.Button("+.1", GUILayout.Width(40)))
        {
            russianRoulete = Mathf.Clamp01(russianRoulete + 0.1f);
            russianRouleteString = russianRoulete.ToString();
        }

        russianRouleteString = GUILayout.TextArea(russianRouleteString, GUILayout.Width(50));
        float.TryParse(russianRouleteString, out russianRoulete);
        russianRoulete = Mathf.Clamp01(russianRoulete);

        if (GUILayout.Button("-.1", GUILayout.Width(40)))
        {
            russianRoulete = Mathf.Clamp01(russianRoulete - 0.1f);
            russianRouleteString = russianRoulete.ToString();
        }
        if (GUILayout.Button("-.01", GUILayout.Width(40)))
        {
            russianRoulete = Mathf.Clamp01(russianRoulete - 0.01f);
            russianRouleteString = russianRoulete.ToString();
        }

        GUI.enabled = true;

        GUILayout.EndHorizontal();
        // End Russian Roulete
        

        // Begin Opacity
        GUILayout.BeginHorizontal();

        GUILayout.Label("Opacity");

        if (GUILayout.Button("+.01", GUILayout.Width(40)))
        {
            opacity = Mathf.Clamp01(opacity + 0.01f);
            opacityString = opacity.ToString();
        }
        if (GUILayout.Button("+.1", GUILayout.Width(40)))
        {
            opacity = Mathf.Clamp01(opacity + 0.1f);
            opacityString = opacity.ToString();
        }

        opacityString = GUILayout.TextArea(opacityString, GUILayout.Width(50));
        float.TryParse(opacityString, out opacity);
        opacity = Mathf.Clamp01(opacity);

        if (GUILayout.Button("-.1", GUILayout.Width(40)))
        {
            opacity = Mathf.Clamp01(opacity - 0.1f);
            opacityString = opacity.ToString();
        }
        if (GUILayout.Button("-.01", GUILayout.Width(40)))
        {
            opacity = Mathf.Clamp01(opacity - 0.01f);
            opacityString = opacity.ToString();
        }

        GUILayout.EndHorizontal();
        // End Opacity


        // Begin Frame
        GUILayout.BeginHorizontal();

        GUILayout.Label("Frame Count: " + (frameCount - 1).ToString());

        if (frameState == FrameState.Run)
        {
            if (GUILayout.Button("Pause", GUILayout.Width(226)))
            {
                frameState = FrameState.Pause;
                invalidCount = CountInvalidPixel();
            }
        }
        else
        {
            if (GUILayout.Button("Continue", GUILayout.Width(73)))
            {
                frameState = FrameState.Run;

                debuggingRay = false;
                selectingPixel = false;
                isPixelSelected = false;

                GetComponent<Transform>().SetPositionAndRotation(cameraPosition, cameraRotation);
            }

            if (GUILayout.Button("Reset", GUILayout.Width(73)))
            {
                frameState = FrameState.Run;
                frameCount = 1;

                debuggingRay = false;
                selectingPixel = false;
                isPixelSelected = false;

                GetComponent<Transform>().SetPositionAndRotation(cameraPosition, cameraRotation);
            }

            if (GUILayout.Button("Next", GUILayout.Width(73)))
            {
                frameState = FrameState.Next;

                GetComponent<Transform>().SetPositionAndRotation(cameraPosition, cameraRotation);
            }
        }

        GUILayout.EndHorizontal();


        // Begin Debug Ray
        if (frameState != FrameState.Run)
        {
            GUILayout.BeginHorizontal();

            GUILayout.Label("Debug Ray");

            GUI.color = new Color(0.9f, 0.1f, 0.0f);

            if (invalidCount > 0)
            {
                GUILayout.Label(invalidCount.ToString() + " Invalid Pixel(s)");
            }

            GUI.color = Color.white;

            string debugRayText = debuggingRay ? "End Debugging" : "Start Debugging";

            if (GUILayout.Button(debugRayText, GUILayout.Width(226)))
            {
                if (debuggingRay)
                {
                    debuggingRay = false;
                    selectingPixel = false;
                    isPixelSelected = false;
                }
                else
                {
                    debuggingRay = true;
                    selectingPixel = false;
                    isPixelSelected = false;

                    DebugInit();
                }
            }

            GUILayout.EndHorizontal();
        }
        // End Debug Ray


        // Begin Select Pixel
        if (debuggingRay)
        {
            GUILayout.BeginHorizontal();

            string selectPixelText = selectingPixel ? "Cancel Selecting Pixel" : "Select Pixel";

            if (GUILayout.Button(selectPixelText))
            {
                if (selectingPixel)
                {
                    selectingPixel = false;
                    isPixelSelected = false;
                }
                else
                {
                    selectingPixel = true;
                    isPixelSelected = false;
                }
            }

            if (GUILayout.Button("Restore Camera Transform", GUILayout.Width(226)))
            {
                GetComponent<Transform>().SetPositionAndRotation(cameraPosition, cameraRotation);
            }

            GUILayout.EndHorizontal();
        }
        // End Select Pixel


        // Begin Pixel Label
        GUILayout.BeginHorizontal();

        if (selectingPixel)
        {
            selectedPixelIndex = GetMousePositionTextureIndex();
            selectedPixelOffset = GetMouseScreenOffset();
            isPixelSelected = true;

            if (Input.GetMouseButtonDown(0) && !debugWindowRect.Contains(GUIUtility.GUIToScreenPoint(Event.current.mousePosition)))
            {
                selectingPixel = false;
            }

            GUILayout.Label("Selecting Pixel: ("
                            + ((int)selectedPixelIndex.x).ToString()
                            + ", "
                            + ((int)selectedPixelIndex.y).ToString() + ")");
        }

        if (isPixelSelected)
        {
            if (!selectingPixel)
            {
                GUILayout.Label("Pixel Selected: ("
                                + ((int)selectedPixelIndex.x).ToString()
                                + ", "
                                + ((int)selectedPixelIndex.y).ToString() + ")");
            }

            GUILayout.Label("Color: " + textureCache.GetPixel((int)selectedPixelIndex.x, (int)selectedPixelIndex.y).ToString());
        }

        GUILayout.EndHorizontal();
        // End Pixel Label


        // Begin Ray Path
        if (isPixelSelected)
        {
            string text = "";
            int offset = ((int)selectedPixelIndex.y * textureWidth + (int)selectedPixelIndex.x) * maxDepth;
            for (int i = 0; i < maxDepth; i++)
            {
                PathTracingRayInfo rayInfo = pathTracingRayInfo[offset + i];
                PathTracingRay ray = rayInfo.ray;
                PathTracingRay shadowRay = rayInfo.shadowRay;
                Vector3 rayBegin = ray.origin + ray.dir * ray.tMin;
                Vector3 rayEnd = ray.origin + ray.dir * ray.tMax;
                Vector3 shadowRayBegin = shadowRay.origin + shadowRay.dir * shadowRay.tMin;
                Vector3 shadowRayEnd = shadowRay.origin + shadowRay.dir * shadowRay.tMax;

                string depthText = "[" + i.ToString() + "] ";
                string padding = " ";
                
                for (int n = 0; n < depthText.Length; n++)
                {
                    padding += " ";
                }

                if (ray.IsValid())
                {
                    text += depthText + "Ray { Begin = " + rayBegin.ToString() + "  End = " + rayEnd.ToString() + " }\n";
                }

                if (shadowRay.IsValid())
                {
                    text += padding + "ShadowRay { " + "Begin = " + shadowRayBegin.ToString() + "  End = " + shadowRayEnd.ToString() + " }\n";
                }

                if (rayInfo.isHitLight == 0)
                {
                    if (shadowRay.IsValid())
                    {
                        text += padding + "ShadowRayRadiance = " + rayInfo.radiance.ToString() + "\n";
                    }
                }
                else
                {
                    if (ray.IsValid())
                    {
                        text += padding + "RayRadiance = " + rayInfo.radiance.ToString() + "\n";
                    }
                }

                if (ray.IsValid())
                {
                    text += padding + "Decay = " + rayInfo.decay.ToString() + "\n";
                }

                if (rayInfo.isEnd == 1)
                {
                    break;
                }
                else
                {
                    text += "\n";
                }
            }

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(100));

            GUILayout.TextArea(text);

            GUILayout.EndScrollView();
        }
        // End Ray Path

        GUI.DragWindow(new Rect(0, 0, 10000, 10000));
    }
}
