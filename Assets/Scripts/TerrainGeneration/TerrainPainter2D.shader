Shader "Custom/TerrainPainter2D"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
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

        float f1;
        float f2;
        float f3;

        float minMaxBounds;
        float offsetY;

        float normalOffsetWeight;
        float musicNoise;
        float musicNoiseWeight;
        float mapBound;
        float worldPosOffset;

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        
        sampler2D originalGrayscaleTex;
        sampler2D originalPalette;
        sampler2D userTex;
        sampler2D metallicTex;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // User texture
            float4 userCol = tex2D(
            userTex, 
            float2((IN.worldPos.x + worldPosOffset) * mapBound, (IN.worldPos.z + worldPosOffset) * mapBound));

            // Load original texture from color palette and noise
            // float texId = tex2D(originalGrayscaleTex, float2(IN.worldPos.x * mapBound, IN.worldPos.z * mapBound)).r;

            float h = smoothstep(  
            -minMaxBounds, 
            minMaxBounds, 
            -IN.worldPos.y + 
            offsetY + 
            (abs(IN.worldNormal.x) + abs(IN.worldNormal.y) + abs(IN.worldNormal.z)) * normalOffsetWeight);

            float3 originalCol = tex2D(originalPalette, float2(h,.5));
            
            // Blend func
            // float blendFactor = step(2.8f, dot(userCol.rgb, userCol.rgb));
            float blendFactor = userCol.a;
            o.Albedo = lerp(originalCol, userCol, blendFactor);

            float metallicFac = step(0.8f, userCol.r);
            o.Metallic = lerp(0, _Metallic, metallicFac);
            o.Smoothness = lerp(0, _Glossiness, metallicFac);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
