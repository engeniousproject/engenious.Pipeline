using System;
using engenious.Content.Pipeline;

namespace engenious.Pipeline
{
    [ContentImporterAttribute(".wav",".ogg",".mp3", DisplayName = "Wave Importer", DefaultProcessor = "AudioProcessor")]
    public class WaveImporter : ContentImporter<FFmpegContent>
    {
        public WaveImporter()
        {
        }

        #region implemented abstract members of ContentImporter

        public override FFmpegContent Import(string filename, ContentImporterContext context)
        {
            return new FFmpegContent(filename);
        }

        #endregion
    }
}

