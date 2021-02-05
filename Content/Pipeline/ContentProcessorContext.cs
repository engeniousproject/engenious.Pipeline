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

        public ContentProcessorContext(SynchronizationContext syncContext, AssemblyCreatedContent createdContentAssembly, IGame game, Guid buildId, string contentDirectory, string workingDirectory = "")
            : base(buildId, createdContentAssembly, contentDirectory, workingDirectory)
        {
            SyncContext = syncContext;


            Game = game;
            GraphicsDevice.SwitchUiThread();
        }

        public SynchronizationContext SyncContext { get; }
        public IGame Game { get; }
        public GraphicsDevice GraphicsDevice => Game.GraphicsDevice;
        
        public override void Dispose()
        {
            GraphicsDevice._context.MakeNoneCurrent();
            //Window.Dispose();
        }
    }
}