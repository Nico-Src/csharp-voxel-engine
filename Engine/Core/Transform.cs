using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Mathematics;

namespace Engine.Core
{
    public class Transform
    {
        private Vector3 _position;
        private Vector3 _rotation;
        private Vector3 _scale;
        
        /// <summary>
        /// Position of the object
        /// </summary>
        public Vector3 Position {
            get
            {
                return this.Parent == null ? _position : this.Parent.Position + _position;
            }

            set
            {
                _position = value;
            } 
        }

        /// <summary>
        /// Rotation of the object
        /// </summary>
        public Vector3 Rotation
        {
            get
            {
                return this.Parent == null ? _rotation : this.Parent.Rotation + _rotation;
            }

            set
            {
                _rotation = value;
            }
        }

        /// <summary>
        /// Scale of the object
        /// </summary>
        public Vector3 Scale
        {
            get
            {
                return this.Parent == null ? _scale : this.Parent.Scale + _scale;
            }

            set
            {
                _scale = value;
            }
        }

        public Vector3 LocalPosition => _position;

        public Vector3 LocalRotation => _rotation;

        public Vector3 LocalScale => _scale;

        /// <summary>
        /// Parent of the object (will be used to calculate the final transform)
        /// Position, Rotation and Scale will be relative to the parent
        /// </summary>
        public Transform Parent { get; set; }

        /// <summary>
        /// Creates a transform (position, rotation, scale)
        /// </summary>
        /// <param name="rotation"> rotation in degrees </param>
        public Transform(Vector3 position, Vector3 rotation, Vector3 scale)
        {
            this.Position = position;
            this.Rotation = rotation;
            this.Scale = scale;
        }

        /// <summary>
        /// Creates a transform (position, rotation) [scale = 1]
        /// </summary>
        /// <param name="rotation"> rotation in degrees </param>
        public Transform(Vector3 position, Vector3 rotation)
        {
            this.Position = position;
            this.Rotation = rotation;
            this.Scale = Vector3.One;
        }

        /// <summary>
        /// Creates a transform (position) [scale = 1, rotation = 0]
        /// </summary>
        public Transform(Vector3 position)
        {
            this.Position = position;
            this.Rotation = Vector3.Zero;
            this.Scale = Vector3.One;
        }

        /// <summary>
        /// Creates a default transform [position = 0, scale = 1, rotation = 0]
        /// </summary>
        public Transform()
        {
            this.Position = Vector3.Zero;
            this.Rotation = Vector3.Zero;
            this.Scale = Vector3.One;
        }
    }
}
