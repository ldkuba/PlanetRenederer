using UnityEngine;

public class ShapeSettings : ScriptableObject {
    [System.Serializable]
    public class NoiseSettings {
        public bool enable;
        [Range(1, 10)]
        public int numberOfLayers = 1;
        [Min(0f)]
        public float amplitudeFading;
        [Min(0f)]
        public float baseFrequency = 1f;
        [Min(0f)]
        public float frequencyMultiplier;
        public float strength = 1f;
        public float baseHeight;
        public Vector3 seed;

        private static readonly System.Random r = new();

        public NoiseSettings(NoiseSettings settings) {
            enable = settings.enable;
            numberOfLayers = settings.numberOfLayers;
            amplitudeFading = settings.amplitudeFading;
            baseFrequency = settings.baseFrequency;
            frequencyMultiplier = settings.frequencyMultiplier;
            strength = settings.strength;
            baseHeight = settings.baseHeight;
            seed.x = settings.seed.x;
            seed.y = settings.seed.y;
            seed.z = settings.seed.z;
        }

        public virtual float[] get_noise() {
            return new float[] {
                numberOfLayers,
                amplitudeFading,
                baseFrequency,
                frequencyMultiplier,
                strength,
                baseHeight,
                seed.x,
                seed.y,
                seed.z
            };
        }

        public void randomize_seed() {
            seed.x = rand_to_float(r.NextDouble(), r.Next(15));
            seed.y = rand_to_float(r.NextDouble(), r.Next(15));
            seed.z = rand_to_float(r.NextDouble(), r.Next(15));
        }
        private float rand_to_float(double mantissa, int exponent) {
            double mn = 2.0 * mantissa - 1.0;
            double ex = System.Math.Pow(2.0, exponent);
            return (float) (mn * ex);
        }
    }

    [System.Serializable]
    public class MNoiseSettings : NoiseSettings {
        public float power = 1f;
        [Range(0, 1)]
        public float gain = 1f;

        public MNoiseSettings(MNoiseSettings settings) : base(settings) {
            power = settings.power;
            gain = settings.gain;
        }

        public override float[] get_noise() {
            return new float[] {
                numberOfLayers,
                amplitudeFading,
                baseFrequency,
                frequencyMultiplier,
                strength,
                baseHeight,
                seed.x,
                seed.y,
                seed.z,
                power,
                gain
            };
        }
    }

    [System.Serializable]
    public class UMNoiseSettings : MNoiseSettings {
        public float offset;

        public UMNoiseSettings(UMNoiseSettings settings) : base(settings) {
            offset = settings.offset;
        }

        public override float[] get_noise() {
            return new float[] {
                numberOfLayers,
                amplitudeFading,
                baseFrequency,
                frequencyMultiplier,
                strength,
                baseHeight,
                seed.x,
                seed.y,
                seed.z,
                power,
                gain,
                offset
            };
        }
    }

    [System.Serializable]
    public class CraterNoiseSettings : NoiseSettings {
        [Range(0, 1)]
        public float depth = 0.5f;
        [Min(1f)]
        public float radius = 2f;
        [Min(1)]
        public int slope = 12;
        [Min(0f)]
        public float centralElevationHeight;
        [Min(0f)]
        public float centralElevationWidth;
        [Min(0f)]
        public float outsideSlope;
        public float jitter;

        public CraterNoiseSettings(CraterNoiseSettings settings) : base(settings) {
            depth = settings.depth;
            radius = settings.radius;
            slope = settings.slope;
            centralElevationHeight = settings.centralElevationHeight;
            centralElevationWidth = settings.centralElevationWidth;
            outsideSlope = settings.outsideSlope;
            jitter = settings.jitter;
        }

        public override float[] get_noise() {
            return new float[] {
                numberOfLayers,
                depth,
                amplitudeFading,
                baseFrequency,
                frequencyMultiplier,
                radius,
                slope,
                centralElevationHeight,
                centralElevationWidth,
                outsideSlope,
                jitter,
                strength,
                baseHeight,
                seed.x,
                seed.y,
                seed.z
            };
        }
    }

    // Shape settings
    [Min(0.5f)]
    public float radius = 1f;

    // Noise shape compute
    public ComputeShader shapeComputeShader;
    private int shader_kernel_id;
    private uint thread_x;
    private uint thread_y;
    private uint thread_z;

    // Noise compute buffers
    ComputeBuffer initial_position_buffer;
    ComputeBuffer position_buffer;
    ComputeBuffer normal_buffer;
    int vertex_count;

    public virtual void set_settings(ShapeSettings settings) {
        shapeComputeShader = settings.shapeComputeShader;
        radius = settings.radius;
    }
    public virtual void randomize_seed() { }

    public virtual void initialize(
        ComputeBuffer initial_position_buffer,
        ComputeBuffer position_buffer,
        ComputeBuffer normal_buffer,
        int vertex_count
    ) {
        if (shapeComputeShader == null)
            throw new UnityException("Error in :: ShapeSettings :: initialize :: Compute shader not set.");
        if (initial_position_buffer.count < position_buffer.count)
            throw new UnityException("Error in :: ShapeSettings :: initialize :: Initial position buffer incompatible with the current one.");

        // Here we will setup compute shader
        // First we need to find kernel
        shader_kernel_id = shapeComputeShader.FindKernel("PlanetShapeCompute");

        // Compute required thread count
        shapeComputeShader.GetKernelThreadGroupSizes(shader_kernel_id, out thread_x, out thread_y, out thread_z);
        thread_x = (uint) Mathf.CeilToInt(1.0f * vertex_count / thread_x);

        // Set buffers
        this.initial_position_buffer = initial_position_buffer;
        this.position_buffer = position_buffer;
        this.normal_buffer = normal_buffer;
        this.vertex_count = vertex_count;

        // Set number of vertices
        shapeComputeShader.SetInt("num_of_vertices", vertex_count);
    }

    private void set_core_noise_settings() {
        // Send radius
        shapeComputeShader.SetFloat("radius", radius);

        // Set buffers
        shapeComputeShader.SetBuffer(shader_kernel_id, "vertices", initial_position_buffer);
        shapeComputeShader.SetBuffer(shader_kernel_id, "out_vertices", position_buffer);
        shapeComputeShader.SetBuffer(shader_kernel_id, "normals", normal_buffer);

        // Set number of vertices
        shapeComputeShader.SetInt("num_of_vertices", vertex_count);
    }
    protected virtual void set_additional_noise_settings() { }

    public void apply_noise() {
        // Check validity
        if (shapeComputeShader == null)
            throw new UnityException("Error in :: ShapeSettings :: apply_noise :: Compute shader not set.");

        // Set noise settings
        set_core_noise_settings();
        set_additional_noise_settings();

        // run
        shapeComputeShader.Dispatch(shader_kernel_id, (int) thread_x, (int) thread_y, (int) thread_z);
    }
}
