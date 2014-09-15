using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace S7.Core
{
    public interface IRenderable
    {
        Rectangle BoundingBox { get; set; }
        Point Location { get; }
        Byte Layer { get; }

    }
}
