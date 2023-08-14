using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CelestialObjectGenerator))]
public class COGenEditor : Editor {
    CelestialObjectGenerator COG;
    SerializedObject COG_serialized;

    private void OnEnable() {
        COG = (CelestialObjectGenerator) target;
        COG_serialized = new SerializedObject(COG);
    }

    public override void OnInspectorGUI() {
        // Default noise settings
        EditorGUILayout.LabelField("Default Noise Settings");
        switch (COG.ObjectType) {
            case CelestialObjectGenerator.COType.Asteroid:
                EditorGUILayout.PropertyField(COG_serialized.FindProperty("DefaultAsteroidShapeSettings"), true);
                break;
            case CelestialObjectGenerator.COType.Moon:
                EditorGUILayout.PropertyField(COG_serialized.FindProperty("DefaultMoonShapeSettings"), true);
                break;
            case CelestialObjectGenerator.COType.RockyDryPlanet:
                EditorGUILayout.PropertyField(COG_serialized.FindProperty("DefaultRockyPlanetDryShapeSettings"), true);
                break;
            case CelestialObjectGenerator.COType.RockyWetPlanet:
                EditorGUILayout.PropertyField(COG_serialized.FindProperty("DefaultRockyPlanetWetShapeSettings"), true);
                EditorGUILayout.PropertyField(COG_serialized.FindProperty("DefaultOceanShapeSettings"), true);
                break;
        }

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Sphere generation");
        EditorGUILayout.PropertyField(COG_serialized.FindProperty("SphereType"));

        int min_resolution = 1;
        int max_resolution = 10;
        switch (COG.SphereType) {
            case SphereMeshGenerator.SphereType.Spiral:
                min_resolution = 20;
                max_resolution = 1000000;
                break;
            case SphereMeshGenerator.SphereType.Cube:
                min_resolution = 2;
                max_resolution = 1000;
                break;
        }
        if (COG.SphereResolution < min_resolution) COG.SphereResolution = min_resolution;
        if (COG.SphereResolution > max_resolution) COG.SphereResolution = max_resolution;
        EditorGUILayout.IntSlider(COG_serialized.FindProperty("SphereResolution"), min_resolution, max_resolution);

        EditorGUILayout.PropertyField(COG_serialized.FindProperty("ObjectRadius"), new("Radius"), true);

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Materials Used");
        if (COG.ObjectType == CelestialObjectGenerator.COType.GasPlanet) {
        } else if (COG.ObjectType == CelestialObjectGenerator.COType.Star) {
            EditorGUILayout.PropertyField(COG_serialized.FindProperty("StarMaterial"), true);
        } else {
            EditorGUILayout.PropertyField(COG_serialized.FindProperty("SurfaceMaterial"), true);
            if (COG.ObjectType == CelestialObjectGenerator.COType.RockyWetPlanet)
                EditorGUILayout.PropertyField(COG_serialized.FindProperty("OceanMaterial"), true);
        }

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Object Settings");

        EditorGUILayout.PropertyField(COG_serialized.FindProperty("ObjectName"), true);
        EditorGUILayout.PropertyField(COG_serialized.FindProperty("ObjectType"), true);

        if (GUILayout.Button("Generate")) {
            COG.generate_object();
        }

        COG_serialized.ApplyModifiedProperties();
    }
}