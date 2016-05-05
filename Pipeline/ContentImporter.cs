using System;

namespace engenious.Content.Pipeline
{
    public abstract class ContentImporter<T> : IContentImporter
    {
        public ContentImporter()
        {
        }
        private static Type exportType = null;
        public static Type ExportType
        {
            get
            {
                if (exportType == null)
                    exportType = typeof(T);
                return exportType;
            }
        }

        public abstract T Import(string filename, ContentImporterContext context);

        object IContentImporter.Import(string filename, ContentImporterContext context)
        {
            return Import(filename, context);
        }

    }
}

