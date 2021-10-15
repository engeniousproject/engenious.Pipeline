using System;
using System.Runtime.InteropServices;

namespace engenious.Pipeline
{
    /// <summary>
    ///     Font config implementation for mac.
    /// </summary>
    public class FontConfigMac : FontConfigUnix
    {
        [DllImport("libfontconfig.1.dylib",EntryPoint="FcInitLoadConfigAndFonts")]
        private static extern IntPtr FcInitLoadConfigAndFonts_Base();
        [DllImport("libfontconfig.1.dylib",EntryPoint="FcPatternCreate")]
        private static extern IntPtr FcPatternCreate_Base();
        [DllImport("libfontconfig.1.dylib",EntryPoint="FcNameParse")]
        private static extern IntPtr FcNameParse_Base(string name);
        [DllImport("libfontconfig.1.dylib",EntryPoint="FcConfigSubstitute")]
        private static extern bool FcConfigSubstitute_Base(IntPtr config,IntPtr pattern,FcMatchKind matchKind);
        [DllImport("libfontconfig.1.dylib",EntryPoint="FcDefaultSubstitute")]
        private static extern void FcDefaultSubstitute_Base(IntPtr pattern);
        [DllImport("libfontconfig.1.dylib",EntryPoint="FcFontMatch")]
        private static extern IntPtr FcFontMatch_Base(IntPtr config,IntPtr pattern,out FcResult result);
        [DllImport("libfontconfig.1.dylib",EntryPoint="FcPatternDestroy")]
        private static extern void FcPatternDestroy_Base(IntPtr pattern);
        [DllImport("libfontconfig.1.dylib",EntryPoint="FcPatternGetString")]
        private static extern FcResult FcPatternGetString_Base(IntPtr pattern, string name, int n, out IntPtr resultString);

        /// <inheritdoc />
        public FontConfigMac()
            : base(FcInitLoadConfigAndFonts_Base, FcPatternCreate_Base, FcNameParse_Base, FcConfigSubstitute_Base,
                FcDefaultSubstitute_Base, FcFontMatch_Base, FcPatternDestroy_Base, FcPatternGetString_Base)
        {

        }
    }
}

