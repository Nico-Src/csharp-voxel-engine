using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;

namespace Engine
{
    public static class Globals
    {
        public static int RENDER_DISTANCE = 12;
        public static int CHUNK_WIDTH = 16;
        public static int CHUNK_HEIGHT = 128;
        public static int MAX_RANGE = int.MaxValue / CHUNK_WIDTH;
        public static int MIN_RANGE = int.MinValue / CHUNK_WIDTH;

        public static float RAY_STEP = 0.25f;

        public static Vector3[] CUBE_VERTICES = {
            new Vector3(0.0f, 0.0f, 0.0f), // 0
            new Vector3(1.0f, 0.0f, 0.0f), // 1
            new Vector3(1.0f, 1.0f, 0.0f), // 2
            new Vector3(0.0f, 1.0f, 0.0f), // 3
            new Vector3(0.0f, 0.0f, 1.0f), // 4
            new Vector3(1.0f, 0.0f, 1.0f), // 5
            new Vector3(1.0f, 1.0f, 1.0f), // 6
            new Vector3(0.0f, 1.0f, 1.0f)  // 7
        };

        public static int[,] CUBE_TRIANGLES = new int[6, 6]
        {
            {0, 3, 1, 1, 3, 2}, // Back Face
		    {5, 6, 4, 4, 6, 7}, // Front Face
		    {3, 7, 2, 2, 7, 6}, // Top Face
		    {1, 5, 0, 0, 5, 4}, // Bottom Face
		    {4, 7, 0, 0, 7, 3}, // Left Face
		    {1, 2, 5, 5, 2, 6} // Right Face
        };

        public static Vector2[] CUBE_UVS = new Vector2[6]
        {
            new Vector2 (0.0f, 0.0f), // 0
            new Vector2 (0.0f, 1.0f), // 1
            new Vector2 (1.0f, 0.0f), // 2
            new Vector2 (1.0f, 0.0f), // 3
            new Vector2 (0.0f, 1.0f), // 4
            new Vector2 (1.0f, 1.0f)  // 5
        };

        public static Dictionary<VoxelType, Voxel> VOXEL_TYPES = new Dictionary<VoxelType, Voxel>()
        {
            { VoxelType.Air,       new Voxel(0, new System.Numerics.Vector3(0,0,0)) },            // Black
            { VoxelType.Dirt_1,      new Voxel(1, new System.Numerics.Vector3(0.545f, 0.271f, 0.075f)) }, // Brown
            { VoxelType.Dirt_2,      new Voxel(1, new System.Numerics.Vector3(0.595f, 0.321f, 0.125f)) }, // Brown
            { VoxelType.Dirt_3,      new Voxel(1, new System.Numerics.Vector3(0.495f, 0.221f, 0.025f)) }, // Brown
            { VoxelType.Grass_1,     new Voxel(2, new System.Numerics.Vector3(0, 0.502f, 0)) },     // Green
            { VoxelType.Grass_2,     new Voxel(2, new System.Numerics.Vector3(0.05f, 0.552f, 0.05f)) },     // Green
            { VoxelType.Grass_3,     new Voxel(2, new System.Numerics.Vector3(0.1f, 0.602f, 0.1f)) },     // Green
            { VoxelType.Stone_1,     new Voxel(3, new System.Numerics.Vector3(0.753f, 0.753f, 0.753f)) }, // Silver
            { VoxelType.Stone_2,     new Voxel(3, new System.Numerics.Vector3(0.803f, 0.803f, 0.803f)) }, // Silver
            { VoxelType.Stone_3,     new Voxel(3, new System.Numerics.Vector3(0.703f, 0.703f, 0.703f)) }, // Silver
            { VoxelType.Iron_Ore,  new Voxel(4, new System.Numerics.Vector3(0.663f, 0.663f, 0.663f)) }, // Dark Gray
            { VoxelType.Red,  new Voxel(4, new System.Numerics.Vector3(1f, 0f, 0f)) }, // Dark Gray
            { VoxelType.Blue,  new Voxel(4, new System.Numerics.Vector3(0f, 0f, 1f)) }, // Dark Gray
            { VoxelType.Light,  new Voxel(4, new System.Numerics.Vector3(1f, 1f, 1f)) }, // Dark Gray
        };
    }
}
