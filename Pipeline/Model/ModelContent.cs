using System;
using System.Collections.Generic;

namespace engenious.Pipeline
{
    public class ModelContent
    {
        public ModelContent()
        {
        }

        internal MeshContent[] Meshes{ get; set; }

        internal NodeContent RootNode{ get; set; }

        internal List<NodeContent> Nodes{ get; set; }

        internal List<AnimationContent> Animations{ get; set; }

    }
}

