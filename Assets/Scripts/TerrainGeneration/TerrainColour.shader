Shader "Custom/TerrainColour"
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

        float4 metalColor;
        float4 grassColor;
        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        
        sampler2D originalGrayscaleTex;
        sampler2D originalPalette;
        sampler3D universalRenderTex;
        sampler2D metallicTex;

        // return: 0 - false, 1 - true
        int colorCmp(float4 col1, float4 col2){
            float threhold = 0.01f;
            return 1 - step(threhold, distance(col1.rgb, col2.rgb));
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // User texture
            float4 userCol = tex3D(
            universalRenderTex, 
            float3( 
            (IN.worldPos.x + worldPosOffset) * mapBound, 
            (IN.worldPos.y + worldPosOffset) * mapBound, 
            (IN.worldPos.z + worldPosOffset) * mapBound));

            float h = smoothstep(  
            -minMaxBounds, 
            minMaxBounds, 
            -IN.worldPos.y + 
            offsetY + 
            (abs(IN.worldNormal.x) + abs(IN.worldNormal.y) + abs(IN.worldNormal.z)) * normalOffsetWeight);

            float3 originalCol = tex2D(originalPalette, float2(h,.5));
            
            // Blend func
            float metallicFac = colorCmp(metalColor, userCol);
            o.Albedo = lerp(originalCol, userCol, metallicFac);

            o.Metallic = lerp(0, _Metallic, metallicFac);
            o.Smoothness = lerp(0, _Glossiness, metallicFac);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
