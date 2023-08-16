using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CelestialObjectGenerator : MonoBehaviour {

    public enum COType {
        Asteroid,
        Moon,
        RockyDryPlanet,
        RockyWetPlanet,
        GasPlanet,
        Star
    }

    public AsteroidShapeSettings DefaultAsteroidShapeSettings;
    public RockyPlanetShapeSettings DefaultMoonShapeSettings;
    public RockyPlanetShapeSettings DefaultRockyPlanetDryShapeSettings;
    public RockyPlanetShapeSettings DefaultRockyPlanetWetShapeSettings;
    public OceanShapeSettings DefaultOceanShapeSettings;

    [SerializeField]
    Material SurfaceMaterial;
    [SerializeField]
    Material OceanMaterial;
    [SerializeField]
    Material StarMaterial;

    public string ObjectName = "Celestial Object";
    public COType ObjectType = COType.Asteroid;
    [Min(0.5f)]
    public float ObjectRadius = 1f;

    public SphereMeshGenerator.SphereType SphereType = SphereMeshGenerator.SphereType.Cube;
    public int SphereResolution = 100;

    public void generate_object() {
        GameObject celestial_body = new(ObjectName);

        GameObject surface = new("surface");
        surface.transform.SetParent(celestial_body.transform);

        // if object is has no surface
        if (ObjectType == COType.Star) {
            // star script
            StarSphere starS = surface.AddComponent<StarSphere>();
            // resolution
            starS.SphereType = SphereType;
            starS.resolution = SphereResolution;
            // material
            starS.material = StarMaterial;
            // radius
            starS.Radius = ObjectRadius;
            starS.OnRadiusUpdate();
            // tag
            starS.gameObject.tag = "StarSurface";
            // initialize
            starS.initialize();
            starS.OnShapeSettingsUpdated();

            // light
            GameObject light = Instantiate(Resources.Load<GameObject>("Starlight"));
            light.transform.SetParent(celestial_body.transform);
            light.name = ObjectName + " " + "Light";

            return;
        }
        if (ObjectType == COType.GasPlanet) {
            return;
        }

        // if object is solid
        // planet script
        Planet planetS = surface.AddComponent<Planet>();
        // resolution
        planetS.SphereType = SphereType;
        planetS.resolution = SphereResolution;
        // material
        planetS.material = SurfaceMaterial;
        // shape
        switch (ObjectType) {
            case COType.Asteroid:
                planetS.shapeSettings = ScriptableObject.CreateInstance<AsteroidShapeSettings>();
                planetS.shapeSettings.set_settings(DefaultAsteroidShapeSettings);
                break;
            case COType.Moon:
                planetS.shapeSettings = ScriptableObject.CreateInstance<RockyPlanetShapeSettings>();
                planetS.shapeSettings.set_settings(DefaultMoonShapeSettings);
                break;
            case COType.RockyDryPlanet:
                planetS.shapeSettings = ScriptableObject.CreateInstance<RockyPlanetShapeSettings>();
                planetS.shapeSettings.set_settings(DefaultRockyPlanetDryShapeSettings);
                break;
            case COType.RockyWetPlanet:
                planetS.shapeSettings = ScriptableObject.CreateInstance<RockyPlanetShapeSettings>();
                planetS.shapeSettings.set_settings(DefaultRockyPlanetWetShapeSettings);
                break;
        }
        planetS.shapeSettings.radius = ObjectRadius;
        planetS.shapeSettings.randomize_seed();
        // tag
        planetS.gameObject.tag = "Surface";
        // initialize
        planetS.generate_planet();

        // if object has an ocean
        if (ObjectType == COType.RockyWetPlanet) {
            GameObject ocean = new("ocean");
            // ocean script
            OceanSphere oceanS = ocean.AddComponent<OceanSphere>();
            // resolution
            oceanS.SphereType = SphereType;
            oceanS.resolution = SphereResolution;
            // material
            oceanS.material = OceanMaterial;
            // shape
            oceanS.shapeSettings = ScriptableObject.CreateInstance<OceanShapeSettings>();
            oceanS.shapeSettings.set_settings(DefaultOceanShapeSettings);
            oceanS.shapeSettings.radius = ObjectRadius;
            oceanS.shapeSettings.randomize_seed();
            // tag
            oceanS.gameObject.tag = "Ocean";
            // initialize
            oceanS.generate_ocean();

            // Set colors
            oceanS.set_mesh_wave_color_mask(planetS.get_vertices(), 8);

            ocean.transform.SetParent(celestial_body.transform);
        }
    }
}
