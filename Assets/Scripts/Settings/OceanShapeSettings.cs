using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class OceanShapeSettings : ShapeSettings {
    // TODO: add custom settings

    public override void set_settings(ShapeSettings settings_in) {
        if (!(settings_in is OceanShapeSettings)) throw new UnityException("Error in :: ShapeSettings :: set_settings :: cannot set settings to the settings of wrong type.");
        OceanShapeSettings settings = (OceanShapeSettings) settings_in;
        base.set_settings(settings);
        // TODO: same
    }

    public override void randomize_seed() {
        base.randomize_seed();
    }
}
