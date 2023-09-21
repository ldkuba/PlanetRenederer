

using System;

public class CoordinateHelpers {
    private enum Face : int {
        Back = 0,
        Front = 1,
        Top = 2,
        Bottom = 3,
        Right = 4,
        Left = 5
    }
    
    public struct CenterCoordinates {
        public uint[] coords;
        public int face_number;
    }

    public static uint node_size(uint level) {
        return (uint) Math.Pow(2, level);
    }

    // Calculates node-coordinates of left neighbours
    public static readonly Func<uint[], uint, CenterCoordinates>[] left_neighbour_map = {
        // Back -> right
        (uint[] coords, uint level) => {
            return new CenterCoordinates {
                coords = new uint[] { node_size(level) - 1, coords[1] },
                face_number = (int) Face.Right
            };
        },
        // Front -> left
        (uint[] coords, uint level) => {
            return new CenterCoordinates {
                coords = new uint[] { node_size(level) - 1, coords[1] },
                face_number = (int) Face.Left
            };
        },
        // Top -> left
        (uint[] coords, uint level) => {
            return new CenterCoordinates {
                coords = new uint[] { node_size(level) - 1 - coords[1], node_size(level) - 1 },
                face_number = (int) Face.Left
            };
        },
        // Bottom -> right
        (uint[] coords, uint level) => {
            return new CenterCoordinates {
                coords = new uint[] { coords[1], 0 },
                face_number = (int) Face.Right
            };
        },
        // Right -> front
        (uint[] coords, uint level) => {
            return new CenterCoordinates {
                coords = new uint[] { node_size(level) - 1, coords[1] },
                face_number = (int) Face.Front
            };
        },
        // Left -> left/back
        (uint[] coords, uint level) => {
            return new CenterCoordinates {
                coords = new uint[] { node_size(level) - 1, coords[1] },
                face_number = (int) Face.Back
            };
        }
    };

    public static readonly Func<uint[], uint, CenterCoordinates>[] right_neighbour_map = {
        // Back -> back/left
        (uint[] coords, uint level) => {
            return new CenterCoordinates {
                coords = new uint[] { 0, coords[1] },
                face_number = (int) Face.Left
            };
        },
        // Front -> front/right
        (uint[] coords, uint level) => {
            return new CenterCoordinates {
                coords = new uint[] { 0, coords[1] },
                face_number = (int) Face.Right
            };
        },
        // Top -> top/right
        (uint[] coords, uint level) => {
            return new CenterCoordinates {
                coords = new uint[] { coords[1], node_size(level) - 1 },
                face_number = (int) Face.Right
            };
        },
        // Bottom -> left
        (uint[] coords, uint level) => {
            return new CenterCoordinates {
                coords = new uint[] { node_size(level) - coords[1], 0 },
                face_number = (int) Face.Left
            };
        },
        // Right -> back
        (uint[] coords, uint level) => {
            return new CenterCoordinates {
                coords = new uint[] { 0, coords[1] },
                face_number = (int) Face.Back
            };
        },
        // Left -> front
        (uint[] coords, uint level) => {
            return new CenterCoordinates {
                coords = new uint[] { 0, coords[1] },
                face_number = (int) Face.Front
            };
        }
    };

    public static readonly Func<uint[], uint, CenterCoordinates>[] top_neighbour_map = {
        // Back -> top
        (uint[] coords, uint level) => {
            return new CenterCoordinates {
                coords = new uint[] { coords[0], node_size(level) - 1 },
                face_number = (int) Face.Top
            };
        },
        // Front -> top
        (uint[] coords, uint level) => {
            return new CenterCoordinates {
                coords = new uint[] { coords[0], 0 },
                face_number = (int) Face.Top
            };
        },
        // Top -> back
        (uint[] coords, uint level) => {
            return new CenterCoordinates {
                coords = new uint[] { node_size(level) - 1 - coords[0], node_size(level) - 1 },
                face_number = (int) Face.Back
            };
        },
        // Bottom -> back
        (uint[] coords, uint level) => {
            return new CenterCoordinates {
                coords = new uint[] { coords[0], 0 },
                face_number = (int) Face.Back
            };
        },
        // Right -> top
        (uint[] coords, uint level) => {
            return new CenterCoordinates {
                coords = new uint[] { node_size(level) - 1, coords[0] },
                face_number = (int) Face.Top
            };
        },
        // Left -> top
        (uint[] coords, uint level) => {
            return new CenterCoordinates {
                coords = new uint[] { 0, node_size(level) - 1 - coords[0] },
                face_number = (int) Face.Top
            };
        }
    };

    public static readonly Func<uint[], uint, CenterCoordinates>[] bottom_neighbour_map = {
        // Back -> bottom
        (uint[] coords, uint level) => {
            return new CenterCoordinates {
                coords = new uint[] { coords[0], node_size(level) - 1 },
                face_number = (int) Face.Bottom
            };
        },
        // Front -> bottom
        (uint[] coords, uint level) => {
            return new CenterCoordinates {
                coords = new uint[] { node_size(level) - 1 - coords[0], 0 },
                face_number = (int) Face.Bottom
            };
        },
        // Top -> front
        (uint[] coords, uint level) => {
            return new CenterCoordinates {
                coords = new uint[] { coords[0], node_size(level) - 1 },
                face_number = (int) Face.Front
            };
        },
        // Bottom -> front
        (uint[] coords, uint level) => {
            return new CenterCoordinates {
                coords = new uint[] { node_size(level) - 1 - coords[0], 0 },
                face_number = (int) Face.Front
            };
        },
        // Right -> bottom
        (uint[] coords, uint level) => {
            return new CenterCoordinates {
                coords = new uint[] { 0, coords[0] },
                face_number = (int) Face.Bottom
            };
        },
        // Left -> bottom
        (uint[] coords, uint level) => {
            return new CenterCoordinates {
                coords = new uint[] { node_size(level) - 1, node_size(level) - 1 - coords[0] },
                face_number = (int) Face.Bottom
            };
        }
    };
}
