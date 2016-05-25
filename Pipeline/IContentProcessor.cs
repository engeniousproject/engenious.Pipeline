using System;
using System.Collections.Generic;

namespace engenious.Content.Pipeline
{
    public interface IContentProcessor
    {
        ProcessorSettings Settings{get;set;}
        object Process(object input, string filename, ContentProcessorContext context);
    }
}

