using System;
using System.Collections.Generic;
using System.Linq;

namespace engenious.Pipeline
{
    /// <summary>
    ///     Font config implementation for unix systems.
    /// </summary>
    public class FontConfigUnix : FontConfig
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="FontConfigUnix"/> class.
        /// </summary>
        /// <param name="fcInitLoadConfigAndFonts">FcInitLoadConfigAndFonts libfontconfig wrapped call.</param>
        /// <param name="fcPatternCreate">FcPatternCreate libfontconfig wrapped call.</param>
        /// <param name="fcNameParse">FcNameParse libfontconfig wrapped call.</param>
        /// <param name="fcConfigSubstitute">FcConfigSubstitute libfontconfig wrapped call.</param>
        /// <param name="fcDefaultSubstitute">FcDefaultSubstitute libfontconfig wrapped call.</param>
        /// <param name="fcFontMatch">FcFontMatch libfontconfig wrapped call.</param>
        /// <param name="fcPatternDestroy">FcPatternDestroy libfontconfig wrapped call.</param>
        /// <param name="fcPatternGetString">FcPatternGetString libfontconfig wrapped call.</param>
        protected FontConfigUnix(Func<IntPtr> fcInitLoadConfigAndFonts,
            Func<IntPtr> fcPatternCreate,
            Func<string, IntPtr> fcNameParse,
            Func<IntPtr, IntPtr, FcMatchKind, bool> fcConfigSubstitute,
            Action<IntPtr> fcDefaultSubstitute,
            FcFontMatchDelegate fcFontMatch,
            Action<IntPtr> fcPatternDestroy,
            FcPatternGetStringDelegate fcPatternGetString)
        {
            FcInitLoadConfigAndFonts = fcInitLoadConfigAndFonts;
            FcPatternCreate = fcPatternCreate;
            FcNameParse = fcNameParse;
            FcConfigSubstitute = fcConfigSubstitute;
            FcDefaultSubstitute = fcDefaultSubstitute;
            FcFontMatch = fcFontMatch;
            FcPatternDestroy = fcPatternDestroy;
            FcPatternGetString = fcPatternGetString;
        }

        /// <summary>
        ///     Enumeration of the match kind.
        /// </summary>
        protected enum FcMatchKind
        {
            /// <summary>
            ///     Match using a pattern.
            /// </summary>
            FcMatchPattern = 0,
            /// <summary>
            ///     Match using a font.
            /// </summary>
            FcMatchFont = 1
        }

        /// <summary>
        ///     Enumeration of possible font config results.
        /// </summary>
        protected enum FcResult
        {
            /// <summary>
            ///     Found a match.
            /// </summary>
            FcResultMatch = 0,

            /// <summary>
            ///     Did not find a match.
            /// </summary>
            FcResultNoMatch = 1,

            /// <summary>
            ///     The result has a type mismatch.
            /// </summary>
            FcResultTypeMismatch = 2,

            /// <summary>
            ///     The result has no id.
            /// </summary>
            FcResultNoId = 3,

            /// <summary>
            ///     The operation could not be completed, because the process ran out of memory.
            /// </summary>
            FcResultOutOfMemory = 4
        }

        /// <summary>
        ///     <see cref="Delegate"/> used for the <see cref="FcFontMatch"/> wrapper function.
        /// </summary>
        protected delegate IntPtr FcFontMatchDelegate(IntPtr config, IntPtr pattern, out FcResult result);

        /// <summary>
        ///     <see cref="Delegate"/> used for the <see cref="FcPatternGetString"/> wrapper function.
        /// </summary>
        protected delegate FcResult FcPatternGetStringDelegate(IntPtr pattern, string name, int n,
            out IntPtr resultString);

        /// <summary>
        ///     Instructs fontconfig to use the parameter as a filename holding the font relative to the config's sysroot.
        /// </summary>
        protected const string FcFile = "file";
        /// <summary>
        ///     FcInitLoadConfigAndFonts libfontconfig wrapped call.
        /// </summary>
        protected readonly Func<IntPtr> FcInitLoadConfigAndFonts;
        /// <summary>
        ///     FcPatternCreate libfontconfig wrapped call.
        /// </summary>
        protected readonly Func<IntPtr> FcPatternCreate;
        /// <summary>
        ///     FcNameParse libfontconfig wrapped call.
        /// </summary>
        protected readonly Func<string, IntPtr> FcNameParse;
        /// <summary>
        ///     FcConfigSubstitute libfontconfig wrapped call.
        /// </summary>
        protected readonly Func<IntPtr, IntPtr, FcMatchKind, bool> FcConfigSubstitute;
        /// <summary>
        ///     FcDefaultSubstitute libfontconfig wrapped call.
        /// </summary>
        protected readonly Action<IntPtr> FcDefaultSubstitute;
        /// <summary>
        ///     FcFontMatch libfontconfig wrapped call.
        /// </summary>
        protected readonly FcFontMatchDelegate FcFontMatch;
        /// <summary>
        ///     FcPatternDestroy libfontconfig wrapped call.
        /// </summary>
        protected readonly Action<IntPtr> FcPatternDestroy;
        /// <summary>
        ///     FcPatternGetString libfontconfig wrapped call.
        /// </summary>
        protected readonly FcPatternGetStringDelegate FcPatternGetString;

        #region implemented abstract members of FontConfig

        /// <inheritdoc />
        public override bool GetFontFile(string fontName, int fontSize, FontStyle style,
            out string? fileName)
        {
            fileName = null;
            var config = FcInitLoadConfigAndFonts();

            var fontStyles = new List<string>();
            foreach (var val in Enum.GetValues(typeof(FontStyle)).OfType<FontStyle>()
                .Skip(1))
            {
                if (style.HasFlag(val))
                    fontStyles.Add(val.ToString().ToLower());
            }

            // configure the search pattern, 
            // assume "name" is a std::string with the desired font name in it
            string styles = string.Join(":", fontStyles);
            var pat = FcNameParse(fontName + "-" + fontSize.ToString() + ":" + styles);
            FcConfigSubstitute(config, pat, FcMatchKind.FcMatchPattern);
            FcDefaultSubstitute(pat);

            // find the font
            var font = FcFontMatch(IntPtr.Zero, pat, out _);
            if (font != IntPtr.Zero)
            {
                if (FcPatternGetString(font, FcFile, 0, out var resultPtr) == FcResult.FcResultMatch)
                {
                    // save the file to another std::string
                    fileName = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(resultPtr);
                }

                FcPatternDestroy(font);
            }

            FcPatternDestroy(pat);

            return true;
        }

        #endregion
    }
}