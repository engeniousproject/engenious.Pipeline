using System;
using System.Collections.Generic;

namespace engenious.Content.Pipeline
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ContentImporterAttribute : System.Attribute
    {
        private List<string> fileExtensions;


        public ContentImporterAttribute(params string[] fileExtensions)
        {
            this.fileExtensions = new List<string>();
            foreach (string ext in fileExtensions)
                this.fileExtensions.Add(ext);
        }

        public IEnumerable<string> FileExtensions { get { return fileExtensions; } }

        public string DisplayName{ get; set; }

        public string DefaultProcessor { get; set; }

        public bool CacheImportedData { get; set; }
    }

}

