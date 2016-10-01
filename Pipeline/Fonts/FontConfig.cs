using System;

namespace engenious.Pipeline
{
    public abstract class FontConfig
    {
        private static FontConfig fontConfig;
        public static FontConfig Instance {
            get{
                
                if (fontConfig == null)
                {
                    switch(PlatformHelper.RunningPlatform())
                    {
                        case Platform.Linux:
                            fontConfig = new FontConfigLinux();
                            break;
                        case Platform.Mac:
                            fontConfig = new FontConfigMac();
                            break;
                        case Platform.Windows:
                            fontConfig = new FontConfigWindows();
                            break;
                    }

                }
                return fontConfig;
            }
        }
        public abstract string GetFontFile(string fontName,int fontSize,System.Drawing.FontStyle style);
    }
}

