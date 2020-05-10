using System;
using System.Collections.Generic;
using engenious.Graphics;
using OpenTK;
using OpenTK.Graphics;
using System.Threading;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using engenious.Helper;
using engenious.Pipeline;
using OpenTK.Platform;

namespace engenious.Content.Pipeline
{
    public class ContentProcessorContext : ContentContext
    {
        static ContentProcessorContext()
        {
            
            //BaseWindow = new GameWindow();
            //_window = new NativeWindow(100,100,"Test",GameWindowFlags.Default, GraphicsMode.Default, DisplayDevice.Default);

        }

        public ContentProcessorContext(SynchronizationContext syncContext, string workingDirectory = "")
            : this(syncContext, null, null, workingDirectory)
        {
            
        }
        public ContentProcessorContext(SynchronizationContext syncContext, IRenderingSurface surface , GraphicsDevice graphicsDevice, string workingDirectory = "")
            : base(workingDirectory)
        {
            SyncContext = syncContext;
            
            if (surface == null && graphicsDevice != null || surface != null && graphicsDevice == null)
                throw new ArgumentException($"Either both of {nameof(surface)} and {nameof(graphicsDevice._context)} must be set or not set.");

            IGraphicsContext context;
            IWindowInfo windowInfo;
            if (surface == null)
            {
                var window = new GameWindow(100, 100);
                windowInfo = window.WindowInfo;
                context = window.Context;
            }
            else
            {
                context = graphicsDevice._context;
                windowInfo = surface.WindowInfo;
            }

            context.MakeCurrent(windowInfo);
            (context as IGraphicsContextInternal)?.LoadAll();

            ThreadingHelper.Initialize(null, null, 0, 0, GraphicsContextFlags.Debug);

            GraphicsDevice = new GraphicsDevice(null, ThreadingHelper.Context);
        }

        public SynchronizationContext SyncContext { get; private set; }
        public GraphicsDevice GraphicsDevice { get; private set; }

        public List<SourceFile> SourceFiles { get; set; }


        public override void Dispose()
        {
            //GraphicsDevice.Dispose();
            //Window.Dispose();
        }
    }
}