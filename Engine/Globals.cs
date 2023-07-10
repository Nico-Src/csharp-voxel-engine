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
        public static int RENDER_DISTANCE = 4;
        public static int CHUNK_WIDTH = 16;
        public static int CHUNK_HEIGHT = 128;
        public static int MAX_RANGE = int.MaxValue / CHUNK_WIDTH;
        public static int MIN_RANGE = int.MinValue / CHUNK_WIDTH;
        public static int ATLAS_SIZE_IN_VOXELS = 16;
        public static float NORMALIZED_ATLAS_SIZE
        {
            get { return 1f / (float)ATLAS_SIZE_IN_VOXELS; }
        }

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

        public static Dictionary<VoxelType, int[]> BLOCK_TEXTURES = new Dictionary<VoxelType, int[]>()
        {
            { VoxelType.Air,      new int[6]{-1, -1, -1, -1, -1, -1 } },
            { VoxelType.Dirt,     new int[6]{ 2,  2,  2,  2,  2,  2 } },
            { VoxelType.Grass,    new int[6]{ 3,  3,  0,  2,  3,  3 } },
            { VoxelType.Stone,    new int[6]{ 1,  1,  1,  1,  1,  1} },
            { VoxelType.Iron_Ore, new int[6]{ 32, 32, 32, 32, 32, 32 } },
            { VoxelType.Wood,     new int[6]{ 20, 20, 21, 21, 20, 20 } },
            { VoxelType.Leaves,   new int[6]{ 52, 52, 52, 52, 52, 52 } },
            { VoxelType.Glass,    new int[6]{ 49, 49, 49, 49, 49, 49 } },
        };

        public static Dictionary<VoxelType, Voxel> VOXEL_TYPES = new Dictionary<VoxelType, Voxel>()
        {
            { VoxelType.Air,       new Voxel(0, true) },
            { VoxelType.Dirt,      new Voxel(1, false) },
            { VoxelType.Grass,     new Voxel(2, false) },
            { VoxelType.Stone,     new Voxel(3, false) },
            { VoxelType.Iron_Ore,  new Voxel(4, false) },
            { VoxelType.Wood,      new Voxel(5, false) },
            { VoxelType.Leaves,    new Voxel(6, true) },
            { VoxelType.Glass,     new Voxel(7, true) },
        };
    }
}
