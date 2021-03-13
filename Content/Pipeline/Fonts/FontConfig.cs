using System;
using engenious.Helper;

namespace engenious.Pipeline
{
    public abstract class FontConfig
    {
        private static FontConfig? _fontConfig;

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

        public abstract bool GetFontFile(string fontName, int fontSize, System.Drawing.FontStyle style,
            out string? fileName);
    }
}