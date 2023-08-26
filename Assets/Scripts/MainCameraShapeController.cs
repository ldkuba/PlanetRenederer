using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode()]
public class MainCameraShapeController : MonoBehaviour {
    public delegate void TransformChangedH();
    public event TransformChangedH transform_changed;


    // Update is called once per frame
    void Update() {
        if (transform.hasChanged) {
            OnTransformChanged();
            transform.hasChanged = false;
        }
    }

    public void OnTransformChanged() {
        transform_changed?.Invoke();
    }
}
