using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using engenious.Content.Pipeline;

namespace engenious.Pipeline.Pipeline.Editors
{
    public interface IContentEditor<TInput,TOutput> : IContentEditor
    {
        void Open(TInput importerInput, TOutput processorOutput);
    }

    public interface IContentEditor
    {
        Control MainControl { get; }
    }
}
