Shader "Unlit/TextureAnimPlayerInstanced"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_PosTex("position texture", 2D) = "black"{}
		_NmlTex("normal texture", 2D) = "white"{}
		_DT ("delta time", float) = 0
		_Length ("animation length", Float) = 1
		[Toggle(ANIM_LOOP)] _Loop("loop", Float) = 0
	}
	SubShader
	{
		Pass
		{
			Tags { "RenderType"="Opaque"
					"Queue"="Transparent+1"
					"RenderPipeline" = "HighDefinitionRenderPipeline"}
			LOD 100 Cull Off
			
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile ___ ANIM_LOOP
		    //#include "UnityCG.cginc"

			//#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
			#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    		//#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonLighting.hlsl"

			#define UNITY_PI 3.14159265359f
			struct VertexInput
			{
				float2 uv : TEXCOORD0;
			};

			struct VertexOutput
			{
				float2 uv : TEXCOORD0;
				float3 normal : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			#define ts _PosTex_TexelSize
			StructuredBuffer<float4> _PositionBuffer;
			float4x4 unity_WorldToObject; // In UnityCG.cginc, it is literally just a declared float4x4, nothing else.
			sampler2D _MainTex, _PosTex, _NmlTex;
			float4 _PosTex_TexelSize;
			float _Length, _DT;

			VertexOutput vert (VertexInput v, uint vid : SV_VertexID, uint vinst : SV_InstanceID)
			{
				float4 unitPosition = _PositionBuffer[vinst];
				float t = (_Time.y - _DT) / _Length;
				
				#if ANIM_LOOP
					t = fmod(t, 1.0);
				#else
					t = saturate(t);
				#endif

				float x = (vid + 0.5) * ts.x;
				float y = t;
				float4 pos = tex2Dlod(_PosTex, float4(x, y, 0, 0));

				// Adjust character rotation
				pos = mul(float4x4(
					1, 0, 0, 0,
					0, -cos(-UNITY_PI/2), -sin(-UNITY_PI/2), 0,
					0, sin(-UNITY_PI/2), -cos(-UNITY_PI/2), 0,
					0, 0, 0, 1), pos);

				// Render each unit relative to the unit's position
				pos += unitPosition;

				float3 normal = tex2Dlod(_NmlTex, float4(x, y, 0, 0)).xyz;

				VertexOutput o;
				o.vertex = mul (UNITY_MATRIX_VP, mul (UNITY_MATRIX_M, pos));
				o.normal = normalize(mul(normal, (float3x3)unity_WorldToObject));
				o.uv = v.uv;
				return o;
			}
			
			half4 frag (VertexOutput i) : SV_Target
			{
				half diff = dot(i.normal, float3(0,1,0))*0.5 + 0.5;
				half4 col = tex2D(_MainTex, i.uv);
				return diff * col;
			}
			ENDHLSL
		}
	}
}