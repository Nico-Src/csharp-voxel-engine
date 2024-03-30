using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;

namespace Engine.Core
{
    public class Size
    {
        public float Width { get; set; }
        public float Height { get; set; }
        public float Depth { get; set; }

        /// <summary>
        /// Creates a size
        /// </summary>
        /// <param name="width"> width of the object </param>
        /// <param name="height"> height of the object </param>
        /// <param name="depth"> depth of the object</param>
        public Size(float width, float height, float depth)
        {
            this.Width = width;
            this.Height = height;
            this.Depth = depth;
        }

        /// <summary>
        /// Creates a square size
        /// </summary>
        /// <param name="width"> width (and depth) of the object </param>
        /// <param name="height"> height of the object </param>
        public Size(float width, float height)
        {
            this.Width = width;
            this.Height = height;
            this.Depth = width;
        }
    }
}
