using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class RockyPlanetShapeSettings : ShapeSettings {
    // Continent shape
    [Range(0, 1)]
    public float continentRatio = 0;
    [Range(0, 1)]
    public float oceanDepth = 1;
    [Range(0, 1)]
    public float flatness = 0;
    // continent noise settings
    public NoiseSettings continentNoise;
    // flatness noise
    public NoiseSettings flatnessNoise;
    // general noise
    public NoiseSettings generalNoise;
    // mountains
    public MNoiseSettings mountainsNoise;
    // underwater mountains
    public UMNoiseSettings underwaterMountainsNoise;
    // craters
    public CraterNoiseSettings craterNoise;


    // Constructors
    public override void set_settings(ShapeSettings settings_in) {
        if (!(settings_in is RockyPlanetShapeSettings)) throw new UnityException("Error in :: ShapeSettings :: set_settings :: cannot set settings to the settings of wrong type.");
        RockyPlanetShapeSettings settings = (RockyPlanetShapeSettings) settings_in;
        base.set_settings(settings);
        continentRatio = settings.continentRatio;
        oceanDepth = settings.oceanDepth;
        flatness = settings.flatness;
        continentNoise = new NoiseSettings(settings.continentNoise);
        flatnessNoise = new NoiseSettings(settings.flatnessNoise);
        generalNoise = new NoiseSettings(settings.generalNoise);
        mountainsNoise = new MNoiseSettings(settings.mountainsNoise);
        underwaterMountainsNoise = new UMNoiseSettings(settings.underwaterMountainsNoise);
        craterNoise = new CraterNoiseSettings(settings.craterNoise);
    }

    public override void randomize_seed() {
        base.randomize_seed();
        continentNoise.randomize_seed();
        flatnessNoise.randomize_seed();
        generalNoise.randomize_seed();
        mountainsNoise.randomize_seed();
        underwaterMountainsNoise.randomize_seed();
        craterNoise.randomize_seed();
    }

    public override void apply_noise() {
        // Send noise settings
        shapeComputeShader.SetInts("enabled", get_enables());
        shapeComputeShader.SetFloats("noise_settings_continent_shape", get_continent_noise_settings());
        shapeComputeShader.SetFloats("noise_settings_flatness", get_flatness_noise_settings());
        shapeComputeShader.SetFloats("noise_settings_both", get_general_noise_settings());
        shapeComputeShader.SetFloats("noise_settings_mountains", get_mountains_noise_settings());
        shapeComputeShader.SetFloats("noise_settings_ocean_mountains", get_underwater_mountains_noise_settings());
        shapeComputeShader.SetFloats("noise_settings_crater", get_crater_noise_settings());

        // Set continent height
        float continent_base = (1f - continentRatio * 2f) * continentNoise.strength + continentNoise.baseHeight;
        shapeComputeShader.SetFloat("continent_base", continent_base);
        // Set ocean depth
        float ocean_depth = continent_base - (continent_base + continentNoise.strength - continentNoise.baseHeight) * this.oceanDepth;
        if (continentRatio == 1f)
            ocean_depth = continent_base - 1f;
        shapeComputeShader.SetFloat("ocean_depth", ocean_depth);
        // Set flatness
        float flatness_ratio = (flatness * 2f - 1f) * flatnessNoise.strength + flatnessNoise.baseHeight;
        shapeComputeShader.SetFloat("flatness_ratio", flatness_ratio);

        // Set radius & Run
        base.apply_noise();
    }

    int[] get_enables() {
        return new int[] {
            continentNoise.enable? 1 : 0,
            generalNoise.enable? 1 : 0,
            mountainsNoise.enable? 1 : 0,
            underwaterMountainsNoise.enable? 1 : 0,
            flatnessNoise.enable? 1 : 0,
            craterNoise.enable? 1 : 0
        };
    }

    float[] get_continent_noise_settings() {
        return new float[] {
            continentNoise.numberOfLayers,
            continentNoise.amplitudeFading,
            continentNoise.baseFrequency,
            continentNoise.frequencyMultiplier,
            continentNoise.strength,
            continentNoise.baseHeight,
            continentNoise.seed.x,
            continentNoise.seed.y,
            continentNoise.seed.z
        };
    }
    float[] get_flatness_noise_settings() {
        return new float[] {
            flatnessNoise.numberOfLayers,
            flatnessNoise.amplitudeFading,
            flatnessNoise.baseFrequency,
            flatnessNoise.frequencyMultiplier,
            flatnessNoise.strength,
            flatnessNoise.baseHeight,
            flatnessNoise.seed.x,
            flatnessNoise.seed.y,
            flatnessNoise.seed.z
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
    float[] get_mountains_noise_settings() {
        return new float[] {
            mountainsNoise.numberOfLayers,
            mountainsNoise.amplitudeFading,
            mountainsNoise.baseFrequency,
            mountainsNoise.frequencyMultiplier,
            mountainsNoise.strength,
            mountainsNoise.baseHeight,
            mountainsNoise.seed.x,
            mountainsNoise.seed.y,
            mountainsNoise.seed.z,
            mountainsNoise.power,
            mountainsNoise.gain
        };
    }
    float[] get_underwater_mountains_noise_settings() {
        return new float[] {
            underwaterMountainsNoise.numberOfLayers,
            underwaterMountainsNoise.amplitudeFading,
            underwaterMountainsNoise.baseFrequency,
            underwaterMountainsNoise.frequencyMultiplier,
            underwaterMountainsNoise.strength,
            underwaterMountainsNoise.baseHeight,
            underwaterMountainsNoise.seed.x,
            underwaterMountainsNoise.seed.y,
            underwaterMountainsNoise.seed.z,
            underwaterMountainsNoise.power,
            underwaterMountainsNoise.gain,
            underwaterMountainsNoise.offset
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