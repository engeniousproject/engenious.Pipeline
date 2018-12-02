using System;
using System.Runtime.InteropServices;

namespace engenious.Pipeline
{
    public class FontConfigLinux:FontConfigUnix
    {
        [DllImport("libfontconfig.so.1",EntryPoint="FcInitLoadConfigAndFonts")]
        private static extern IntPtr FcInitLoadConfigAndFonts_Base();
        [DllImport("libfontconfig.so.1",EntryPoint="FcPatternCreate")]
        private static extern IntPtr FcPatternCreate_Base();
        [DllImport("libfontconfig.so.1",EntryPoint="FcNameParse")]
        private static extern IntPtr FcNameParse_Base(string name);
        [DllImport("libfontconfig.so.1",EntryPoint="FcConfigSubstitute")]
        private static extern bool FcConfigSubstitute_Base(IntPtr config,IntPtr pattern,FcMatchKind matchKind);
        [DllImport("libfontconfig.so.1",EntryPoint="FcDefaultSubstitute")]
        private static extern void FcDefaultSubstitute_Base(IntPtr pattern);
        [DllImport("libfontconfig.so.1",EntryPoint="FcFontMatch")]
        private static extern IntPtr FcFontMatch_Base(IntPtr config,IntPtr pattern,out FcResult result);
        [DllImport("libfontconfig.so.1",EntryPoint="FcPatternDestroy")]
        private static extern void FcPatternDestroy_Base(IntPtr pattern);
        [DllImport("libfontconfig.so.1",EntryPoint="FcPatternGetString")]
        private static extern FcResult FcPatternGetString_Base(IntPtr pattern, string name, int n, out IntPtr resultString);
        public FontConfigLinux()
        {
            FcInitLoadConfigAndFonts = FcInitLoadConfigAndFonts_Base;
            FcPatternCreate=FcPatternCreate_Base;
            FcNameParse=FcNameParse_Base;
            FcConfigSubstitute=FcConfigSubstitute_Base;
            FcDefaultSubstitute=FcDefaultSubstitute_Base;
            FcFontMatch=FcFontMatch_Base;
            FcPatternDestroy=FcPatternDestroy_Base;
            FcPatternGetString=FcPatternGetString_Base;
            
        }
    }
}

