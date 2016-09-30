using System;
using System.IO;
using engenious.Content.Pipeline;
using System.Reflection;

namespace engenious.Pipeline
{
    [ContentImporterAttribute(".fbx",".dae", DisplayName = "Model Importer", DefaultProcessor = "ModelProcessor")]
    public class FbxImporter : ContentImporter<Assimp.Scene>
    {

        private static Exception dllLoadExc;

        static FbxImporter()
        {
            try
            {
                string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string ext = ".dll";
                switch (PlatformHelper.RunningPlatform())
                {
                    case Platform.Linux:
                        ext = ".so";
                        break;
                    case Platform.Mac:
                        ext = ".dylib";
                        break;
                }
                if (Environment.Is64BitProcess)
                    dir = Path.Combine(dir, "Assimp64" + ext);
                else
                    dir = Path.Combine(dir, "Assimp64" + ext);
                Assimp.Unmanaged.AssimpLibrary.Instance.LoadLibrary(dir);
            }
            catch (Exception ex)
            {
                dllLoadExc = ex;
            }
        }

        public FbxImporter()
        {
        }

        public override Assimp.Scene Import(string filename, ContentImporterContext context)
        {
            if (dllLoadExc != null)
                context.RaiseBuildMessage("FBXIMPORT" , dllLoadExc.Message, BuildMessageEventArgs.BuildMessageType.Error);
            try
            {
                Assimp.AssimpContext c = new Assimp.AssimpContext();
                return c.ImportFile(filename,Assimp.PostProcessSteps.Triangulate | Assimp.PostProcessSteps.OptimizeMeshes | Assimp.PostProcessSteps.OptimizeGraph);
            }
            catch (Exception ex)
            {
                context.RaiseBuildMessage(filename , ex.Message, BuildMessageEventArgs.BuildMessageType.Error);
            }
            return null;
        }
    }
}

