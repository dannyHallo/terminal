Shader "Custom/Fresnel"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_fresnelBase("fresnelBase", Range(0, 1)) = 1
		_fresnelScale("fresnelScale", Range(0, 1)) = 1
		_fresnelIndensity("fresnelIndensity", Range(0, 5)) = 5
		_fresnelCol("_fresnelCol", Color) = (1,1,1,1)
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			tags{"lightmode="="forward"}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "Lighting.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float3 L : TEXCOORD1;
				float3 N : TEXCOORD2;
				float3 V : TEXCOORD3;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			float _fresnelBase;

			float _fresnelScale;

			float _fresnelIndensity;

			float4 _fresnelCol;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				//将法线转到世界坐标
				o.N = mul(v.normal, (float3x3)unity_WorldToObject);
				//获取世界坐标的光向量
				o.L = WorldSpaceLightDir(v.vertex);
				//获取世界坐标的视角向量
				o.V = WorldSpaceViewDir(v.vertex);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);

				float3 N = normalize(i.N);
				float3 L = normalize(i.L);
				float3 V = normalize(i.V);

				col.rgb *= saturate(dot(N, L)) * _LightColor0.rgb;
				//菲尼尔公式
				float fresnel = _fresnelBase + _fresnelScale*pow(1 - dot(N, V), _fresnelIndensity);

				col.rgb += lerp(col.rgb, _fresnelCol, fresnel) * _fresnelCol.a;

				return col;
			}

			ENDCG
		}
	}
}

