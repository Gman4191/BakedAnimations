Shader "Unlit/TextureAnimPlayerInstanced"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_BaseColor("Base Color", color) = (1,1,1,1)
		_Smoothness("Smoothness", Range(0,1)) = 0
        _Metallic("Metallic", Range(0,1)) = 0
		_PosTex("position texture", 2D) = "black"{}
		_NmlTex("normal texture", 2D) = "white"{}
		_DT ("delta time", float) = 0
		_Length ("animation length", Float) = 1
		[Toggle(ANIM_LOOP)] _Loop("loop", Float) = 0
	}
	SubShader
	{
		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		//#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"  
		CBUFFER_START(UnityPerMaterial)
			#define UNITY_PI 3.14159265359f
			#define ts _PosTex_TexelSize
			StructuredBuffer<float4> _PositionBuffer;
			sampler2D _MainTex, _PosTex, _NmlTex;
			float4 _MainTex_ST;
			float4 _BaseColor;
			float _Smoothness, _Metallic;
			float4 _PosTex_TexelSize;
			float _Length, _DT;
		CBUFFER_END
		ENDHLSL

		Pass
		{		
			Tags {"Queue"="Transparent+1" "LightMode"="ShadowCaster"}

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			struct appdata
			{
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float3 normal : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};
			
			v2f vert (appdata v, uint vid : SV_VertexID, uint vinst : SV_InstanceID)
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

				v2f o;
                o.normal = TransformObjectToWorldNormal(normal);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.vertex = TransformWorldToHClip(TransformObjectToWorld(pos.xyz));
				return o;
			}

			half4 frag (v2f i) : SV_Target
			{
				half4 col = tex2D(_MainTex, i.uv);
				return col;
			}
			ENDHLSL
		}
		Pass
		{
			Tags { "RenderType"="Opaque"
					"Queue"="Transparent+1"
					"RenderPipeline" = "UniversalRenderPipeline"
					"LightMode"="UniversalForward"}
			LOD 100 Cull Off
			
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile ___ ANIM_LOOP
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
			#pragma multi_compile _ _SHADOWS_SOFT    

			struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 normal : NORMAL;
                float4 texcoord1 : TEXCOORD1;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 4);
            };

			v2f vert (appdata v, uint vid : SV_VertexID, uint vinst : SV_InstanceID)
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

				v2f o;
                o.positionWS = TransformObjectToWorld(pos.xyz);
                o.normalWS = TransformObjectToWorldNormal(normal);
                o.viewDir = normalize(_WorldSpaceCameraPos - o.positionWS);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.vertex = TransformWorldToHClip(o.positionWS);
				return o;
			}
			
            half4 frag (v2f i) : SV_Target
            {
                half4 col = tex2D(_MainTex, i.uv);
                InputData inputdata = (InputData)0;
                inputdata.positionWS = i.positionWS;
                inputdata.normalWS = normalize(i.normalWS);
                inputdata.viewDirectionWS = i.viewDir;
                inputdata.bakedGI = SAMPLE_GI( i.lightmapUV, i.vertexSH, inputdata.normalWS );


                SurfaceData surfacedata;
                surfacedata.albedo = _BaseColor.xyz;
                surfacedata.specular = 0;
                surfacedata.metallic = _Metallic;
                surfacedata.smoothness = _Smoothness;
                surfacedata.normalTS = 0;
                surfacedata.emission = 0;
                surfacedata.occlusion = 1;
                surfacedata.alpha = 0;
                surfacedata.clearCoatMask = 0;
                surfacedata.clearCoatSmoothness = 0;

                return UniversalFragmentPBR(inputdata, surfacedata);
            }
			ENDHLSL
		}
	}
}