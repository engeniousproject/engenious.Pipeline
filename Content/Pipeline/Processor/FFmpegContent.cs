namespace engenious.Pipeline
{
    /// <summary>
    ///     Class describing data to create content files from with ffmpeg.
    /// </summary>
    public class FFmpegContent
    {
        /// <summary>
        ///     Gets the name of the file to create a content file from.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="FFmpegContent"/> class.
        /// </summary>
        /// <param name="fileName">The name of the file to create a content file from.</param>
        public FFmpegContent(string fileName)
        {
            FileName = fileName;
        }
    }
}