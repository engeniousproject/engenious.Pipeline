using System;
using System.ComponentModel;
using System.IO;
using engenious.Audio;
using engenious.Content.Pipeline;

namespace engenious.Pipeline
{
    /// <summary>
    ///     Processor that processes audio files to engenious audio content files.
    /// </summary>
    [ContentProcessor(DisplayName = "Audio Processor")]
    public class AudioProcessor : ContentProcessor<FFmpegContent, AudioContent, AudioProcessorSettings>
    {
        #region implemented abstract members of ContentProcessor

        /// <inheritdoc />
        public override AudioContent? Process(FFmpegContent input, string filename, ContentProcessorContext context)
        {
            try
            {
                var ff = new FFmpeg(context.SyncContext);
                string args = string.Empty;
                switch (_settings.OutputFormat)
                {
                    case SoundEffect.AudioFormat.Ogg:
                        args = "-acodec libvorbis -ab 128k -ar 44100 -f ogg";
                        break;
                    case SoundEffect.AudioFormat.Wav:
                        args = "-acodec pcm_s16le -ar 44100 -f wav";
                        break;
                }
                var process = ff.RunCommand($"-i \"{filename}\" {args} -nostdin pipe:1 -hide_banner -loglevel error");
                var outputStream = process.StandardOutput.BaseStream;
                // if (outputStream == null)
                // {
                //     context.RaiseBuildMessage(filename, "error: ffmpeg: could not read from stdout", BuildMessageEventArgs.BuildMessageType.Error);
                //     return null;
                // }
                var output = new AudioContent(_settings.OutputFormat, outputStream, false);
                process.WaitForExit();
                var err = process.StandardError.ReadToEnd();//TODO: error handling
                if (!string.IsNullOrEmpty(err))
                {
                    context.RaiseBuildMessage(filename, "error: ffmpeg: " + err, BuildMessageEventArgs.BuildMessageType.Error);
                }
                return output;
            }catch (FileNotFoundException ex)
            {
                context.RaiseBuildMessage(filename, "error: ffmpeg: " + ex.Message, BuildMessageEventArgs.BuildMessageType.Error);
                return null;
            }
        }

        #endregion
    }
    /// <summary>
    ///     <see cref="AudioProcessor"/> specific settings.
    /// </summary>
    [Serializable]
    public class AudioProcessorSettings : ProcessorSettings
    {
        /// <summary>
        ///     Gets or sets a value indicating the audio content file output format to use for content files.
        /// </summary>
        [Category("Settings")]
        [DefaultValue(SoundEffect.AudioFormat.Ogg)]
        public SoundEffect.AudioFormat OutputFormat { get; set; }
    }
}

