using System;
using System.Collections.Generic;
using engenious.Graphics;
using System.Threading;
using engenious.Helper;
using engenious.Pipeline;
using Mono.Cecil;
using OpenToolkit.Windowing.Common;
using OpenToolkit.Windowing.Desktop;

namespace engenious.Content.Pipeline
{
    public class ContentProcessorContext : ContentContext
    {
        static ContentProcessorContext()
        {
            
            //BaseWindow = new GameWindow();
            //_window = new NativeWindow(100,100,"Test",GameWindowFlags.Default, GraphicsMode.Default, DisplayDevice.Default);

        }

        public ContentProcessorContext(SynchronizationContext syncContext, AssemblyDefinition createdContentAssembly, string workingDirectory = "")
            : this(syncContext, createdContentAssembly, null, null, workingDirectory)
        {
            
        }
        public ContentProcessorContext(SynchronizationContext syncContext, AssemblyDefinition createdContentAssembly, IRenderingSurface surface , GraphicsDevice graphicsDevice, string workingDirectory = "")
            : base(workingDirectory)
        {
            SyncContext = syncContext;
            CreatedContentAssembly = createdContentAssembly;

            if (surface == null && graphicsDevice != null || surface != null && graphicsDevice == null)
                throw new ArgumentException($"Either both of {nameof(surface)} and {nameof(graphicsDevice._context)} must be set or not set.");

            IGraphicsContext context;
            if (surface == null)
            {
                var nativeWindowSettings = new NativeWindowSettings();
                nativeWindowSettings.StartVisible = false;
                var window = new GameWindow(GameWindowSettings.Default, nativeWindowSettings);
                context = window.Context;
            }
            else
            {
                context = graphicsDevice._context;
            }

            context.MakeCurrent();

            ThreadingHelper.Initialize(null, null);

            GraphicsDevice = new GraphicsDevice(null, ThreadingHelper.Context);
        }

        public SynchronizationContext SyncContext { get; }
        public GraphicsDevice GraphicsDevice { get; }

        public AssemblyDefinition CreatedContentAssembly { get; }


        public override void Dispose()
        {
            //GraphicsDevice.Dispose();
            //Window.Dispose();
        }
    }
}