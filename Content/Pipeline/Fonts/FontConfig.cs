using System;
using engenious.Helper;

namespace engenious.Pipeline
{
    /// <summary>
    ///     Base class for operating system independent font conig.
    /// </summary>
    public abstract class FontConfig
    {
        private static FontConfig? _fontConfig;

        /// <summary>
        ///     Gets a font config for the current operating system.
        /// </summary>
        /// <exception cref="NotSupportedException">Thrown when the current operating system is not supported.</exception>
        public static FontConfig Instance
        {
            get
            {
                if (_fontConfig != null)
                    return _fontConfig;
                _fontConfig = PlatformHelper.RunningPlatform() switch
                {
                    Platform.Linux => new FontConfigLinux(),
                    Platform.Mac => new FontConfigMac(),
                    Platform.Windows => new FontConfigWindows(),
                    _ => throw new NotSupportedException()
                };
                return _fontConfig;
            }
        }

        /// <summary>
        ///     Tries to get a font file by name, size, and style.
        /// </summary>
        /// <param name="fontName">The font name of the font file to search for.</param>
        /// <param name="fontSize">The font size of the font file to search for.</param>
        /// <param name="style">The style of the font file to search for.</param>
        /// <param name="fileName">The file name of the matching font file.</param>
        /// <returns>Whether a font file was found.</returns>
        public abstract bool GetFontFile(string fontName, int fontSize, FontStyle style,
            out string? fileName);
    }
}