using System;

namespace engenious.Content.Pipeline
{
    public interface IContentProcessor
    {

        object Process(object input, string filename, ContentProcessorContext context);

    }
}

