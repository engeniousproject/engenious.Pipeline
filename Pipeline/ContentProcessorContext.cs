using System;
using engenious.Graphics;
using OpenTK;
using OpenTK.Graphics;

namespace engenious.Content.Pipeline
{
    public class ContentProcessorContext:IDisposable
    {
        private GameWindow window;

        public ContentProcessorContext(string workingDirectory = "")
        {
            this.WorkingDirectory = workingDirectory;
            window = new GameWindow();
            ThreadingHelper.Initialize(window.WindowInfo);
            GraphicsDevice = new GraphicsDevice(window.Context);

        }

        public GraphicsDevice GraphicsDevice{ get; private set; }

        public string WorkingDirectory{ get; private set; }

        public void Dispose()
        {
            window.Dispose();
        }
    }
}