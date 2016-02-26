using System;
using engenious.Content.Pipeline;

namespace engenious.Pipeline
{
    [ContentProcessor(DisplayName = "Passtrough Processor")]
    public class PassthroughProcessor : ContentProcessor<object,object>
    {
        public PassthroughProcessor()
        {
        }

        #region implemented abstract members of ContentProcessor

        public override object Process(object input, ContentProcessorContext context)
        {
            return input;
        }

        #endregion
    }
}

