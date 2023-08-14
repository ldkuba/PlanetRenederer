using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CelestialObject : MonoBehaviour {
    // components
    [SerializeField, HideInInspector]
    protected MeshFilter MeshF;
    public ShapeSettings ShapeSettings;

    // settings
    [HideInInspector]
    public SphereMeshGenerator.SphereType SphereType = SphereMeshGenerator.SphereType.Cube;
    [HideInInspector]
    public int Resolution = 100;
    public Material Material;

    // Public Methods
    [ContextMenu("initialize")]
    public void initialize() {
        initialize_mesh_filter();
    }

    public void OnResolutionChanged() {
        generate_mesh();
    }

    public void OnShapeSettingsUpdated() {
        apply_noise();
    }

    public Vector3[] get_vertices() {
        return MeshF.sharedMesh.vertices;
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
        mesh_obj.AddComponent<MeshRenderer>().sharedMaterial = Material;

        MeshF = mesh_obj.AddComponent<MeshFilter>();
        MeshF.sharedMesh = new() {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };

        transform.localPosition = Vector3.zero;
    }

    private void generate_mesh() {
        // Generate unity sphere
        SphereMeshGenerator.construct_mesh(MeshF.sharedMesh, (uint) Resolution, SphereType);
    }

    private void apply_noise() {
        if (ShapeSettings == null) {
            Debug.Log("Shape settings not set!");
            return;
        }

        var vertices_deformed = ShapeSettings.apply_noise(MeshF.sharedMesh.vertices);
        MeshF.sharedMesh.vertices = vertices_deformed;
        MeshF.sharedMesh.RecalculateNormals();
    }
}
