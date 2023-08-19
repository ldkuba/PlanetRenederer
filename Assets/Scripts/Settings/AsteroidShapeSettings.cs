using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class AsteroidShapeSettings : ShapeSettings {
    // Asteroid shape
    public NoiseSettings shapeNoise;
    // general noise
    public NoiseSettings generalNoise;
    // craters
    public CraterNoiseSettings craterNoise;

    // Constructors
    public override void set_settings(ShapeSettings settings_in) {
        if (!(settings_in is AsteroidShapeSettings))
            throw new UnityException("Error in :: AsteroidShapeSettings :: set_settings :: Cannot set settings to the settings of wrong type.");
        AsteroidShapeSettings settings = (AsteroidShapeSettings) settings_in;
        base.set_settings(settings);
        shapeNoise = new NoiseSettings(settings.shapeNoise);
        generalNoise = new NoiseSettings(settings.generalNoise);
        craterNoise = new CraterNoiseSettings(settings.craterNoise);
    }

    public override void randomize_seed() {
        base.randomize_seed();
        shapeNoise.randomize_seed();
        generalNoise.randomize_seed();
        craterNoise.randomize_seed();
    }

    protected override void set_additional_noise_settings() {
        // send noise settings
        shapeComputeShader.SetInts("enabled", get_enables());
        shapeComputeShader.SetFloats("noise_settings_shape", shapeNoise.get_noise());
        shapeComputeShader.SetFloats("noise_settings_general", generalNoise.get_noise());
        shapeComputeShader.SetFloats("noise_settings_crater", craterNoise.get_noise());
    }

    int[] get_enables() {
        return new int[] {
            shapeNoise.enable? 1 : 0,
            generalNoise.enable? 1 : 0,
            craterNoise.enable? 1 : 0
        };
    }
}
