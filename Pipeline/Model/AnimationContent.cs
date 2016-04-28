﻿using System;
using System.Collections.Generic;

namespace engenious.Pipeline
{
    internal class AnimationContent
    {
        public float Time{ get; set; }

        public float MaxTime{ get; set; }

        public List<AnimationNodeContent> Channels{ get; set; }
    }
}

