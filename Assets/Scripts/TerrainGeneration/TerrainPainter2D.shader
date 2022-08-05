Shader "Custom/TerrainPainter2D"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _BoundTest("Bound", Range(0,0.01)) = 0.01
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float3 worldNormal;
        };

        float bound;
        float normalOffsetWeight;
        float musicNoise;
        float musicNoiseWeight;

        float _BoundTest;
        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        sampler2D ramp;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 tex = tex2D(_MainTex, float2(IN.worldPos.x * _BoundTest, IN.worldPos.z * _BoundTest));

            o.Albedo = tex;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
