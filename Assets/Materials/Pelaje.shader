Shader "FluxLab/Pelaje"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Sombra ("Sombra", Range(0, 1)) = 0.6
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Sombra;

            struct appdata
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

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 normal = normalize(i.worldNormal);
                float3 luz = normalize(float3(-0.25, 0.85, -0.45));
                float banda = smoothstep(0.02, 0.22, dot(normal, luz));
                float3 color = _Color.rgb * lerp(_Sombra, 1.0, banda);
                return fixed4(color, 1);
            }
            ENDCG
        }
    }
}
