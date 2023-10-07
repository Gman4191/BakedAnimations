Shader "Unlit/PlayShaderInstanced"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_BaseColor("Base Color", color) = (1,1,1,1)
		_Smoothness("Smoothness", Range(0,1)) = 0
        _Metallic("Metallic", Range(0,1)) = 0
		_PosTex("position texture", 2D) = "black"{}
		_NmlTex("normal texture", 2D) = "white"{}
		[Toggle(ANIM_LOOP)] _Loop("loop", Float) = 0
	}
	SubShader
	{
		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl" 

		struct unitInfo
		{
			float3 pos;
			float3 rot;
			float3 scale;
			float currentAnimation;
			float length;
			float animScale;
			float time;
			uint isLooping;
		};
 
		CBUFFER_START(UnityPerMaterial)
			#define UNITY_PI 3.14159265359f
			#define ts _PosTex_TexelSize
			
			StructuredBuffer<unitInfo> _UnitInfoBuffer;
			sampler2D _MainTex, _PosTex, _NmlTex;
			float4 _MainTex_ST;
			float4 _BaseColor;
			float _Smoothness, _Metallic;
			float4 _PosTex_TexelSize;
			float _Length;
		CBUFFER_END
		ENDHLSL

		Pass
		{		
			Tags {"Queue"="Transparent" "LightMode"="ShadowCaster"}

			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile ___ ANIM_LOOP

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
				// Get unit information from the buffer
				float3 unitPosition = _UnitInfoBuffer[vinst].pos;
				float3 unitRotationDegrees = _UnitInfoBuffer[vinst].rot;
				float unitScale = _UnitInfoBuffer[vinst].scale.x;

				float3 radians = unitRotationDegrees * (UNITY_PI / 180.0f);
				float animLength = _UnitInfoBuffer[vinst].length;
				float yOffset = _UnitInfoBuffer[vinst].currentAnimation;
				float animScale = _UnitInfoBuffer[vinst].animScale;

				// Adjust the time of the current unit's animation
				float t = _Time.y / animLength * animScale;
				
				//#if ANIM_LOOP
					t = fmod(t, animScale);
				//#else
				//	t = saturate(t);
				//#endif

				float x = ((float)vid + 0.5) * ts.x;
				float y = t + yOffset + (0.5 * ts.y);
				float4 pos = tex2Dlod(_PosTex, float4(x, y, 0, 0));
				pos.xyz *= unitScale;

				// Adjust character rotation
				pos = mul(float4x4(
					1, 0, 0, 0,
					0, -cos(-UNITY_PI/2), -sin(-UNITY_PI/2), 0,
					0, sin(-UNITY_PI/2), -cos(-UNITY_PI/2), 0,
					0, 0, 0, 1), pos);

				pos = mul(float4x4(
					cos(radians.y) * cos(radians.z), sin(radians.x) * sin(radians.y) * cos(radians.z) - cos(radians.x) * sin(radians.z), cos(radians.x) * sin(radians.y) * cos(radians.z) + sin(radians.x) * sin(radians.z), 0,
					cos(radians.y) * sin(radians.z), sin(radians.x) * sin(radians.y) * sin(radians.z) + cos(radians.x) * cos(radians.z), cos(radians.x) * sin(radians.y) * sin(radians.z) - sin(radians.x) * cos(radians.z), 0,
					-sin(radians.y), sin(radians.x) * cos(radians.y), cos(radians.x) * cos(radians.y), 0,
					0, 0, 0, 1), pos);	

				// Render each unit relative to the unit's position
				pos.xyz += unitPosition;

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
			LOD 100
			
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
				float3 unitPosition = _UnitInfoBuffer[vinst].pos;
				float3 unitRotationDegrees = _UnitInfoBuffer[vinst].rot;
				float unitScale = _UnitInfoBuffer[vinst].scale.x;
				float3 radians = unitRotationDegrees * (UNITY_PI / 180.0f);

				float4x4 worldMatrix = float4x4(
					cos(radians.y) * cos(radians.z), sin(radians.x) * sin(radians.y) * cos(radians.z) - cos(radians.x) * sin(radians.z), cos(radians.x) * sin(radians.y) * cos(radians.z) + sin(radians.x) * sin(radians.z), 0,
					cos(radians.y) * sin(radians.z), sin(radians.x) * sin(radians.y) * sin(radians.z) + cos(radians.x) * cos(radians.z), cos(radians.x) * sin(radians.y) * sin(radians.z) - sin(radians.x) * cos(radians.z), 0,
					-sin(radians.y), sin(radians.x) * cos(radians.y), cos(radians.x) * cos(radians.y), 0,
					0, 0, 0, 1);
				float4x4 xAxisRotationMatrix = float4x4(
					1, 0, 0, 0,
					0, -cos(-UNITY_PI/2), -sin(-UNITY_PI/2), 0,
					0, sin(-UNITY_PI/2), -cos(-UNITY_PI/2), 0,
					0, 0, 0, 1);

				float animLength = _UnitInfoBuffer[vinst].length;
				float yOffset = _UnitInfoBuffer[vinst].currentAnimation;
				float animScale = _UnitInfoBuffer[vinst].animScale;
				
				float t = _Time.y / animLength * animScale;

				#if ANIM_LOOP
					t = fmod(t, animScale);
				#else
					t = saturate(t);
				#endif

				float x = (vid + 0.5) * ts.x;
				float y = t + yOffset + (0.5 * ts.y);
				float4 pos = tex2Dlod(_PosTex, float4(x, y, 0, 0));
				pos.xyz *= unitScale;

				// Adjust character rotation
				pos = mul(xAxisRotationMatrix, pos);

				pos = mul(worldMatrix, pos);	

				// Render each unit relative to the unit's position
				pos.xyz += unitPosition;

				float3 normal = tex2Dlod(_NmlTex, float4(x, y, 0, 0)).xyz;
				normal = mul((float3x3)xAxisRotationMatrix, normal);
				normal = mul((float3x3)worldMatrix, normal);

				v2f o;
                o.positionWS = TransformObjectToWorld(pos.xyz);
                o.normalWS = TransformObjectToWorldNormal(normal);
                o.viewDir = normalize(_WorldSpaceCameraPos - o.positionWS);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.vertex = TransformWorldToHClip(o.positionWS);
				OUTPUT_LIGHTMAP_UV( v.texcoord1, unity_LightmapST, o.lightmapUV );
    			OUTPUT_SH(o.normalWS.xyz, o.vertexSH );
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
				inputdata.shadowCoord = TransformWorldToShadowCoord(i.positionWS);

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