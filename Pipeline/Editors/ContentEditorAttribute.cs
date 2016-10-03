using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace engenious.Pipeline.Pipeline.Editors
{
    [AttributeUsage(AttributeTargets.Class)]
    class ContentEditorAttribute : Attribute
    {
        public ContentEditorAttribute(string[] supportedFileExtensions,KeyValuePair<Type,Type>[] supportedImporterProcessorOutputs)
        {
            SupportedFileExtensions = supportedFileExtensions;
            SupportedImporterProcessorOutputs = supportedImporterProcessorOutputs;
        }
        public string[] SupportedFileExtensions { get; private set; }
        public KeyValuePair<Type,Type>[] SupportedImporterProcessorOutputs { get; private set; }
    }
}
