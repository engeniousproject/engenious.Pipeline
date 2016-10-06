using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace engenious.Pipeline.Pipeline.Editors
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ContentEditorAttribute : Attribute
    {
        public ContentEditorAttribute(params string[] supportedFileExtensions)
        {
            SupportedFileExtensions = supportedFileExtensions;
        }
        public string[] SupportedFileExtensions { get; private set; }
    }
}
