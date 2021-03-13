﻿using System;
using System.IO;
using engenious.Content;
using engenious.Content.Pipeline;
using engenious.Graphics;

namespace engenious.Pipeline
{
    [ContentImporter(".ego", DisplayName = "Model Importer", DefaultProcessor = "EgoModelProcessor")]
    public class EgoImporter : ContentImporter<ModelContent>
    {
        #region implemented abstract members of ContentImporter

        public override ModelContent? Import(string filename, ContentImporterContext context)
        {
            var dirName = Path.GetDirectoryName(filename) ??
                          throw new DirectoryNotFoundException($"Could not get directory name of {filename}]");
            var fileWithoutExt = Path.GetFileNameWithoutExtension(filename) ??
                                 throw new ArgumentException("Not a valid filename", nameof(filename));
            ContentManagerBase managerBase = new AggregateContentManager(null!, dirName);
            return managerBase.Load<ModelContent>(fileWithoutExt);
        }

        #endregion
    }
}

