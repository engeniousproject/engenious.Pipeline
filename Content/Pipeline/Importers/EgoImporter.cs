using System;
using System.IO;
using engenious.Content;
using engenious.Content.Pipeline;
using engenious.Graphics;

namespace engenious.Pipeline
{
    /// <summary>
    ///     <see cref="ContentImporter{T}"/> used to import <see cref="ModelContent"/> files from(.ego).
    /// </summary>
    [ContentImporter(".ego", DisplayName = "Model Importer", DefaultProcessor = "EgoModelProcessor")]
    public class EgoImporter : ContentImporter<ModelContent>
    {
        #region implemented abstract members of ContentImporter

        /// <inheritdoc />
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

