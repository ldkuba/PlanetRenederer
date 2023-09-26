Shader "RockyPlanetSurfaceTiled"
{
	Properties
	{
		_DiffuseMaps("", 2DArray)   = "" {}
		_NormalMaps("", 2DArray)    = "" {}
		_OcclusionMaps("", 2DArray) = "" {}
		_MacroVariation("", 2D) 	= "white" {}
		_NoiseMap("", 2D)		 	= "white" {}
	}
	SubShader
	{
		CGPROGRAM
		
		// Data passed to vertex function
		struct appdata {
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			float4 tangent : TANGENT;
			float4 texcoord : TEXCOORD0;
			float4 texcoord1 : TEXCOORD1;
			float4 texcoord2 : TEXCOORD2;
			float4 color : COLOR;
			uint vertexID : SV_VertexID;
			uint instanceID : SV_InstanceID;
		};

		// Data passed to surface shader
		#define MAX_BIOME_COUNT 16
		#define BIOMES_USED 11
		struct Input {
			float3 local_coord;
			float3 local_normal;
			float4 biome_factors_1;
			float4 biome_factors_2;
			float4 biome_factors_3;
			float4 biome_factors_4;
		};

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
		#pragma surface surf Standard fullforwardshadows addshadow vertex:vert
		#pragma target 5.0
		#pragma enable_d3d11_debug_symbols

		#pragma shader_feature _NORMALMAP
		#pragma shader_feature _OCCLUSIONMAP

		#include "UnityCG.cginc" // for UnityObjectToWorldNormal
		#include "Includes/Biomes.cginc"

		// LOD buffer
		StructuredBuffer<LodBufferLayoutData> lod_layout;
		StructuredBuffer<float3> position_buffer;
		StructuredBuffer<float3> normal_buffer;
		StructuredBuffer<float2> uv_buffer;
		StructuredBuffer<uint> biome_buffer;

		// Material settings
		StructuredBuffer<float> material_settings;

		// Texture array samplers
		UNITY_DECLARE_TEX2DARRAY(_DiffuseMaps);
		UNITY_DECLARE_TEX2DARRAY(_NormalMaps);
		UNITY_DECLARE_TEX2DARRAY(_OcclusionMaps);

		// Texture sampler
		sampler2D _MacroVariation;
		sampler2D _NoiseMap;

		// Float arrays
		float  _MapScale	 	  [MAX_BIOME_COUNT];
		float  _NormalStrength	  [MAX_BIOME_COUNT];
		float  _OcclusionStrength [MAX_BIOME_COUNT];
		float  _Metallic		  [MAX_BIOME_COUNT];
		float  _Glossiness		  [MAX_BIOME_COUNT];
		float4 _Color			  [MAX_BIOME_COUNT];

		// void   get_surface_material_info(out SurfaceMaterialInfo smi[BIOMES_USED]);
		void set_biome_input(uint biome, inout Input data);
		void parse_biome_factors(Input data, out float biome_factors[MAX_BIOME_COUNT]);
		void apply_triplanar(
		/**/ float3 coords,
		/**/ float3 normal,
		/**/ uint 	biome,
		/**/ float 	biome_factor,
		/**/ inout SurfaceOutputStandard o
		);
		half4 sample_macro_variation(float2 uv);
		
		uniform float4x4 _ObjectToWorld;
		uniform uint num_vertices_per_tile;
		uniform uint max_tiles_per_face;
		
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

		// Vertex function
		void vert (inout appdata v, out Input data) {
			UNITY_SETUP_INSTANCE_ID(v);
			UNITY_INITIALIZE_OUTPUT(Input, data);

			uint face_index = lod_layout[v.instanceID].face_id * max_tiles_per_face * num_vertices_per_tile;
			uint tile_index = face_index + lod_layout[v.instanceID].position * num_vertices_per_tile;
			uint vertex_index = tile_index + v.vertexID;

			// Get position and normal
			float3 position;
			float3 normal;
			get_position_and_normal(v.vertexID, vertex_index, v.instanceID, position, normal);

			v.vertex.xyz = mul(_ObjectToWorld, position).xyz;
			v.normal = normalize(mul((float3x3)_ObjectToWorld, normal));

			data.local_coord = v.vertex.xyz;
			data.local_normal = v.normal;
			
			uint biome = get_biome(biome_buffer, vertex_index);
			set_biome_input(biome, data);
		}

		// Surface shader
		void surf (Input IN, inout SurfaceOutputStandard o) {
			// Reset output
			o.Albedo     = (float4) 0.f;
			o.Alpha      = (float)  0.f;
			o.Normal     = (float3) 0.f;
			o.Occlusion  = (float)  0.f;
			o.Metallic 	 = (float)  0.f;
			o.Smoothness = (float)  0.f;

			// Get biome info
			float biome_factors[MAX_BIOME_COUNT];
			parse_biome_factors(IN, biome_factors);

			// Render biome materials for each biome
			for (uint i = 0; i < MAX_BIOME_COUNT; i++){
				float biome_f = biome_factors[i];
				if (biome_f == 0.f) continue;
				else if (i < BIOMES_USED) {
					apply_triplanar(IN.local_coord, IN.local_normal, i, biome_f, o);
				}
			}
		}

		void set_biome_input(uint biome, inout Input data) {
			switch (biome) {
				case BIOME_FLATLANDS:    data.biome_factors_1.x = 1.0f; break;
				case BIOME_MOUNTAINS:    data.biome_factors_1.y = 1.0f; break;
				case BIOME_U_MOUNTAINS:  data.biome_factors_1.z = 1.0f; break;
				case BIOME_UNDERWATER:   data.biome_factors_1.w = 1.0f; break;
				case BIOME_DEEP_WATERS:  data.biome_factors_2.x = 1.0f; break;
				case BIOME_BEACH:        data.biome_factors_2.y = 1.0f; break;
				case BIOME_CRATER:       data.biome_factors_2.z = 1.0f; break;
				case BIOME_CRATER_RIDGE: data.biome_factors_2.w = 1.0f; break;
				case BIOME_ISLAND:       data.biome_factors_3.x = 1.0f; break;
				case BIOME_PEAKS:        data.biome_factors_3.y = 1.0f; break;
				case BIOME_VALLEY:       data.biome_factors_3.z = 1.0f; break;
			}
		}

		void parse_biome_factors(Input data, out float biome_factors[MAX_BIOME_COUNT]) {
			biome_factors[0]  =  data.biome_factors_1.x;
			biome_factors[1]  =  data.biome_factors_1.y;
			biome_factors[2]  =  data.biome_factors_1.z;
			biome_factors[3]  =  data.biome_factors_1.w;
			biome_factors[4]  =  data.biome_factors_2.x;
			biome_factors[5]  =  data.biome_factors_2.y;
			biome_factors[6]  =  data.biome_factors_2.z;
			biome_factors[7]  =  data.biome_factors_2.w;
			biome_factors[8]  =  data.biome_factors_3.x;
			biome_factors[9]  =  data.biome_factors_3.y;
			biome_factors[10] =  data.biome_factors_3.z;
			biome_factors[11] =  data.biome_factors_3.w;
			biome_factors[12] =  data.biome_factors_4.x;
			biome_factors[13] =  data.biome_factors_4.y;
			biome_factors[14] =  data.biome_factors_4.z;
			biome_factors[15] =  data.biome_factors_4.w;
		}
		
		SamplerState g_samPoint
		{
			Filter = MIN_MAG_MIP_POINT;
			AddressU = Wrap;
			AddressV = Wrap;
		};

		void apply_triplanar(
		/**/ float3 coords,
		/**/ float3 normal,
		/**/ uint 	biome,
		/**/ float 	biome_factor,
		/**/ inout SurfaceOutputStandard o
		) {
			// Blending factor of triplanar mapping
			float3 bf = normalize(abs(normal));
			bf /= dot(bf, (float3)1);
			
			// Triplanar mapping
			float2 tx = coords.yz * _MapScale[biome];
			float2 ty = coords.zx * _MapScale[biome];
			float2 tz = coords.xy * _MapScale[biome];

			// Sample macro variations
			half4 mvx = sample_macro_variation(tx / 10.f);
			half4 mvy = sample_macro_variation(ty / 10.f);
			half4 mvz = sample_macro_variation(tz / 10.f);

			// Sample diffuse map
			half4 sample_x = UNITY_SAMPLE_TEX2DARRAY(_DiffuseMaps, float3(tx, biome));
			half4 sample_y = UNITY_SAMPLE_TEX2DARRAY(_DiffuseMaps, float3(ty, biome));
			half4 sample_z = UNITY_SAMPLE_TEX2DARRAY(_DiffuseMaps, float3(tz, biome));

			// Base color
			half4 cx = sample_x * mvx * bf.x;
			half4 cy = sample_y * mvy * bf.y;
			half4 cz = sample_z * mvz * bf.z;
			half4 base_color = (cx + cy + cz) * _Color[biome];
			o.Albedo += base_color.rgb * biome_factor;
			o.Alpha += base_color.a * biome_factor;

			// Normal map
			// Swizzle world normals into tangent space and apply Whiteout blend
			half3 t_normal_x = UnpackScaleNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalMaps, float3(tx, biome)), _NormalStrength[biome]);
			half3 t_normal_y = UnpackScaleNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalMaps, float3(ty, biome)), _NormalStrength[biome]);
			half3 t_normal_z = UnpackScaleNormal(UNITY_SAMPLE_TEX2DARRAY(_NormalMaps, float3(tz, biome)), _NormalStrength[biome]);
			t_normal_x = half3(t_normal_x.xy + normal.zy, abs(t_normal_x.z) * normal.x);
			t_normal_y = half3(t_normal_y.xy + normal.xz, abs(t_normal_y.z) * normal.y);
			t_normal_z = half3(t_normal_z.xy + normal.xy, abs(t_normal_z.z) * normal.z);
			
			o.Normal += normalize(
			/**/ t_normal_x.zyx * bf.x +
			/**/ t_normal_y.xzy * bf.y +
			/**/ t_normal_z.xyz * bf.z
			) * biome_factor;

			// Occlusion map
			half ox = UNITY_SAMPLE_TEX2DARRAY(_OcclusionMaps, float3(tx, biome)).g * bf.x;
			half oy = UNITY_SAMPLE_TEX2DARRAY(_OcclusionMaps, float3(ty, biome)).g * bf.y;
			half oz = UNITY_SAMPLE_TEX2DARRAY(_OcclusionMaps, float3(tz, biome)).g * bf.z;
			o.Occlusion += lerp((half4)1, ox + oy + oz, _OcclusionStrength[biome]) * biome_factor;

			// Misc parameters
			o.Metallic   += _Metallic[biome] * biome_factor;
			o.Smoothness += _Glossiness[biome] * biome_factor;
		}

		half4 sample_macro_variation(float2 uv) {
			// Scale UV's
			float2 uv_scale_1 = uv * 0.2134f;
			float2 uv_scale_2 = uv * 0.05341f;
			float2 uv_scale_3 = uv * 0.002f;

			// Sample macro variation texture
			half sample_scale_1 = tex2D(_MacroVariation, uv_scale_1).r;
			half sample_scale_2 = tex2D(_MacroVariation, uv_scale_2).r;
			half sample_scale_3 = tex2D(_MacroVariation, uv_scale_3).r;
			half sample = sample_scale_1 * sample_scale_2 * sample_scale_3;

			// Reduce contrast
			half3 sample_reduced = lerp((half3) 0.5f, (half3)1.f, sample);
			return half4(sample_reduced, 1.f);
		}

#else
		void vert (inout appdata v, out Input data) {}
		void surf (Input IN, inout SurfaceOutputStandard o) { o.Albedo = float4(1,0,0.5,1); }
#endif
		ENDCG
	}
}
