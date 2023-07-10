using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace Engine
{

    public enum VoxelType
    {
        Air = 0,
        Dirt = 1,
        Grass = 2,
        Stone = 3,
        Iron_Ore = 4,
        Wood = 5,
        Leaves = 6,
        Glass = 7,
        None = 255
    }
    
    public class Voxel
    {
        public byte Id { get; set; }
        public string Name { get; set; }
        public bool Transparent { get; set; }

        public Voxel(byte id, bool transparent)
        {
            this.Id = id;
            this.Transparent = transparent;
        }
    }
}
