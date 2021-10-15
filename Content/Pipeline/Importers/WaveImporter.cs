using engenious.Content.Pipeline;

namespace engenious.Pipeline
{
    /// <summary>
    ///     <see cref="ContentImporter{T}"/> used to import <see cref="FFmpegContent"/> files from(.wav, .ogg, .mp3).
    /// </summary>
    [ContentImporter(".wav",".ogg",".mp3", DisplayName = "Wave Importer", DefaultProcessor = "AudioProcessor")]
    public class WaveImporter : ContentImporter<FFmpegContent>
    {
        #region implemented abstract members of ContentImporter

        /// <inheritdoc />
        public override FFmpegContent Import(string filename, ContentImporterContext context)
        {
            return new FFmpegContent(filename);
        }

        #endregion
    }
}

