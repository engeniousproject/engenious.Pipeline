using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace engenious.Content.Pipeline
{
    public delegate void BuildMessageDel(object sender,BuildMessageEventArgs e);
    public interface IContentContext : IDisposable
    {
        List<string> Dependencies { get; }

        void RaiseBuildMessage(string filename,string message, BuildMessageEventArgs.BuildMessageType messageType);

        event BuildMessageDel BuildMessage;
    }
}
