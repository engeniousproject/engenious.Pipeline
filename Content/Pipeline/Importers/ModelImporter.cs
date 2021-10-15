using System;
using System.IO;
using System.Reflection;
using Assimp;
using engenious.Content.Pipeline;
using engenious.Helper;

namespace engenious.Pipeline
{
    /// <summary>
    ///     <see cref="ContentImporter{T}"/> used to import <see cref="Scene"/> files from
    ///     (.3D,.3DS,.3MF,.AC,.AC3D,.ACC,.AMJ,.ASE,.ASK,.B3D,.BLEND,.BVH,.COB,.CMS,.DAE,.DXF,.ENFF,.FBX,.glTF,.glTF,
    ///      .glB,.HMB,.IFC-STEP,.IRR,.LWO,.LWS,.LXO,.MD2,.MD3,.MD5,.MDC,.MDL,.MESH,.MOT,.MS3D,.NDO,.NFF,.OBJ,.OFF,.OGEX,
    ///      .PLY,.PMX,.PRJ,.Q3O,.Q3S,.RAW,.SCN,.SIB,.SMD,.STL,.STP,.TER,.UC,.VTA,.X,.X3D,.XGL,.ZGL).
    /// </summary>
    [ContentImporter(".3D",".3DS",".3MF",".AC",".AC3D",".ACC",".AMJ",".ASE",".ASK",".B3D",".BLEND",".BVH",".COB",".CMS",".DAE",".DXF",".ENFF",".FBX",".glTF",".glTF", ".glB",".HMB",".IFC-STEP",".IRR",".LWO",".LWS",".LXO",".MD2",".MD3",".MD5",".MDC",".MDL",".MESH",".MOT",".MS3D",".NDO",".NFF",".OBJ",".OFF",".OGEX",".PLY",".PMX",".PRJ",".Q3O",".Q3S",".RAW",".SCN",".SIB",".SMD",".STL",".STP",".TER",".UC",".VTA",".X",".X3D",".XGL",".ZGL", DisplayName = "Model Importer", DefaultProcessor = "ModelProcessor")]
    public class ModelImporter : ContentImporter<Scene>
    {

        private static readonly Exception? DllLoadExc;

        static ModelImporter()
        {
            try
            {
                var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (dir == null)
                    throw new Exception("executing path not found");
                string ext = ".dll";
                string prefix = "win";
                string libprefix = "";
                switch (PlatformHelper.RunningPlatform())
                {
                    case Platform.Linux:
                        ext = ".so";
                        prefix = "linux";
                        libprefix = "lib";
                        break;
                    case Platform.Mac:
                        ext = ".dylib";
                        prefix = "osx";
                        libprefix = "lib";
                        break;
                }
                if (Environment.Is64BitProcess)
                    dir = Path.Combine(dir, "runtimes", prefix + "-x64", "native", libprefix + "assimp" + ext);
                else
                    dir = Path.Combine(dir, "runtimes", prefix + "-x86", "native", libprefix + "assimp" + ext);
                Assimp.Unmanaged.AssimpLibrary.Instance.LoadLibrary(dir);
            }
            catch (Exception ex)
            {
                DllLoadExc = ex;
            }
        }

        /// <inheritdoc />
        public override Scene? Import(string filename, ContentImporterContext context)
        {
            if (DllLoadExc != null)
                context.RaiseBuildMessage("FBXIMPORT" , DllLoadExc.Message, BuildMessageEventArgs.BuildMessageType.Error);
            try
            {
                AssimpContext c = new AssimpContext();
                return c.ImportFile(filename,PostProcessSteps.Triangulate);//,PostProcessSteps.JoinIdenticalVertices | PostProcessSteps.FindInstances | PostProcessSteps.Triangulate | PostProcessSteps.OptimizeMeshes | PostProcessSteps.OptimizeGraph
            }
            catch (Exception ex)
            {
                context.RaiseBuildMessage(filename , ex.Message, BuildMessageEventArgs.BuildMessageType.Error);
            }
            return null;
        }
    }
}

