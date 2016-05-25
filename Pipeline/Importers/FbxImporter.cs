using System;
using System.IO;
using engenious.Content.Pipeline;
using System.Reflection;

namespace engenious.Pipeline
{
    [ContentImporterAttribute(".fbx", DisplayName = "Model Importer", DefaultProcessor = "ModelProcessor")]
    public class FbxImporter : ContentImporter<Assimp.Scene>
    {
        public enum Platform
        {
            Windows,
            Linux,
            Mac
        }

        public static Platform RunningPlatform()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Unix:
                    // Well, there are chances MacOSX is reported as Unix instead of MacOSX.
                    // Instead of platform check, we'll do a feature checks (Mac specific root folders)
                    if (Directory.Exists("/Applications")
                        & Directory.Exists("/System")
                        & Directory.Exists("/Users")
                        & Directory.Exists("/Volumes"))
                        return Platform.Mac;
                    else
                        return Platform.Linux;

                case PlatformID.MacOSX:
                    return Platform.Mac;

                default:
                    return Platform.Windows;
            }
        }

        private static Exception dllLoadExc;

        static FbxImporter()
        {
            try
            {
                string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string ext = ".dll";
                switch (RunningPlatform())
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
                return c.ImportFile(filename);
            }
            catch (Exception ex)
            {
                context.RaiseBuildMessage(filename , ex.Message, BuildMessageEventArgs.BuildMessageType.Error);
            }
            return null;
        }
    }
}

