Shader "RockyPlanetSurface"
{
	Properties
	{
		_MainTex ("_MainTex (RGBA)", 2D) = "white" {}
		_Color ("_Color", Color) = (1,1,1,1)
	}
	SubShader
	{
		CGPROGRAM
		
		#pragma surface surf Standard fullforwardshadows addshadow vertex:vert
		#pragma target 5.0

		#include "UnityCG.cginc" // for UnityObjectToWorldNormal

		// Vertex information
		// Same one is used with compute shader & C# script
		#ifdef SHADER_API_D3D11	
		StructuredBuffer<float3> position_buffer;
		StructuredBuffer<float3> normal_buffer;
		StructuredBuffer<float2> uv_buffer;
		#endif

		// Data passed to vertex function
		struct appdata {
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			float4 texcoord : TEXCOORD0;
			float4 texcoord1 : TEXCOORD1;
			float4 texcoord2 : TEXCOORD2;
			uint vertexID : SV_VertexID;
		};

		// Data passed to surface shader
		struct Input {
			uint vertexID;
		};

		// Properties
		sampler2D _MainTex;
		float4 _Color;
		
		// Vertex function
		void vert (inout appdata v, out Input data) {

			UNITY_INITIALIZE_OUTPUT(Input, data);

			#ifdef SHADER_API_D3D11	
			v.vertex.xyz = position_buffer[v.vertexID];
			v.normal = normal_buffer[v.vertexID];
			#endif

			data.vertexID = v.vertexID;
		}

		// Surface shader
		void surf (Input IN, inout SurfaceOutputStandard o) {
			#ifdef SHADER_API_D3D11
			//float3 texture_color = tex2D(_MainTex, uv_buffer[IN.vertexID]).rgb;
			o.Albedo = _Color; // no texture color for now
			#endif
		}

		ENDCG
	}
}
