Shader "Custom/Wave"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Steepness ("Steepness", Range(0,1)) = 0.5
        _WaveLength ("Wavelength", Float) = 10
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows vertex:vert addshadow

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        sampler2D _MainTex;

        struct Input
        {
            float2 uv_MainTex;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float  _Steepness, _WaveLength;

        void vert(inout appdata_full vertexData){
            float3 p = vertexData.vertex.xyz;
            float k = 2 * UNITY_PI / _WaveLength;
            float c = sqrt(9.8 / k);                    // Wave speed
            float f = k * (p.x - c * _Time.y);
            
            // Amplitude
            float a = _Steepness / k;

            
            p.x = p.x + a * cos(f);
            p.y = a * sin(f);

            float3 tangent = normalize(float3(1 - k * a * sin(f), k * a * cos(f), 0));
            float3 normal = float3(-tangent.y, tangent.x, 0);
            vertexData.vertex.xyz = p;
            vertexData.normal = normal;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
