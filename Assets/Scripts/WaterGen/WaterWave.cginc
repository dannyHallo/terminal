#include "UnityCG.cginc"
#include "Lighting.cginc"

sampler2D _MainTex;
sampler2D _BumpMap;

float _WaveHeight1, _WaveHeight2, _WaveHeight3;
float _WaveSteepness1, _WaveSteepness2, _WaveSteepness3;
float4 _WaveParam1, _WaveParam2, _WaveParam3;
float _fresnelBase;
float _fresnelScale;
float _fresnelIntensity;
float4 _fresnelCol;

sampler2D _FesnelTex;
sampler2D _DumpTex;
sampler2D _ReflectTex;
sampler2D _RefractTex;

float4 _Color;
float4 _WaveScale;
float4 _WaveOffset;
float _RefrectFac;

struct waveAppData {
	float4 vertex : POSITION;
	float2 uv:TEXCOORD0;
	float4 tangent : TANGENT;
	float3 normal : NORMAL;
};

struct waveV2f {
	float4 pos : SV_POSITION;
	float2 uv:TEXCOORD0;
	float3 normal:NORMAL;
	float3 lightDir : TEXCOORD1;
	float3 viewDir:TEXCOORD5;
	
	float4 tangent : TANGENT;
	float4 ref:TEXCOORD2;
	float2 bumpUv1 : TEXCOORD3;
	float2 bumpUv2 : TEXCOORD4;
};

float3 GerstnerWave(float2 pos,float A,float sharp, float4 param, inout float3 normal) {
	float3 WaveDir = normalize(float3(param.xy, 0));
	float waveT = param.z;
	float3 vertex = 0;
	float w = 2 * UNITY_PI / waveT;
	half speed = param.w;

	float f = _Time.y * speed + w * dot(WaveDir, pos);

	vertex.x = (sharp / w) * WaveDir.x * cos(_Time.y * speed + w * dot(WaveDir, pos));
	vertex.z = (sharp / w) * WaveDir.y * cos(_Time.y * speed + w * dot(WaveDir, pos));
	vertex.y = A * sin(_Time.y * speed + w * dot(WaveDir, pos));

	float3 P = float3(vertex.x, vertex.z, vertex.y);
	normal.xz += WaveDir * w * A * cos(speed * _Time.y + w * dot(WaveDir, P));
	normal.y += sharp * sin(speed * _Time.y + w * dot(WaveDir, P)) - 1;
	return vertex;
}

waveV2f waveVert(waveAppData i) {
	waveV2f o;
	o.uv = i.uv;
	float3 normal = 0;
	float3 calVertex = 0;
	float3 _normal = 0;
	float3 displayVertex = i.vertex.xyz;

	calVertex += GerstnerWave(i.vertex.xz, _WaveHeight1, _WaveSteepness1, _WaveParam1, normal);
	// _normal += normal;
	
	// calVertex += GerstnerWave(i.vertex.xz, _WaveHeight2, _WaveSteepness2, _WaveParam2, normal);
	// _normal += normal;

	// calVertex += GerstnerWave(i.vertex.xz, _WaveHeight3, _WaveSteepness3, _WaveParam3, normal);
	// _normal += normal;

	displayVertex += calVertex;

	o.pos = UnityObjectToClipPos(displayVertex);

	// o.lightDir = ObjSpaceLightDir(i.vertex);
	
	//获取世界坐标的光向量
	o.lightDir = WorldSpaceLightDir(float4(displayVertex.xyz, 1));
	//获取世界坐标的视角向量
	o.viewDir = WorldSpaceViewDir(float4(displayVertex.xyz, 1));

	// o.normal.x = -_normal.x;
	// o.normal.y = 1 - _normal.y;
	// o.normal.z = -_normal.z;

	//将法线转到世界坐标
	o.normal = mul(normal, (float3x3)unity_WorldToObject);

	float4 wpos = mul(unity_ObjectToWorld, i.vertex);
	o.bumpUv1 = wpos.xz;
	o.bumpUv2 = wpos.zx;
	o.ref = ComputeScreenPos(o.pos);
	// o.viewDir.xzy = WorldSpaceViewDir(i.vertex);
	return o;
}

half4 waveFrag(waveV2f i) : SV_Target
{
	float3 N = normalize(i.normal);
	float3 L = normalize(i.lightDir);
	float3 V = normalize(i.viewDir);
	
	// // Get normal of this point from the normal map
	// float3 bump1 = UnpackNormal(tex2D(_DumpTex,i.bumpUv1)).rgb;
	// // Reverse version
	// float3 bump2 = UnpackNormal(tex2D(_DumpTex, i.bumpUv2)).rgb;
	// float3 bump = (bump1 + bump2)*0.5 + i.normal;

	// float3 viewDir = i.viewDir;
	// half fresnelFac = dot(viewDir,bump);
	// //菲涅尔贴图，Y值越大值越大。X值没变化。
	// half fesnel = UNITY_SAMPLE_1CHANNEL(_FesnelTex, float2(fresnelFac, fresnelFac));

	// float4 uv1 = i.ref;
	// uv1.xy += bump * _RefrectFac;
	// float4 relCol = tex2Dproj(_ReflectTex, UNITY_PROJ_COORD(uv1));

	// float4 uv2 = i.ref;
	// uv2.xy -= bump * _RefractFac;
	// float4 refraCol = tex2Dproj(_RefractTex, UNITY_PROJ_COORD(uv2))*_Color;

	// half4 fincol = 0;
	// fincol = lerp(refraCol, relCol, fesnel);

	float4 col = _Color;
	col.rgb *= saturate(dot(N, L)) * _LightColor0.rgb;
	//菲尼尔公式
	float fresnel = _fresnelBase + _fresnelScale*pow(1 - dot(N, V), _fresnelIntensity);

	col.rgb += lerp(col.rgb, _fresnelCol, fresnel) * _fresnelCol.a;
	
	// return fincol;
	return col;
}