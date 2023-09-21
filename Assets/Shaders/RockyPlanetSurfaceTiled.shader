Shader "RockyPlanetSurfaceTiled"
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
		#pragma enable_d3d11_debug_symbols

		#include "UnityCG.cginc" // for UnityObjectToWorldNormal

		struct LodBufferLayoutData {
			uint node_code;
			uint position;
			uint face_id;
			uint edge_smoothing_flags;
		};

		static const uint EDGE_SMOOTHING_LEFT = 1;
		static const uint EDGE_SMOOTHING_RIGHT = 2;
		static const uint EDGE_SMOOTHING_TOP = 4;
		static const uint EDGE_SMOOTHING_BOTTOM = 8;

		#ifdef SHADER_API_D3D11
		// LOD buffer
		StructuredBuffer<LodBufferLayoutData> lod_layout;
		StructuredBuffer<float3> position_buffer;
		StructuredBuffer<float3> normal_buffer;
		StructuredBuffer<float2> uv_buffer;
		#endif

		// Data passed to vertex function
		struct appdata {
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			uint vertexID : SV_VertexID;
			uint instanceID : SV_InstanceID;
		};

		// Data passed to surface shader
		struct Input {
			float4 color : COLOR;
			float3 normal : NORMAL;
		};

		// Properties
		sampler2D _MainTex;
		float4 _Color;
		
		uniform float4x4 _ObjectToWorld;
		uniform uint num_vertices_per_tile;
		uniform uint max_tiles_per_face;
		
		#ifdef SHADER_API_D3D11
		void get_position_and_normal(uint vertex_id, uint vertex_index, uint tile_index, out float3 position, out float3 normal) {
			uint resolution = sqrt(num_vertices_per_tile);
			uint vertex_x = vertex_id % resolution;
			uint vertex_y = vertex_id / resolution;

			bool is_left_smooth = vertex_x == 0 && ((lod_layout[tile_index].edge_smoothing_flags & EDGE_SMOOTHING_LEFT) != 0);
			bool is_right_smooth = vertex_x == resolution - 1 && ((lod_layout[tile_index].edge_smoothing_flags & EDGE_SMOOTHING_RIGHT) != 0);
			bool is_top_smooth = vertex_y == 0 && ((lod_layout[tile_index].edge_smoothing_flags & EDGE_SMOOTHING_TOP) != 0);
			bool is_bottom_smooth = vertex_y == resolution - 1 && ((lod_layout[tile_index].edge_smoothing_flags & EDGE_SMOOTHING_BOTTOM) != 0);

			if(is_left_smooth || is_right_smooth) {
				// left edge vertex
				if(vertex_y % 2 == 1) {
					// Calculate position
					float3 previous = position_buffer[vertex_index - resolution];
					float3 next = position_buffer[vertex_index + resolution];
					position = (previous + next) / 2.0;

					// Calculate normal
					float3 previous_normal = normal_buffer[vertex_index - resolution];
					float3 next_normal = normal_buffer[vertex_index + resolution];
					normal = (previous_normal + next_normal) / 2.0;

					return;
				}
			}else if(is_top_smooth || is_bottom_smooth) {
				if(vertex_x % 2 == 1) {
					// Calculate position
					float3 previous = position_buffer[vertex_index - 1];
					float3 next = position_buffer[vertex_index + 1];
					position = (previous + next) / 2.0;

					// Calculate normal
					float3 previous_normal = normal_buffer[vertex_index - 1];
					float3 next_normal = normal_buffer[vertex_index + 1];
					normal = (previous_normal + next_normal) / 2.0;

					return;
				} 
			}

			position = position_buffer[vertex_index];
			normal = normal_buffer[vertex_index];
		}
		#endif

		// Vertex function
		void vert (inout appdata v, out Input data) {

			UNITY_SETUP_INSTANCE_ID(v);
			UNITY_INITIALIZE_OUTPUT(Input, data);

			#ifdef SHADER_API_D3D11
			uint face_index = lod_layout[v.instanceID].face_id * max_tiles_per_face * num_vertices_per_tile;
			uint tile_index = face_index + lod_layout[v.instanceID].position * num_vertices_per_tile;
			uint vertex_index = tile_index + v.vertexID;

			// Get position and normal
			float3 position;
			float3 normal;
			get_position_and_normal(v.vertexID, vertex_index, v.instanceID, position, normal);

			v.vertex.xyz = mul(_ObjectToWorld, position).xyz;
			v.normal = normalize(mul((float3x3)_ObjectToWorld, normal));
			#endif
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
