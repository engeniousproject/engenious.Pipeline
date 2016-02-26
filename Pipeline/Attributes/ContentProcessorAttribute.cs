using System;
using System.Collections.Generic;

namespace engenious.Content.Pipeline
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ContentProcessorAttribute : Attribute
    {
        public virtual string DisplayName{ get; set; }
    }
}

