using System;
using System.Collections.Generic;
using System.ComponentModel;
using engenious.Graphics;
using System.Threading;
using engenious.Helper;
using engenious.Pipeline;
using Mono.Cecil;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace engenious.Content.Pipeline
{
    public class ContentProcessorContext : ContentContext
    {
        static ContentProcessorContext()
        {
            
            //BaseWindow = new GameWindow();
            //_window = new NativeWindow(100,100,"Test",GameWindowFlags.Default, GraphicsMode.Default, DisplayDevice.Default);

        }

        public ContentProcessorContext(SynchronizationContext syncContext, AssemblyCreatedContent createdContentAssembly, Guid buildId, string contentDirectory, string workingDirectory = "")
            : this(syncContext, createdContentAssembly, null, null, buildId, contentDirectory, workingDirectory)
        {
            
        }

        private class ContentProcessorControl : IRenderingSurface
        {
            public ContentProcessorControl(INativeWindow windowInfo)
            {
                WindowInfo = windowInfo;
            }

            public void Dispose()
            {
            }

            public Point PointToScreen(Point pt)
            {
                throw new NotImplementedException();
            }

            public Point PointToClient(Point pt)
            {
                throw new NotImplementedException();
            }

            public Vector2 Vector2ToScreen(Vector2 pt)
            {
                throw new NotImplementedException();
            }

            public Vector2 Vector2ToClient(Vector2 pt)
            {
                throw new NotImplementedException();
            }

            public Rectangle ClientRectangle { get; set; }
            public Size ClientSize { get; set; }
            public bool Focused { get; }
            public bool CursorVisible { get; set; }
            public bool CursorGrabbed { get; set; }
            public bool Visible { get; set; }
            public IntPtr Handle { get; }
            public INativeWindow WindowInfo { get; }
            public event Action<FrameEventArgs> RenderFrame;
            public event Action<FrameEventArgs> UpdateFrame;
            public event Action<CancelEventArgs> Closing;
            public event Action<FocusedChangedEventArgs> FocusedChanged;
            public event Action<TextInputEventArgs> KeyPress;
            public event Action<ResizeEventArgs> Resize;
            public event Action Load;
            public event Action<MouseWheelEventArgs> MouseWheel;
        }

        private class ContentProcessorGame : Game<IRenderingSurface>
        {
            public ContentProcessorGame(IRenderingSurface surface, IGraphicsContext context)
            {
                ConstructContext(surface, context);
                InitializeControl();
            }

        }
        
        public ContentProcessorContext(SynchronizationContext syncContext, AssemblyCreatedContent createdContentAssembly, IRenderingSurface surface , GraphicsDevice graphicsDevice, Guid buildId, string contentDirectory, string workingDirectory = "")
            : base(buildId, createdContentAssembly, contentDirectory, workingDirectory)
        {
            SyncContext = syncContext;

            if (surface == null && graphicsDevice != null || surface != null && graphicsDevice == null)
                throw new ArgumentException($"Either both of {nameof(surface)} and {nameof(graphicsDevice._context)} must be set or not set.");

            IGraphicsContext context;
            if (surface == null)
            {
                var nativeWindowSettings = new NativeWindowSettings();
                nativeWindowSettings.StartVisible = false;
                var window = new GameWindow(GameWindowSettings.Default, nativeWindowSettings);
                surface = new ContentProcessorControl(window);
                context = window.Context;
            }
            else
            {
                
                context = graphicsDevice._context;
            }

            context.MakeCurrent();

            GraphicsDevice = new ContentProcessorGame(surface, context).GraphicsDevice;
        }

        public SynchronizationContext SyncContext { get; }
        public GraphicsDevice GraphicsDevice { get; }
        
        public override void Dispose()
        {
            GraphicsDevice.Game.Dispose();
            //Window.Dispose();
        }
    }
}