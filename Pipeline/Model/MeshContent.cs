using System;
using engenious.Graphics;

namespace engenious.Pipeline
{
    internal class MeshContent
    {
        public MeshContent()
        {
        }

        public int PrimitiveCount{ get; set; }

        public VertexPositionNormalTexture[] Vertices{ get; set; }
    }
}

