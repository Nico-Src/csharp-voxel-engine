using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Numerics;

namespace Engine
{

    public enum VoxelType
    {
        Air = 0,
        Dirt_1,
        Dirt_2,
        Dirt_3,
        Grass_1,
        Grass_2,
        Grass_3,
        Stone_1,
        Stone_2,
        Stone_3,
        Iron_Ore,
        Light,
        Red,
        Blue,
        None = 255
    }
    
    public class Voxel
    {
        public byte Id { get; set; }
        public Vector3 Color { get; set; }
        public VoxelType Type { get; set; }

        public Voxel(byte id, Vector3 color)
        {
            this.Id = id;
            this.Type = (VoxelType)this.Id;
            this.Color = color;
        }
    }
}
