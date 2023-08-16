Shader "RockyPlanetSurface"
{
	Properties
	{
		_MainTex ("_MainTex (RGBA)", 2D) = "white" {}
		_Color ("_Color", Color) = (1,1,1,1)
	}
	SubShader
	{
		Pass
		{
			// indicate that our pass is the "base" pass in forward
            // rendering pipeline. It gets ambient and main directional
            // light data set up; light direction in _WorldSpaceLightPos0
            // and color in _LightColor0
            Tags {"LightMode"="ForwardBase"}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc" // for UnityObjectToWorldNormal
            #include "UnityLightingCommon.cginc" // for _LightColor0

			// Vertex information
			// Same one is used with compute shader & C# script
			StructuredBuffer<float3> position_buffer;
			StructuredBuffer<float3> normal_buffer;
			StructuredBuffer<float2> uv_buffer;

			struct v2f {
				float4 position : SV_POSITION;
				float2 uv : TEXCOORD0;
				fixed4 diffuse : COLOR0;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _Color;
			
			v2f vert (uint id : SV_VertexID) {
				v2f output;
				output.position = UnityObjectToClipPos(float4(position_buffer[id], 1));
				output.uv = TRANSFORM_TEX(uv_buffer[id], _MainTex);

				// dot product between normal and light direction for
                // standard diffuse (Lambert) lighting
				half3 world_normal = UnityObjectToWorldNormal(normal_buffer[id]);
                half nl = max(0, dot(world_normal, _WorldSpaceLightPos0.xyz));

                // factor in the light color
                output.diffuse = nl * _LightColor0;

				// In addition to the diffuse lighting from the main light,
                // add illumination from ambient or light probes
                // ShadeSH9 function from UnityCG.cginc evaluates it,
                // using world space normal
                output.diffuse.rgb += ShadeSH9(half4(world_normal,1));

				return output;
			}
			
			fixed4 frag (v2f i) : SV_Target {
				// sample texture
                fixed4 col = tex2D(_MainTex, i.uv);
                // multiply by lighting
                col *= i.diffuse;
                return col * _Color;
			}
			ENDCG
		}
	}
}
