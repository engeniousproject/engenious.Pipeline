using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace engenious.Pipeline.Pipeline.Editors
{
    public class ContentEditorWrapper//TODO: rename
    {
        public ContentEditorWrapper(IContentEditor editor,Action<object,object> open)
        {
            Editor = editor;
            Open = open;
        }
        public IContentEditor Editor { get; private set; }
        public Action<object, object> Open { get; private set; }
    }
}
