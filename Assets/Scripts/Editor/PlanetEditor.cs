using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Planet))]
public class PlanetEditor : CelestialObjectEditor {
    private void OnEnable() {
        CO = (Planet) target;
    }
}