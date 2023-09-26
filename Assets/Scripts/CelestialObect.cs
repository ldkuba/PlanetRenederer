using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

[ExecuteInEditMode()]
public abstract class CelestialObject : MonoBehaviour
{

    // Components
    public ShapeSettings shapeSettings;
    public SurfaceMaterialSettings surfaceMaterialSettings;

    [SerializeField, HideInInspector]
    protected MeshFilter mesh_filter;

    // Used for instanced rendering
    protected Mesh tile_mesh;

    // Settings
    [HideInInspector]
    public SphereMeshGenerator.SphereType SphereType = SphereMeshGenerator.SphereType.Cube;
    [HideInInspector]
    public int resolution = 100;
    public Material material;

    // Vertex buffers
    private ComputeBuffer initial_pos_buffer;
    private ComputeBuffer position_buffer;
    private ComputeBuffer normal_buffer;
    private ComputeBuffer biome_buffer;
    private ComputeBuffer uv_buffer;

    private bool apply_noise_flag = false;
    private bool update_view_based_culling_flag = false;

    // Camera shape control
    Camera main_camera;
    private MainCameraShapeController camera_shape_controller;

    private void OnEnable()
    {
        if (mesh_filter == null) return;
        generate_mesh();

        // // Noise applied automatically when lod kernel runs first time
        // if(SphereType != SphereMeshGenerator.SphereType.Tile)
            apply_noise_flag = true;
        if (main_camera != null)
            camera_shape_controller.transform_changed += OnCameraTransformChanged;
    }
    private void OnDisable()
    {
        release_buffers();
        if (main_camera != null)
            camera_shape_controller.transform_changed -= OnCameraTransformChanged;
    }

    private void Update() {
        if (!Application.IsPlaying(gameObject)) rebind_buffers();

        if (transform.hasChanged) {
            OnTransformChanged();
        }

        // Render tile instances
        if(SphereType == SphereMeshGenerator.SphereType.Tile) {
            if (shapeSettings == null)
                throw new UnityException("Error in :: CelestialObject :: apply_noise :: Shape settings not set!");

            // Run lod kernel
            // this will automatically apply noise to new tiles
            if(shapeSettings.run_lod_kernels(main_camera)) {
                apply_noise_flag = true;
            }
        }

        if(update_view_based_culling_flag) {
            update_view_based_culling();
        } else if(apply_noise_flag) {
            apply_noise();
        }

        update_view_based_culling_flag = false;
        apply_noise_flag = false;

        if(SphereType == SphereMeshGenerator.SphereType.Tile) {
            // Render instanced
            RenderParams renderParams = new RenderParams(material);
            renderParams.worldBounds = new Bounds(
                Vector3.zero,
                new Vector3(shapeSettings.radius, shapeSettings.radius, shapeSettings.radius) * 2.0f
            );
            renderParams.matProps = new MaterialPropertyBlock();
            renderParams.matProps.SetMatrix("_ObjectToWorld", transform.localToWorldMatrix);
            renderParams.matProps.SetBuffer("lod_layout", shapeSettings.get_lod_manager().get_lod_layout_buffer());
            renderParams.matProps.SetInt("num_vertices_per_tile", tile_mesh.vertexCount);
            renderParams.matProps.SetInt("max_tiles_per_face", LodQuadTree.MAX_NUM_NODES);

            Graphics.RenderMeshPrimitives(renderParams, tile_mesh, 0, shapeSettings.get_lod_manager().get_node_count());
        }
    }

    // Public Methods
    public void setup_camera_shape_control(Camera main_camera, MainCameraShapeController camera_shape_controller) {
        this.main_camera = main_camera;
        this.camera_shape_controller = camera_shape_controller;
        camera_shape_controller.transform_changed += OnCameraTransformChanged;
    }

    [ContextMenu("initialize")]
    public void Initialize() { 
        initialize_mesh_filter();
        generate_mesh();

        // // noise applied automatically when lod kernel runs first time
        // if(SphereType != SphereMeshGenerator.SphereType.Tile)
            apply_noise_flag = true;
    }
    public void OnResolutionChanged() { 
        generate_mesh();
        
        // // noise applied automatically when lod kernel runs first time
        // if(SphereType != SphereMeshGenerator.SphereType.Tile)
            apply_noise_flag = true;
    }
    public void OnShapeSettingsUpdated() { apply_noise_flag = true; }
    public void OnCameraTransformChanged() { update_view_based_culling_flag = true; }
    public void OnTransformChanged() { update_view_based_culling_flag = true; }
    public void OnSurfaceMaterialInfoChanged() { set_surface_material_info(); }

    // Private methods
    private void release_buffers()
    {
        initial_pos_buffer?.Release();
        position_buffer?.Release();
        normal_buffer?.Release();
        biome_buffer?.Release();
        uv_buffer?.Release();
    }

    private void rebind_buffers()
    {
        material.SetBuffer("position_buffer", position_buffer);
        material.SetBuffer("normal_buffer", normal_buffer);
        material.SetBuffer("biome_buffer", biome_buffer);
        material.SetBuffer("uv_buffer", uv_buffer);
    }

    private void set_surface_material_info()
    {
        if (surfaceMaterialSettings == null) return;
        // Set textures
        material.SetTexture("_DiffuseMaps", surfaceMaterialSettings.get_diffuse_map());
        material.SetTexture("_NormalMaps", surfaceMaterialSettings.get_normal_map());
        material.SetTexture("_OcclusionMaps", surfaceMaterialSettings.get_occlusion_map());
        // Set surface texture settings
        material.SetFloatArray("_MapScale", surfaceMaterialSettings.get_scale());
        material.SetFloatArray("_NormalStrength", surfaceMaterialSettings.get_normal_strength());
        material.SetFloatArray("_OcclusionStrength", surfaceMaterialSettings.get_occlusion_strength());
        material.SetFloatArray("_Metallic", surfaceMaterialSettings.get_metallic());
        material.SetFloatArray("_Glossiness", surfaceMaterialSettings.get_glossiness());
        material.SetColorArray("_Color", surfaceMaterialSettings.get_colors());
    }

    private void initialize_mesh_filter()
    {
        // Delete all children meshes
        foreach (Transform child in transform)
            DestroyImmediate(child.gameObject);

        // mesh and mesh filter not needed if using instanced rendering
        if(SphereType == SphereMeshGenerator.SphereType.Tile) return;

        // Create mesh object (later maybe move this into generation, if we want to have more meshes per one celestial object)
        GameObject mesh_obj = new("Mesh");
        mesh_obj.transform.parent = transform;
        mesh_obj.transform.localPosition = Vector3.zero;
        mesh_obj.AddComponent<MeshRenderer>().sharedMaterial = material;

        mesh_filter = mesh_obj.AddComponent<MeshFilter>();
        mesh_filter.sharedMesh = new()
        {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };

        transform.localPosition = Vector3.zero;
    }

    private void generate_mesh()
    {
        // Generate unit sphere
        Mesh mesh = new Mesh();
        SphereMeshGenerator.construct_mesh(mesh, (uint) resolution, SphereType);
        mesh.RecalculateNormals();

        // Get mesh data
        var positions = mesh.vertices;
        var normals = mesh.normals;
        var uvs = mesh.uv;
        var vertex_count = positions.Length;

        // Keep reference to old buffers
        var old_initial_pos_buffer = initial_pos_buffer;
        var old_position_buffer = position_buffer;
        var old_normal_buffer = normal_buffer;
        var old_biome_buffer = biome_buffer;
        var old_uv_buffer = uv_buffer;

        if(SphereType == SphereMeshGenerator.SphereType.Tile)
        {
            // Store tile mesh
            tile_mesh = mesh;

            // Initialize buffers
            int max_vertices = (int) (vertex_count * LodQuadTree.MAX_NUM_NODES * 6);
            initial_pos_buffer = new ComputeBuffer(max_vertices, 3 * sizeof(float));
            position_buffer = new ComputeBuffer(max_vertices, 3 * sizeof(float));
            normal_buffer = new ComputeBuffer(max_vertices, 3 * sizeof(float));
            biome_buffer = new ComputeBuffer(max_vertices / 4 + 1, sizeof(uint));
            uv_buffer = new ComputeBuffer(max_vertices, 2 * sizeof(float));

            // Initialize shape settings
            shapeSettings.initialize(transform, initial_pos_buffer, position_buffer, normal_buffer, biome_buffer, vertex_count, true, tile_mesh.GetIndexCount(0));
        } else {
            // Set mesh in mesh filter
            mesh_filter.sharedMesh = mesh;

            // Initialize buffers
            initial_pos_buffer = new ComputeBuffer(vertex_count, 3 * sizeof(float));
            position_buffer = new ComputeBuffer(vertex_count, 3 * sizeof(float));
            normal_buffer = new ComputeBuffer(vertex_count, 3 * sizeof(float));
            biome_buffer = new ComputeBuffer(vertex_count / 4 + 1, sizeof(uint));
            uv_buffer = new ComputeBuffer(vertex_count, 2 * sizeof(float));

            // Set initial buffer data
            initial_pos_buffer.SetData(positions, 0, 0, vertex_count);
            position_buffer.SetData(positions, 0, 0, vertex_count);
            normal_buffer.SetData(normals, 0, 0, vertex_count);
            uv_buffer.SetData(uvs, 0, 0, vertex_count);

            // Initialize shape noise settings
            shapeSettings.initialize(transform, initial_pos_buffer, position_buffer, normal_buffer, biome_buffer, vertex_count, false, 0);
        }

        // Initialize culling
        if (main_camera != null)
            shapeSettings.setup_view_based_culling(main_camera.transform);

        old_initial_pos_buffer?.Release();
        old_position_buffer?.Release();
        old_normal_buffer?.Release();
        old_biome_buffer?.Release();
        old_uv_buffer?.Release();

        // Set surface material info
        set_surface_material_info();
    }

    private void apply_noise()
    {
        if (shapeSettings == null)
            throw new UnityException("Error in :: CelestialObject :: apply_noise :: Shape settings not set!");
        shapeSettings.apply_noise();
    }

    private void update_view_based_culling() {
        if (main_camera == null) return;
        if (shapeSettings == null)
            throw new UnityException("Error in :: CelestialObject :: OnCameraTransformChanged :: Shape settings not set!");
        shapeSettings.update_view_based_culling();
    }
}
