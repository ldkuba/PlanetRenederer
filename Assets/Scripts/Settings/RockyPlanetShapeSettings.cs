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
        if (settings_in is not RockyPlanetShapeSettings)
            throw new UnityException("Error in :: RockyPlanetShapeSettings :: set_settings :: Cannot set settings to the settings of wrong type.");
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

    protected override void set_additional_noise_settings() {
        // Send noise settings
        shapeComputeShader.SetInts("enabled", get_enables());
        shapeComputeShader.SetFloats("noise_settings_continent_shape", continentNoise.get_noise());
        shapeComputeShader.SetFloats("noise_settings_flatness", flatnessNoise.get_noise());
        shapeComputeShader.SetFloats("noise_settings_both", generalNoise.get_noise());
        shapeComputeShader.SetFloats("noise_settings_mountains", mountainsNoise.get_noise());
        shapeComputeShader.SetFloats("noise_settings_ocean_mountains", underwaterMountainsNoise.get_noise());
        shapeComputeShader.SetFloats("noise_settings_crater", craterNoise.get_noise());

        // Set continent height
        float cr = 1f - continentRatio * 2f;
        float continent_base = cr * cr * cr * continentNoise.strength + continentNoise.baseHeight;
        shapeComputeShader.SetFloat("continent_base", continent_base);
        // Set ocean depth
        float od = oceanDepth * oceanDepth * oceanDepth;
        float ocean_depth = continent_base - (continent_base + continentNoise.strength - continentNoise.baseHeight) * od;
        if (continentRatio == 1f)
            ocean_depth = continent_base - 1f;
        shapeComputeShader.SetFloat("ocean_depth", ocean_depth);
        // Set flatness
        float flatness_ratio = (flatness * 2f - 1f) * flatnessNoise.strength + flatnessNoise.baseHeight;
        shapeComputeShader.SetFloat("flatness_ratio", flatness_ratio);
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
}