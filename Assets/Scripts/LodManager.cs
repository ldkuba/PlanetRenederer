using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class NodeCodeUtil {

    private static uint undilate(uint x) {
        x = (x | (x >> 1)) & 0x33333333;
        x = (x | (x >> 2)) & 0x0f0f0f0f;
        x = (x | (x >> 4)) & 0x00ff00ff;
        x = (x | (x >> 8)) & 0x0000ffff;
        return x & 0x0000ffff;
    }

    public static void decode(uint node_code, ref uint level, ref uint[] coords) {
        level = node_code & 0xf;
        coords[0] = undilate((node_code >> 4) & 0x05555555);
        coords[1] = undilate((node_code >> 5) & 0x05555555);
    }

    private static uint dilate(uint x) {
        x = (x | (x << 8)) & 0x00ff00ff;
        x = (x | (x << 4)) & 0x0f0f0f0f;
        x = (x | (x << 2)) & 0x33333333;
        x = (x | (x << 1)) & 0x55555555;
        return x & 0x55555555;
    }

    public static uint encode(uint level, uint[] coords) {
        uint node_code = 0;
        node_code |= level & 0xf;
        node_code |= dilate(coords[0]) << 4;
        node_code |= dilate(coords[1]) << 5;
        return node_code;
    }

    public static uint last_quadrant(uint node_code) {
        return (node_code & 0x30) >> 4;
    }

    public static uint[] generate_children(uint node_code) {
        node_code = (++node_code & 0xf) | ((node_code & ~((uint)0xf)) << 2);
        
        uint[] ret = new uint[4];
        ret[0] = node_code;
        ret[1] = node_code | 0x10;
        ret[2] = node_code | 0x20;
        ret[3] = node_code | 0x30;

        return ret;
    }
}

public class LodQuadTree {

    // Perfectly spaced quadtree will have 3 * max_level + 1 nodes but theoretically we can have 4^max_level nodes
    // Since we are using uint32 for the nodes we can have a max lod level of 15.
    public static readonly int MAX_LEVEL = 15;
    public static readonly int MAX_NUM_NODES = 20 * 3 * MAX_LEVEL + 1;
    public static readonly uint ROOT_NODE_CODE = 0;

    public LodQuadTree(uint node_code, uint level, Vector3 node_center, LodQuadTree parent) {
        this.node_code = node_code;
        this.node_center = node_center;
        this.parent = parent;
    }

    public List<LodQuadTree> GetAllChildren() {
        List<LodQuadTree> ret = new List<LodQuadTree>();
        
        if(children != null) {
            foreach(LodQuadTree child in children) {
                ret.Add(child);
                ret.AddRange(child.GetAllChildren());
            }
        }

        return ret;
    }

    public List<LodQuadTree> GetAllChildrenLeaves() {
        List<LodQuadTree> ret = new List<LodQuadTree>();
        
        if(children != null) {
            foreach(LodQuadTree child in children) {
                ret.AddRange(child.GetAllChildrenLeaves());
            }
        } else {
            ret.Add(this);
        }

        return ret;
    }

    public static Vector3 GetNodeCenter(uint[] coords, uint level, Vector3 face_center) {
        float two_to_level = Mathf.Pow(2, level);

        // Get center on -1 to 1 grid
        float unit_cube_x = -1.0f + (2.0f * coords[0] + 1.0f) / two_to_level;
        float unit_cube_y = -1.0f + (2.0f * coords[1] + 1.0f) / two_to_level;

        // Get center on unit cube
        Vector3 child_center_unit_cube = new Vector3();
        if(face_center.x != 0.0f) {
            // Right or left face
            child_center_unit_cube = new Vector3(face_center.x, unit_cube_y, unit_cube_x * face_center.x);
        } else if(face_center.y != 0.0f) {
            // Top or bottom face
            child_center_unit_cube = new Vector3(unit_cube_x * face_center.y, face_center.y, unit_cube_y);
        } else if(face_center.z != 0.0f) {
            // Front or back face
            child_center_unit_cube = new Vector3(unit_cube_x * -face_center.z, unit_cube_y, face_center.z);
        }

        // Get center on sphere
        return CubeSphereMesh.map_point_to_sphere(child_center_unit_cube).normalized;
    }

    public void Split(Vector3 face_center) {
        children = new LodQuadTree[4];
        uint[] children_codes = NodeCodeUtil.generate_children(node_code);

        for(int i = 0; i < 4; i++) {
            uint[] coords = new uint[2];
            uint level = 0;
            NodeCodeUtil.decode(children_codes[i], ref level, ref coords);

            // Get center on sphere
            Vector3 child_center = GetNodeCenter(coords, level, face_center);

            children[i] = new LodQuadTree(children_codes[i], level + 1, child_center, this);
        }
    }

    public void Merge() {
        children = null;

        
    }

    // Node centers are being stored as if they were on the unit sphere
    public Vector3 node_center;
    public uint level;
    public uint node_code;

    public uint edge_smoothing_flags;
    public enum EDGE_SMOOTHING_FLAGS {
        LEFT = 1,
        RIGHT = 2,
        TOP = 4,
        BOTTOM = 8
    }

    public LodQuadTree[] children;
    public bool IsLeaf() { return children == null; }

    public LodQuadTree parent;
}

// This struct is passed to the compute shader with a list of changes which need to be recomputed
public struct LodBufferLayoutChanges {
    public uint new_node_code;
    public uint offset;
    public int face_number; // 0-5 for 6 sides of cube
    
    public static int get_size() {
        return sizeof(uint) * 2 + sizeof(int);
    }
}

public class LodBufferLayout {

    public LodBufferLayout(uint max_size) {
        this.max_size = max_size;
        layout = new List<LodBufferLayoutData>();

        first_free_index = 0;
    }

    public List<LodBufferLayoutChanges> UpdateIndices(List<LodQuadTree> new_nodes, List<LodBufferLayoutChanges> changes, int face_number = 0) {
        // First free unused indices
        foreach(LodBufferLayoutData layout_data in layout.ToList()) {
            if(!new_nodes.Exists(node => node.node_code == layout_data.node_code)) {
                layout.RemoveAll(data => data.node_code == layout_data.node_code);

                if(layout_data.position < first_free_index)
                    first_free_index = layout_data.position;
            }
        }

        // Then allocate new ones
        if(first_free_index == max_size)
            throw new Exception("Lod buffer layout is full");

        foreach(LodQuadTree node in new_nodes) {
            if(!layout.Exists(data => data.node_code == node.node_code)) {
                layout.Add(new LodBufferLayoutData {
                    node_code = node.node_code,
                    position = first_free_index,
                    face_id = (uint)face_number,
                    edge_smoothing_flags = node.edge_smoothing_flags
                });

                changes.Add(new LodBufferLayoutChanges {
                    new_node_code = node.node_code,
                    offset = first_free_index,
                    face_number = face_number
                });

                // Find next free index
                first_free_index++;
                while(first_free_index < max_size) {
                    if(!layout.Exists(data => data.position == first_free_index))
                        break;
                    first_free_index++;
                }
            }
        }

        return changes;
    }

    public struct LodBufferLayoutData {
        public uint node_code;
        public uint position;
        public uint face_id;
        // left, right, top, bottom
        public uint edge_smoothing_flags;
        public static int get_size() {
            return sizeof(uint) * 4;
        }
    }

    public List<LodBufferLayoutData> layout;
    public uint first_free_index;
    private uint max_size;
}

public class LodManager {

    public static readonly int NUM_QUAD_TREES = 6;
    public static readonly float LOD_DISTANCE_SCALE = 8.0f;
    private static readonly float ROOT_NODE_SPLIT_DISTANCE = Vector3.Distance(
        new Vector3(1.0f, 0.0f, 0.0f),
        new Vector3(1.0f, 1.0f, 1.0f).normalized
    );

    // THIS ORDER IS IMPORTANT! ALL VERTEX DATA IN THE BUFFERS
    // WILL BE ASSUMED TO BE IN THIS ORDER
    private static readonly Vector3[] node_centers = {
        new Vector3(0.0f, 0.0f, 1.0f),  // Back
        new Vector3(0.0f, 0.0f, -1.0f), // Front
        new Vector3(0.0f, 1.0f, 0.0f),  // Top
        new Vector3(0.0f, -1.0f, 0.0f), // Bottom
        new Vector3(1.0f, 0.0f, 0.0f),  // Right
        new Vector3(-1.0f, 0.0f, 0.0f)  // Left
    };

    public LodManager(ComputeShader compute_shader, uint index_count_per_instance, int vertex_count_per_instance) {
        lod_shader = compute_shader;
        lod_kernel_id = lod_shader.FindKernel("lod_kernel");

        this.vertex_count_per_instance = vertex_count_per_instance;
        this.resolution = (uint) Math.Round(Math.Sqrt(vertex_count_per_instance));
        
        // Setup quad trees and buffer layouts
        lod_quad_trees = new LodQuadTree[NUM_QUAD_TREES];
        lod_buffer_layouts = new LodBufferLayout[NUM_QUAD_TREES];

        for (int i = 0; i < NUM_QUAD_TREES; i++) {
            lod_quad_trees[i] = new LodQuadTree(LodQuadTree.ROOT_NODE_CODE, 0, node_centers[i], null);
            lod_buffer_layouts[i] = new LodBufferLayout((uint) LodQuadTree.MAX_NUM_NODES);
        }

        // Lod layout changes buffer
        lod_buffer_layout_buffer_changes = new ComputeBuffer(LodQuadTree.MAX_NUM_NODES * 6, LodBufferLayoutChanges.get_size());
        lod_shader.SetBuffer(lod_kernel_id, "lod_layout_changes", lod_buffer_layout_buffer_changes);

        // Lod layout buffer
        lod_layout_buffer = new ComputeBuffer(LodQuadTree.MAX_NUM_NODES * 6, LodBufferLayout.LodBufferLayoutData.get_size());
        lod_layout_buffer.SetData(get_full_lod_buffer_layout());

        // Setup shader constants
        lod_shader.SetInt("index_count_per_instance", (int) index_count_per_instance);
        lod_shader.SetInt("MAX_NUM_NODES", LodQuadTree.MAX_NUM_NODES);
    }

    private List<LodBufferLayout.LodBufferLayoutData> get_full_lod_buffer_layout() {
        List<LodBufferLayout.LodBufferLayoutData> ret = new List<LodBufferLayout.LodBufferLayoutData>();
        for(int i = 0; i < NUM_QUAD_TREES; i++) {
            ret.AddRange(lod_buffer_layouts[i].layout);
        }
        return ret;
    }

    private float s(float z, float alpha) {
        return 2.0f * z * Mathf.Tan(alpha / 2.0f);
    }

    private bool split_criterion(float distance, float node_size) {
        return node_size * LOD_DISTANCE_SCALE > distance;
    }

    private void merge_quad_trees(Vector3 camera_pos, float camera_fov, float sphere_radius, float min_radius) {
        for(int i = 0; i < NUM_QUAD_TREES; i++) {
            
            // Skip this tree if it's only a root node
            if(lod_quad_trees[i].children == null)
                continue;

            Queue<LodQuadTree> queue = new Queue<LodQuadTree>();
            queue.Enqueue(lod_quad_trees[i]);

            while(queue.Count > 0) {
                LodQuadTree node = queue.Dequeue();

                float node_size;
                if(node.parent == null) {
                    node_size = ROOT_NODE_SPLIT_DISTANCE * sphere_radius;
                } else {
                    node_size = Vector3.Distance(node.parent.node_center, node.node_center) * sphere_radius;
                }

                // Check if node should be merged by distance 
                // TODO: include resolution of tile and maybe s()
                float distance_to_node_center = Vector3.Distance(camera_pos, node.node_center * min_radius);
                if(!split_criterion(distance_to_node_center, node_size)) {
                    // merge
                    node.Merge();
                
                }else {
                    // Add children to queue
                    if(node.children != null) {
                        foreach(LodQuadTree child in node.children)
                            queue.Enqueue(child);
                    }
                }
            }
        }
    }

    private void split_quad_trees(Vector3 camera_pos, float camera_fov, float sphere_radius, float min_radius) {
        for(int i = 0; i < NUM_QUAD_TREES; i++) {
            Queue<LodQuadTree> queue = new Queue<LodQuadTree>();
            queue.Enqueue(lod_quad_trees[i]);

            while(queue.Count > 0) {
                LodQuadTree node = queue.Dequeue();

                if(node.children != null) {
                    // Add children to queue
                    foreach(LodQuadTree child in node.children)
                        queue.Enqueue(child);
                    continue;
                }

                float node_size;
                if(node.parent == null) {
                    node_size = ROOT_NODE_SPLIT_DISTANCE * sphere_radius;
                } else {
                    node_size = Vector3.Distance(node.parent.node_center, node.node_center) * sphere_radius;
                }

                // Check if node should be split by distance 
                // TODO: include resolution of tile and maybe s()
                float distance_to_node_center = Vector3.Distance(camera_pos, node.node_center * min_radius);
                if(split_criterion(distance_to_node_center, node_size)) {
                    if(node.level < LodQuadTree.MAX_LEVEL) {
                        // split
                        node.Split(node_centers[i]);

                        // Add children to queue
                        foreach(LodQuadTree child in node.children)
                            queue.Enqueue(child);
                    }
                }
            }
        }
    }

    private void close_holes(float sphere_radius, float min_radius, Vector3 camera_pos) {
        for(int i = 0; i < NUM_QUAD_TREES; i++) {
            List<LodQuadTree> nodes = lod_quad_trees[i].GetAllChildrenLeaves();
            List<LodQuadTree> all_nodes = lod_quad_trees[i].GetAllChildren();

            foreach(LodQuadTree node in nodes) {

                // Skip if it's the root node. Never has to be smoothed
                if(node.parent == null)
                    continue;

                // These refer to the parent node
                uint level = 0;
                uint[] coords = new uint[2];
                NodeCodeUtil.decode(node.parent.node_code, ref level, ref coords);
                // node size
                float node_size;
                if(node.parent.parent == null) {
                    node_size = ROOT_NODE_SPLIT_DISTANCE * sphere_radius;
                } else {
                    node_size = Vector3.Distance(node.parent.parent.node_center, node.parent.node_center) * sphere_radius;
                }

                // Left neighbour
                Vector3 left_neighbour_center = new Vector3(0.0f, 0.0f, 0.0f);
                if(coords[0] == 0) {
                    CoordinateHelpers.CenterCoordinates centerCoordinates = CoordinateHelpers.left_neighbour_map[i](coords, level);
                    left_neighbour_center = LodQuadTree.GetNodeCenter(centerCoordinates.coords, level, node_centers[centerCoordinates.face_number]);
                }else{
                    left_neighbour_center = LodQuadTree.GetNodeCenter(new uint[] { coords[0] - 1, coords[1] }, level, node_centers[i]);   
                }
                left_neighbour_center *= min_radius;
                if(!split_criterion(Vector3.Distance(camera_pos, left_neighbour_center), node_size)) {
                    node.edge_smoothing_flags |= (uint) LodQuadTree.EDGE_SMOOTHING_FLAGS.LEFT;
                } else {
                    node.edge_smoothing_flags &= ~(uint) LodQuadTree.EDGE_SMOOTHING_FLAGS.LEFT;
                }

                // Right neighbour
                Vector3 right_neighbour_center = new Vector3(0.0f, 0.0f, 0.0f);
                if(coords[0] == CoordinateHelpers.node_size(level) - 1) {
                    CoordinateHelpers.CenterCoordinates centerCoordinates = CoordinateHelpers.right_neighbour_map[i](coords, level);
                    right_neighbour_center = LodQuadTree.GetNodeCenter(centerCoordinates.coords, level, node_centers[centerCoordinates.face_number]);
                }else{
                    right_neighbour_center = LodQuadTree.GetNodeCenter(new uint[] { coords[0] + 1, coords[1] }, level, node_centers[i]);   
                }
                right_neighbour_center *= min_radius;
                if(!split_criterion(Vector3.Distance(camera_pos, right_neighbour_center), node_size)) {
                    node.edge_smoothing_flags |= (uint) LodQuadTree.EDGE_SMOOTHING_FLAGS.RIGHT;
                } else {
                    node.edge_smoothing_flags &= ~(uint) LodQuadTree.EDGE_SMOOTHING_FLAGS.RIGHT;
                }

                // Top neighbour
                Vector3 top_neighbour_center = new Vector3(0.0f, 0.0f, 0.0f);
                if(coords[1] == CoordinateHelpers.node_size(level) - 1) {
                    CoordinateHelpers.CenterCoordinates centerCoordinates = CoordinateHelpers.top_neighbour_map[i](coords, level);
                    top_neighbour_center = LodQuadTree.GetNodeCenter(centerCoordinates.coords, level, node_centers[centerCoordinates.face_number]);
                }else{
                    top_neighbour_center = LodQuadTree.GetNodeCenter(new uint[] { coords[0], coords[1] + 1 }, level, node_centers[i]);   
                }
                top_neighbour_center *= min_radius;
                if(!split_criterion(Vector3.Distance(camera_pos, top_neighbour_center), node_size)) {
                    node.edge_smoothing_flags |= (uint) LodQuadTree.EDGE_SMOOTHING_FLAGS.TOP;
                } else {
                    node.edge_smoothing_flags &= ~(uint) LodQuadTree.EDGE_SMOOTHING_FLAGS.TOP;
                }

                // Bottom neighbour
                Vector3 bottom_neighbour_center = new Vector3(0.0f, 0.0f, 0.0f);
                if(coords[1] == 0) {
                    CoordinateHelpers.CenterCoordinates centerCoordinates = CoordinateHelpers.bottom_neighbour_map[i](coords, level);
                    bottom_neighbour_center = LodQuadTree.GetNodeCenter(centerCoordinates.coords, level, node_centers[centerCoordinates.face_number]);
                }else{
                    bottom_neighbour_center = LodQuadTree.GetNodeCenter(new uint[] { coords[0], coords[1] - 1 }, level, node_centers[i]);   
                }
                bottom_neighbour_center *= min_radius;
                if(!split_criterion(Vector3.Distance(camera_pos, bottom_neighbour_center), node_size)) {
                    node.edge_smoothing_flags |= (uint) LodQuadTree.EDGE_SMOOTHING_FLAGS.BOTTOM;
                } else {
                    node.edge_smoothing_flags &= ~(uint) LodQuadTree.EDGE_SMOOTHING_FLAGS.BOTTOM;
                }
            }
        }
    }

    // returns true if lod kernel as run
    public bool run_lod_kernels(Camera camera, Transform planet_transform, float sphere_radius, float min_radius) {

        // Camera position in planet coordinate space
        Vector3 camera_pos = camera.transform.position;
        camera_pos += planet_transform.position;
        camera_pos = planet_transform.rotation * camera_pos;

        // Split/merge quad trees
        // merge first then split to limit buffer fragmentation
        merge_quad_trees(camera_pos, camera.fieldOfView, sphere_radius, min_radius);
        split_quad_trees(camera_pos, camera.fieldOfView, sphere_radius, min_radius);
        close_holes(sphere_radius, min_radius, camera_pos);

        // Update lod buffer layouts
        List<LodBufferLayoutChanges> changes = new List<LodBufferLayoutChanges>();
        for(int i = 0; i < NUM_QUAD_TREES; i++) {
            List<LodQuadTree> indices = lod_quad_trees[i].GetAllChildrenLeaves();
            lod_buffer_layouts[i].UpdateIndices(indices, changes, i);
        }

        if(changes.Count == 0)
            return false;

        // If changes occured update lod layout buffer (TODO: move to compute shader)
        lod_layout_buffer.SetData(get_full_lod_buffer_layout());

        // Update compute layout changes buffer
        lod_buffer_layout_buffer_changes.SetData(changes);

        // Set constants
        lod_shader.SetFloat("sphere_radius", sphere_radius);
        lod_shader.SetInt("num_changes", changes.Count);

        // Dispatch
        uint thread_x, thread_y, thread_z;
        lod_shader.GetKernelThreadGroupSizes(lod_kernel_id, out thread_x, out thread_y, out thread_z);
        thread_x = (uint) Mathf.CeilToInt(changes.Count * vertex_count_per_instance / (float)thread_x);
        lod_shader.Dispatch(lod_kernel_id, (int) thread_x, 1, 1);

        return true;
    }

    public int get_node_count() { return get_full_lod_buffer_layout().Count; }

    ComputeShader lod_shader;
    int lod_kernel_id;
    public int get_kernel_id() { return lod_kernel_id; }

    int vertex_count_per_instance;
    uint resolution;

    public LodQuadTree[] lod_quad_trees;

    public LodBufferLayout[] lod_buffer_layouts;
    
    // Need to be double buffered so compute shader can check for changes
    private ComputeBuffer lod_buffer_layout_buffer_changes;
    private ComputeBuffer lod_layout_buffer;
    public ComputeBuffer get_lod_layout_buffer() { return lod_layout_buffer; }
}
