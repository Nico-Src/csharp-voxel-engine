using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;

namespace Engine.Core
{
    public class Vector3I
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }

        public Vector3I(int x, int y, int z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public Vector3I(System.Numerics.Vector3 vec)
        {
            this.X = (int)Math.Floor(vec.X);
            this.Y = (int)Math.Floor(vec.Y);
            this.Z = (int)Math.Floor(vec.Z);
        }

        public Vector3I(Vector3 vec)
        {
            this.X = (int)Math.Floor(vec.X);
            this.Y = (int)Math.Floor(vec.Y);
            this.Z = (int)Math.Floor(vec.Z);
        }

        /// <summary>
        /// Convert to Vector3 (System.Numerics)
        /// </summary>
        /// <returns> System.Numerics.Vector3 </returns>
        public System.Numerics.Vector3 ToVector3()
        {
            return new System.Numerics.Vector3(this.X, this.Y, this.Z);
        }

        /// <summary>
        /// Convert to Vector3 (OpenTK.Mathematics)
        /// </summary>
        /// <returns> OpenTK.Mathematics </returns>
        public Vector3 ToOTKVector3()
        {
            return new Vector3(this.X, this.Y, this.Z);
        }
    }
}
