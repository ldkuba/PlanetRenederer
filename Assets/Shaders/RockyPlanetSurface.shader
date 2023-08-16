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
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			// Vertex information
			// Same with the one with compute shader & C# script
			StructuredBuffer<float3> position_buffer;
			StructuredBuffer<float3> normal_buffer;
			StructuredBuffer<float2> uv_buffer;

			struct v2f {
				float4 position : SV_POSITION;
				float2 uv : TEXCOORD0;
				fixed4 color : COLOR;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _Color;
			
			v2f vert (uint id : SV_VertexID) {
				v2f output;
				output.position = UnityObjectToClipPos(float4(position_buffer[id], 1));
				output.uv = TRANSFORM_TEX(uv_buffer[id], _MainTex);
				output.color = _Color;
				return output;
			}
			
			fixed4 frag (v2f i) : SV_Target {
				fixed4 col = tex2D(_MainTex, i.uv);
				col.rgb *= i.color.rgb;
				return col*_Color;
			}
			ENDCG
		}
	}
}
