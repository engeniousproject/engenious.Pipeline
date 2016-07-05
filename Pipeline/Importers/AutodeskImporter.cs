using System;
using System.IO;
using engenious.Content.Pipeline;
using System.Reflection;
using FBXImporter;
namespace engenious.Pipeline
{
    [ContentImporterAttribute(".fbx", DisplayName = "Model Importer", DefaultProcessor = "AutodeskProcessor")]
    public class AutodeskImporter : ContentImporter<FBXImporter.FbxScene>
    {

        public AutodeskImporter()
        {

        }

        public override FBXImporter.FbxScene Import(string filename, ContentImporterContext context)
        {
            try
            {
                using(FbxContext c = new FbxContext())
                    return new FbxScene(c,filename);
            }
            catch (Exception ex)
            {
                context.RaiseBuildMessage(filename , ex.Message, BuildMessageEventArgs.BuildMessageType.Error);
            }
            return null;
        }

    }
}

