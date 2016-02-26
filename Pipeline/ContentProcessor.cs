using System;

namespace engenious.Content.Pipeline
{
    public abstract class ContentProcessor<TInput,TOutput> : IContentProcessor
    {
        public ContentProcessor()
        {
        }


        public abstract TOutput Process(TInput input, ContentProcessorContext context);

        object IContentProcessor.Process(object input, ContentProcessorContext context)
        {
            return Process((TInput)input, context);
        }
    }
}

