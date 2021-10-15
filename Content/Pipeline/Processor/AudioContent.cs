using System.IO;
using engenious.Audio;

namespace engenious.Pipeline
{
    /// <summary>
    ///     Content type writer to serialize engenious audio content.
    /// </summary>
    public class AudioContent
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="AudioContent"/> class.
        /// </summary>
        /// <param name="outputFormat">The audio output format.</param>
        /// <param name="inputStream">The audio input stream.</param>
        /// <param name="closeStream">Whether to close the stream at the end.</param>
        public AudioContent(SoundEffect.AudioFormat outputFormat, Stream inputStream, bool closeStream = true)
        {
            OutputFormat = outputFormat;
            Data = new MemoryStream();
            byte[] buffer = new byte[1024 * 1024];
            while (true)
            {
                int read = inputStream.Read(buffer, 0, buffer.Length);
                if (read == 0)
                    break;
                Data.Write(buffer,0,read);
            }
            if (closeStream)
                inputStream.Close();
            Data.Position = 0;
        }
        
        /// <summary>
        ///     Gets the audio output format.
        /// </summary>
        public SoundEffect.AudioFormat OutputFormat { get; }
        /// <summary>
        ///     Gets the audio content data stream.
        /// </summary>
        public MemoryStream Data { get; }
    }
}

