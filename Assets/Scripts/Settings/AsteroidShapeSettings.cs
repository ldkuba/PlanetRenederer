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
        if (!(settings_in is AsteroidShapeSettings)) throw new UnityException("Error in :: ShapeSettings :: set_settings :: cannot set settings to the settings of wrong type.");
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

    public override void apply_noise() {
        // send noise settings
        shapeComputeShader.SetInts("enabled", get_enables());
        shapeComputeShader.SetFloats("noise_settings_shape", get_shape_noise_settings());
        shapeComputeShader.SetFloats("noise_settings_general", get_general_noise_settings());
        shapeComputeShader.SetFloats("noise_settings_crater", get_crater_noise_settings());

        // Set radius & Run
        base.apply_noise();
    }

    int[] get_enables() {
        return new int[] {
            shapeNoise.enable? 1 : 0,
            generalNoise.enable? 1 : 0,
            craterNoise.enable? 1 : 0
        };
    }

    float[] get_shape_noise_settings() {
        return new float[] {
            shapeNoise.numberOfLayers,
            shapeNoise.amplitudeFading,
            shapeNoise.baseFrequency,
            shapeNoise.frequencyMultiplier,
            shapeNoise.strength,
            shapeNoise.baseHeight,
            shapeNoise.seed.x,
            shapeNoise.seed.y,
            shapeNoise.seed.z
        };
    }
    float[] get_general_noise_settings() {
        return new float[] {
            generalNoise.numberOfLayers,
            generalNoise.amplitudeFading,
            generalNoise.baseFrequency,
            generalNoise.frequencyMultiplier,
            generalNoise.strength,
            generalNoise.baseHeight,
            generalNoise.seed.x,
            generalNoise.seed.y,
            generalNoise.seed.z
        };
    }
    float[] get_crater_noise_settings() {
        return new float[] {
            craterNoise.numberOfLayers,
            craterNoise.depth,
            craterNoise.amplitudeFading,
            craterNoise.baseFrequency,
            craterNoise.frequencyMultiplier,
            craterNoise.radius,
            craterNoise.slope,
            craterNoise.centralElevationHeight,
            craterNoise.centralElevationWidth,
            craterNoise.outsideSlope,
            craterNoise.jitter,
            craterNoise.strength,
            craterNoise.baseHeight,
            craterNoise.seed.x,
            craterNoise.seed.y,
            craterNoise.seed.z
        };
    }
}
