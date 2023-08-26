using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode()]
public abstract class CelestialObject : MonoBehaviour {
    // Components
    public ShapeSettings shapeSettings;
    [SerializeField, HideInInspector]
    protected MeshFilter mesh_filter;

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
    private ComputeBuffer uv_buffer;

    // Camera shape control
    Transform main_camera_transform;
    private MainCameraShapeController camera_shape_controller;

    void OnDestroy() {
        initial_pos_buffer?.Release();
        position_buffer?.Release();
        normal_buffer?.Release();
        uv_buffer?.Release();
        if (main_camera_transform != null)
            camera_shape_controller.transform_changed -= OnCameraTransformChanged;
    }

    private void Update() {
        if (transform.hasChanged) {
            OnTransformChanged();
            transform.hasChanged = false;
        }
    }

    public void setup_camera_shape_control(Transform main_camera_transform, MainCameraShapeController camera_shape_controller) {
        this.main_camera_transform = main_camera_transform;
        this.camera_shape_controller = camera_shape_controller;
        camera_shape_controller.transform_changed += OnCameraTransformChanged;
    }

    // Public Methods
    [ContextMenu("initialize")]
    public void Initialize() { initialize_mesh_filter(); generate_mesh(); apply_noise(); }
    public void OnResolutionChanged() { generate_mesh(); apply_noise(); }
    public void OnShapeSettingsUpdated() { apply_noise(); }
    public void OnCameraTransformChanged() { update_view_based_culling(); }
    public void OnTransformChanged() { update_view_based_culling(); }

    // Private methods
    private void initialize_mesh_filter() {
        // clear all sub meshes
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        // initialize mesh filter
        GameObject mesh_obj = new("Mesh");
        mesh_obj.transform.parent = transform;
        mesh_obj.transform.localPosition = Vector3.zero;
        mesh_obj.AddComponent<MeshRenderer>().sharedMaterial = material;

        mesh_filter = mesh_obj.AddComponent<MeshFilter>();
        mesh_filter.sharedMesh = new() {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };

        transform.localPosition = Vector3.zero;
    }

    private void generate_mesh() {
        // Generate unit sphere
        SphereMeshGenerator.construct_mesh(mesh_filter.sharedMesh, (uint) resolution, SphereType);
        mesh_filter.sharedMesh.RecalculateNormals();

        // Get mesh data
        var positions = mesh_filter.sharedMesh.vertices;
        var normals = mesh_filter.sharedMesh.normals;
        var uvs = mesh_filter.sharedMesh.uv;
        var vertex_count = positions.Length;

        // Keep reference to old buffers
        var old_initial_pos_buffer = initial_pos_buffer;
        var old_position_buffer = position_buffer;
        var old_normal_buffer = normal_buffer;
        var old_uv_buffer = uv_buffer;

        // Initialize buffers
        initial_pos_buffer = new(vertex_count, 3 * sizeof(float));
        position_buffer = new(vertex_count, 3 * sizeof(float));
        normal_buffer = new(vertex_count, 3 * sizeof(float));
        uv_buffer = new(vertex_count, 2 * sizeof(float));

        // Set initial buffer data
        initial_pos_buffer.SetData(positions, 0, 0, vertex_count);
        position_buffer.SetData(positions, 0, 0, vertex_count);
        normal_buffer.SetData(normals, 0, 0, vertex_count);
        uv_buffer.SetData(uvs, 0, 0, vertex_count);

        // Initialize shape noise settings
        shapeSettings.initialize(transform, initial_pos_buffer, position_buffer, normal_buffer, vertex_count);

        // Initialize culling
        if (main_camera_transform != null)
            shapeSettings.setup_view_based_culling(main_camera_transform);

        // Set material buffers
        material.SetBuffer("position_buffer", position_buffer);
        material.SetBuffer("normal_buffer", normal_buffer);
        material.SetBuffer("uv_buffer", uv_buffer);

        // Release old buffers if necessary
        old_initial_pos_buffer?.Release();
        old_position_buffer?.Release();
        old_normal_buffer?.Release();
        old_uv_buffer?.Release();
    }

    private void apply_noise() {
        if (shapeSettings == null)
            throw new UnityException("Error in :: CelestialObject :: apply_noise :: Shape settings not set!");
        shapeSettings.apply_noise();
    }

    private void update_view_based_culling() {
        if (main_camera_transform == null) return;
        if (shapeSettings == null)
            throw new UnityException("Error in :: CelestialObject :: OnCameraTransformChanged :: Shape settings not set!");
        shapeSettings.update_view_based_culling();
    }
}
