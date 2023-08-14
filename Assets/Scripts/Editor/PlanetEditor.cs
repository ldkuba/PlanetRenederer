using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Planet))]
public class PlanetEditor : CelestialObjectEditor {

    private void OnEnable() {
        CO = (Planet) target;
    }

    public override void OnInspectorGUI() {
        Planet planet = (Planet) CO;

        using (var check = new EditorGUI.ChangeCheckScope()) {
            base.OnInspectorGUI();
            if (check.changed) planet.generate_planet();
        }
    }
}