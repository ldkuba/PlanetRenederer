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

    // Properties
    private float continent_base => (1f - continentRatio * 2f) * continentNoise.strength + continentNoise.baseHeight;
    private float ocean_floor(float continent_base) => continent_base - (continent_base + continentNoise.strength - continentNoise.baseHeight) * oceanDepth;
    private float flatness_ratio => (flatness * 2f - 1f) * flatnessNoise.strength + flatnessNoise.baseHeight;

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

    protected override Vector2 noise_range() {
        var factor = 0.25f;
        var gn_range = factor * generalNoise.get_noise_range();
        var mn_range = factor * mountainsNoise.get_noise_range();
        var un_range = factor * underwaterMountainsNoise.get_noise_range();
        var cn_range = factor * craterNoise.get_noise_range();

        // Eliminate mountains for fully flat terrain
        if (flatness == 1.0) {
            mn_range = new();
            un_range = new();
        }

        // On continent
        var continent_range = mn_range + cn_range;

        // On ocean floor
        var ocean_range = new Vector2();
        if (continentNoise.enable) {
            var c_base = continent_base;
            var ocean_depth = c_base - ocean_floor(c_base);
            ocean_range = un_range - new Vector2(ocean_depth, ocean_depth);
        }

        // Combined range
        var range = new Vector2(
            Mathf.Min(ocean_range.x, continent_range.x),
            Mathf.Max(ocean_range.y, continent_range.y)
        );
        range += gn_range;

        // Account for radius
        range = radius * (range + new Vector2(1, 1));

        return range;
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
        float c_base = continent_base;
        shapeComputeShader.SetFloat("continent_base", c_base);
        // Set ocean depth
        shapeComputeShader.SetFloat("ocean_depth", ocean_floor(c_base));
        // Set flatness
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