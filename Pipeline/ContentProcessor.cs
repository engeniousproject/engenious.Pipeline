using System;

namespace engenious.Content.Pipeline
{
    public abstract class ContentProcessor<TInput,TOutput> : IContentProcessor
    {
        public ContentProcessor()
        {
        }
        private static Type importType = null;
        public static Type ImportType
        {
            get
            {
                if (importType == null)
                    importType = typeof(TInput);
                return importType;
            }
        }
        private static Type exportType = null;
        public static Type ExportType
        {
            get
            {
                if (exportType == null)
                    exportType = typeof(TOutput);
                return exportType;
            }
        }

        public abstract TOutput Process(TInput input, ContentProcessorContext context);

        object IContentProcessor.Process(object input, ContentProcessorContext context)
        {
            return Process((TInput)input, context);
        }
    }
}

