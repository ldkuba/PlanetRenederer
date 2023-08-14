using UnityEngine;

public class SphereMesh {
    public int[] indices;
    public Vector3[] vertices;

    public SphereMesh() {
        vertices = null;
        indices = null;
    }
    public SphereMesh(int[] indices, Vector3[] vertices) {
        this.indices = indices;
        this.vertices = vertices;
    }
}

public static class SphereMeshGenerator {
    //  Sphere type
    public enum SphereType {
        Spiral,
        Cube
    }

    // Provided functionality

    /// <summary>
    /// Construct unity sphere with given resolution.
    /// </summary>
    /// <param name="target_mesh">Mesh in which to store vertex and index data generated</param>
    /// <param name="resolution">Resolution of the unit sphere.</param>
    /// <param name="type">Type of sphere being generated. Determines generation algorithm</param>
    /// <exception cref="System.Exception">If generation algorithm for the given type is unimplemented.</exception>
    public static void construct_mesh(Mesh target_mesh, uint resolution, SphereType type = SphereType.Cube) {
        // Construct unit sphere
        SphereMesh unit_sphere;
        switch (type) {
            case SphereType.Spiral:
                unit_sphere = SpiralSphereMesh.construct_mesh(target_mesh, resolution);
                break;
            case SphereType.Cube:
                unit_sphere = CubeSphereMesh.construct_mesh(target_mesh, resolution);
                break;
            default:
                throw new System.Exception("Unimplemented.");
        }

        // Transform into mesh
        target_mesh.Clear();
        target_mesh.vertices = unit_sphere.vertices;
        target_mesh.triangles = unit_sphere.indices;
        target_mesh.SetUVs(0, unit_sphere.vertices);
    }
}
