Shader "Custom/ModelGrass" {
    Properties {
        _Albedo1 ("Albedo 1", Color) = (1, 1, 1)
        _Albedo2 ("Albedo 2", Color) = (1, 1, 1)
        _AOColor ("Ambient Occlusion", Color) = (1, 1, 1)
        _TipColor ("Tip Color", Color) = (1, 1, 1)
        verticalScale ("Vertical Scale", Range(0.0, 5.0)) = 1.0
        verticalStretchDueToHeight ("Vertical Stretch Due To Height", Range(0.0, 1.0)) = 0.5
        overallScale ("Overall Scale", Range(0.0, 5.0)) = 1.0
        bottomThickness ("Bottom Thickness", Range(0.0, 5.0)) = 2.0
        _Droop ("Droop", Range(0.0, 10.0)) = 0.0
        _FogColor ("Fog Color", Color) = (1, 1, 1)
        _FogDensity ("Fog Density", Range(0.0, 1.0)) = 0.0
        _FogOffset ("Fog Offset", Range(0.0, 10.0)) = 0.0
    }

    SubShader {
        Cull Off
        Zwrite On

        Tags {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma target 4.5

            #include "UnityPBSLighting.cginc"
            #include "AutoLight.cginc"
            #include "Assets/Resources/Random.cginc"
            #include "Assets/Scripts/Compute/Includes/Noise.compute"

            struct VertexData {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 worldPos : TEXCOORD1;
                float noiseVal : TEXCOORD2;
                float3 chunkNum : TEXCOORD3;
            };

            struct GrassData {
                float4 position;
                float2 uv;
                float displacement;
            };

            sampler2D _WindTex;
            float4 _Albedo1, _Albedo2, _AOColor, _TipColor, _FogColor;
            StructuredBuffer<GrassData> positionBuffer;
            float verticalStretchDueToHeight, _Droop, _FogDensity, _FogOffset;
            float overallScale, verticalScale;
            float bottomThickness;
            
            // int _ChunkNum;

            float4 RotateAroundYInDegrees (float4 vertex, float degrees) {
                float alpha = degrees * UNITY_PI / 180.0;
                float sina, cosa;
                sincos(alpha, sina, cosa);
                float2x2 m = float2x2(cosa, -sina, sina, cosa);
                return float4(mul(m, vertex.xz), vertex.yw).xzyw;
            }
            
            float4 RotateAroundXInDegrees (float4 vertex, float degrees) {
                float alpha = degrees * UNITY_PI / 180.0;
                float sina, cosa;
                sincos(alpha, sina, cosa);
                float2x2 m = float2x2(cosa, -sina, sina, cosa);
                return float4(mul(m, vertex.yz), vertex.xw).zxyw;
            }

            v2f vert (VertexData v, uint instanceID : SV_INSTANCEID) {
                v2f o;
                float4 grassPosition = positionBuffer[instanceID].position;

                // A random val ranged from 0 to 1 only due to its world position
                float idHash = randValue(abs(grassPosition.x * 10000 + grassPosition.y * 100 + grassPosition.z * 0.05f));
                float age = lerp(0.4f, 1.0f, idHash);
                float tenderness = (1.4f - age) * lerp(0.1f, 0.3f, idHash);

                // Transform the grass rotation
                float4 localPosition = RotateAroundXInDegrees(v.vertex, 90.0f);
                // Rotate grass randomly
                localPosition = RotateAroundYInDegrees(localPosition, idHash * 180.0f);

                // Stretch grass vertically
                localPosition.y += verticalStretchDueToHeight * v.uv.y * v.uv.y * v.uv.y;
                localPosition.y *= verticalScale;

                // Droop the grass
                float4 animationDirection = float4(0.0f, 0.0f, 1.0f, 0.0f);
                animationDirection = normalize(RotateAroundYInDegrees(animationDirection, idHash * 180.0f));
                localPosition.xz += _Droop * lerp(0.5f, 1.0f, idHash) * (v.uv.y * v.uv.y * tenderness) 
                * animationDirection;

                // Stretch the grass horizontally
                localPosition.xz *= bottomThickness;

                // Overall scale
                localPosition *= overallScale * age;

                float4 worldUV = float4(positionBuffer[instanceID].uv, 0, 0);
                
                // idHash = 0 -> sway = 0.8
                // idHash = 1 -> sway = 1.0
                // float windFac = v.uv.y * v.uv.y * (tex2Dlod(_WindTex, worldUV).r);
                float windFrequency = 0.1f;
                float windScale = 0.1f;
                float numOctaves = 4;
                float amplitude = 1;
                float persistence = 0.6f;
                float lacunarity = 1.6f;

                float windFac = 0;
                for(int j = 0; j < numOctaves; j++){
                    float n = snoise(float3(
                    (grassPosition.x * windScale + _Time.y), 
                    0.0f, 
                    (grassPosition.z * windScale + _Time.y)) * windFrequency);
                    
                    windFac += n * amplitude;

                    amplitude *= persistence;
                    windFrequency *= lacunarity;
                }

                localPosition.xz += windFac * v.uv.y * tenderness * 5.0f;
                
                float4 worldPosition = float4(grassPosition.xyz + localPosition, 1.0f);

                worldPosition.y -= positionBuffer[instanceID].displacement;
                worldPosition.y += positionBuffer[instanceID].displacement;
                
                o.vertex = UnityObjectToClipPos(worldPosition);
                o.uv = v.uv;
                o.noiseVal = tex2Dlod(_WindTex, worldUV).r;
                o.worldPos = worldPosition;
                // o.chunkNum = float3(randValue(_ChunkNum * 20 + 1024), randValue(randValue(_ChunkNum) * 10 + 2048), randValue(_ChunkNum * 4 + 4096));

                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float4 col = lerp(_Albedo1, _Albedo2, i.uv.y);
                float3 lightDir = _WorldSpaceLightPos0.xyz;
                float ndotl = DotClamped(lightDir, normalize(float3(0, 1, 0)));

                float4 ao = lerp(_AOColor, 1.0f, i.uv.y);
                float4 tip = lerp(0.0f, _TipColor, i.uv.y * i.uv.y * (1.0f + verticalStretchDueToHeight));
                //return fixed4(i.chunkNum, 1.0f);
                //return i.noiseVal;

                float4 grassColor = (col + tip) * ndotl * ao;

                /* Fog */
                float viewDistance = length(_WorldSpaceCameraPos - i.worldPos);
                float fogFactor = (_FogDensity / sqrt(log(2))) * (max(0.0f, viewDistance - _FogOffset));
                fogFactor = exp2(-fogFactor * fogFactor);


                return lerp(_FogColor, grassColor, fogFactor);
            }

            ENDCG
        }
    }
}
