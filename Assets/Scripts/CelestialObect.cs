using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CelestialObject : MonoBehaviour {
    // Components
    public ShapeSettings shapeSettings;
    [SerializeField, HideInInspector]
    protected MeshFilter mesh_filter;
    public GameObject mainCamera;

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

    public CelestialObject() {
        Debug.Log("CO created");
    }
    ~CelestialObject() {
        Debug.Log("CO destroyed");
        initial_pos_buffer?.Release();
        position_buffer?.Release();
        normal_buffer?.Release();
        uv_buffer?.Release();
    }

    // Public Methods
    [ContextMenu("initialize")]
    public void initialize() {
        initialize_mesh_filter();
    }

    public void OnResolutionChanged() {
        generate_mesh();
        apply_noise();
    }

    public void OnShapeSettingsUpdated() {
        apply_noise();
    }

    public Vector3[] get_vertices() {
        return mesh_filter.sharedMesh.vertices;
    }

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
        shapeSettings.initialize(initial_pos_buffer, position_buffer, normal_buffer, vertex_count);

        // Initialize culling
        if (mainCamera != null)
            shapeSettings.setup_view_based_culling(transform, mainCamera.transform);

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
        // MeshF.sharedMesh.RecalculateNormals();
    }
}
