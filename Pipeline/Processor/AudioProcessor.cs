using System;
using engenious.Content.Pipeline;
using System.IO;

namespace engenious.Pipeline
{
    [ContentProcessor(DisplayName = "Audio Processor")]
    public class AudioProcessor : ContentProcessor<FFmpegContent,AudioContent>
    {
        public AudioProcessor()
        {
        }

        #region implemented abstract members of ContentProcessor

        public override AudioContent Process(FFmpegContent input, string filename, ContentProcessorContext context)
        {
            ffmpeg ff = new ffmpeg();
            var process = ff.RunCommand("-i "+filename+ " ");
            var outputStream = process.StandardOutput.BaseStream as FileStream;
            process.WaitForExit();
            var err=process.StandardError.ReadToEnd();
            return new AudioContent(outputStream);
        }

        #endregion
    }
}

