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
        Tags { "RenderType" = "Opaque" }

        Pass {

            CGPROGRAM

            #include "UnityCG.cginc"

            #pragma vertex vert
            #pragma fragment frag

            uniform float4 _Albedo;

            struct a2v
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed3 color : COLOR0;
            };

            v2f vert(a2v v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_TARGET
            {
                return fixed4(_Albedo.rgb, 1.0);
            }

            ENDCG
        }
    }
}
