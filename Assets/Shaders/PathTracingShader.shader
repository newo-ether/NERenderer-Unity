// PathTracingShader.shader

Shader "Custom/Path Tracing Shader"
{
    Properties
    {
        [Enum(Lambertian,0,Specular,1,Microfacet,2)] _BSDFType("BSDF Type", Int) = 0
        _Albedo("Albedo", Color) = (1,1,1,1)
        _Metallic("Metallic", Range(0,1)) = 0
        _Roughness("Roughness", Range(0,1)) = 1
        _IOR("IOR", Float) = 1
        _Transmission("Transmission", Range(0,1)) = 0
        [HDR] _Emission("Emission", Color) = (0,0,0,1)
    }
    SubShader
    {
        Tags { "LightMode" = "ForwardBase" "RenderType" = "Opaque" }

        Pass {

            Cull Off

            CGPROGRAM

            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            #pragma vertex vert
            #pragma fragment frag

            uniform float4 _Albedo;
            uniform float4 _Emission;

            struct a2v
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert(a2v v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = mul(v.normal, (float3x3)unity_WorldToObject);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_TARGET
            {
                if (_Emission.r != 0.0f || _Emission.g != 0.0f || _Emission.b != 0.0f)
                {
                    return fixed4(_Emission.rgb, 1.0f);
                }
                fixed3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos.xyz);
                fixed3 worldNormal = normalize(i.worldNormal);
                worldNormal = dot(viewDir, worldNormal) < 0.0f ? -worldNormal : worldNormal;
                fixed3 diffuse = _Albedo.rgb * dot(viewDir, worldNormal) * 0.5f;
                fixed3 specular = _Albedo.rgb * pow(dot(viewDir, worldNormal), 50.0f);
                return fixed4(diffuse + specular, 1.0f);
            }

            ENDCG
        }
    }
}
