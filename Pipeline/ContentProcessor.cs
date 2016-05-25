using System;
using System.Collections.Generic;

namespace engenious.Content.Pipeline
{
    public abstract class ContentProcessor<TInput,TOutput,TSettings> : IContentProcessor where TSettings:ProcessorSettings,new()
    {
        public ContentProcessor()
        {
            settings = new TSettings();
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

        /*public TSettings Settings
        {
            get;set;
        }*/
        [System.Xml.Serialization.XmlIgnore()]
        protected TSettings settings;

        public ProcessorSettings Settings{
            get
            {
                return settings;
            }
            set
            {
                settings = (TSettings)value;
            }
        }

        public abstract TOutput Process(TInput input,string filename, ContentProcessorContext context);

        object IContentProcessor.Process(object input, string filename, ContentProcessorContext context)
        {
            return Process((TInput)input,filename, context);
        }
    }
    public abstract class ContentProcessor<TInput,TOutput> : ContentProcessor<TInput,TOutput,ProcessorSettings>
    {
        public ContentProcessor()
        {
            Settings = null;
        }
    }
}

