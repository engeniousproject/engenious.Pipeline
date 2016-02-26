using System;

namespace engenious.Content.Pipeline
{
    public interface IContentProcessor
    {

        object Process(object input, ContentProcessorContext context);

    }
}

