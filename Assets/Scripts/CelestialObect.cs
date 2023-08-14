using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CelestialObject : MonoBehaviour {
    // components
    [SerializeField, HideInInspector]
    protected MeshFilter MeshFilter;
    public ShapeSettings ShapeSettings;

    // settings
    [HideInInspector]
    public SphereMeshGenerator.SphereType SphereType = SphereMeshGenerator.SphereType.Cube;
    [HideInInspector]
    public int Resolution = 100;
    public Material Material;

    [ContextMenu("initialize")]
    public void initialize() {
        initialize_mesh_filter();
    }

    private void initialize_mesh_filter() {
        // clear all sub meshes
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        // initialize mesh filter
        GameObject mesh_obj = new("Mesh");
        mesh_obj.transform.parent = transform;
        mesh_obj.transform.localPosition = Vector3.zero;
        mesh_obj.AddComponent<MeshRenderer>().sharedMaterial = Material;

        MeshFilter = mesh_obj.AddComponent<MeshFilter>();
        MeshFilter.sharedMesh = new() {
            indexFormat = UnityEngine.Rendering.IndexFormat.UInt32
        };

        transform.localPosition = Vector3.zero;
    }

    private void generate_mesh() {
        if (ShapeSettings == null) {
            Debug.Log("Shape settings not set!");
            return;
        }

        // Generate unity sphere
        SphereMeshGenerator.construct_mesh(MeshFilter.sharedMesh, (uint) Resolution, SphereType);

        // Apply noise
        var vertices_deformed = ShapeSettings.apply_noise(MeshFilter.sharedMesh.vertices);
        MeshFilter.sharedMesh.vertices = vertices_deformed;
        MeshFilter.sharedMesh.RecalculateNormals();
    }

    public void OnShapeSettingsUpdated() {
        generate_mesh();
    }

    public Vector3[] get_vertices() {
        return MeshFilter.sharedMesh.vertices;
    }
}
