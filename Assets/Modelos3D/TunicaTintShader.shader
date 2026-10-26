Shader "Custom/MagnusPeloYTunica"
{
    Properties
    {
        _MainTex ("Textura Original", 2D) = "white" {}
        _ColorTunica ("Color Tunica", Color) = (1,1,1,1)
        _ColorPelo ("Color Pelo", Color) = (1,1,1,1)
        
        _CorteCuello ("Altura del Cuello", Range(-0.5, 0.6)) = 0.29 
        _Tolerancia ("Tolerancia Ropa/Pelo", Range(0.01, 0.4)) = 0.127
        _Brillo ("Intensidad Tinte", Range(1.0, 3.0)) = 1.68
        _Glossiness ("Smoothness", Range(0,1)) = 0.372
        _Metallic ("Metallic", Range(0,1)) = 0.409
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _ColorTunica;
        fixed4 _ColorPelo;
        float _CorteCuello;
        half _Tolerancia;
        half _Brillo;
        half _Glossiness;
        half _Metallic;

        struct Input
        {
            float2 uv_MainTex;
            float localY; 
        };

        void vert (inout appdata_full v, out Input o) {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.localY = v.vertex.y;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex);

            float maxC = max(c.r, max(c.g, c.b));
            float minC = min(c.r, min(c.g, c.b));
            float delta = maxC - minC;
            float esGris = 1.0 - smoothstep(0.01, _Tolerancia, delta);

            float esPelo = step(_CorteCuello, IN.localY);
            float esTunica = 1.0 - esPelo;

            float tenirPelo = distance(_ColorPelo.rgb, fixed3(1,1,1)) > 0.01 ? 1.0 : 0.0;
            float tenirTunica = distance(_ColorTunica.rgb, fixed3(1,1,1)) > 0.01 ? 1.0 : 0.0;

            fixed3 peloTinteado = lerp(c.rgb, saturate(c.rgb * _ColorPelo.rgb * _Brillo), tenirPelo);
            fixed3 tunicaTinteada = lerp(c.rgb, saturate(c.rgb * _ColorTunica.rgb * _Brillo), tenirTunica);

            fixed3 colorTenido = lerp(tunicaTinteada, peloTinteado, esPelo);

            o.Albedo = lerp(c.rgb, colorTenido, esGris);
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}