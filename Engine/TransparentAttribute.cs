using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
    public class TransparentAttribute : Attribute
    {
        public bool Transparent { get; protected set; }
        
        public TransparentAttribute(bool transparent)
        {
            this.Transparent = transparent;
        }
    }
}
