using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using engenious.Helper;

namespace engenious.Pipeline
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal unsafe struct LOGFONT
    {
        private const int LF_FACESIZE = 32;

        public int lfHeight;
        public int lfWidth;
        public int lfEscapement;
        public int lfOrientation;
        public int lfWeight;
        public byte lfItalic;
        public byte lfUnderline;
        public byte lfStrikeOut;
        public byte lfCharSet;
        public byte lfOutPrecision;
        public byte lfClipPrecision;
        public byte lfQuality;
        public byte lfPitchAndFamily;
        private fixed char _lfFaceName[LF_FACESIZE];
        public Span<char> lfFaceName => MemoryMarshal.CreateSpan(ref _lfFaceName[0], LF_FACESIZE);

        public override string ToString()
        {
            return
                "lfHeight=" + lfHeight + ", " +
                "lfWidth=" + lfWidth + ", " +
                "lfEscapement=" + lfEscapement + ", " +
                "lfOrientation=" + lfOrientation + ", " +
                "lfWeight=" + lfWeight + ", " +
                "lfItalic=" + lfItalic + ", " +
                "lfUnderline=" + lfUnderline + ", " +
                "lfStrikeOut=" + lfStrikeOut + ", " +
                "lfCharSet=" + lfCharSet + ", " +
                "lfOutPrecision=" + lfOutPrecision + ", " +
                "lfClipPrecision=" + lfClipPrecision + ", " +
                "lfQuality=" + lfQuality + ", " +
                "lfPitchAndFamily=" + lfPitchAndFamily + ", " +
                "lfFaceName=" + lfFaceName.ToString();
        }
    }

    internal partial class Gdip
    {
        /// <summary>
        /// Simple wrapper to create a screen HDC within a using statement.
        /// </summary>
        internal struct ScreenDC : IDisposable
        {
            private const string User32LibraryName = "user32.dll";
            private IntPtr _handle;

            public static ScreenDC Create() => new ScreenDC
                                               {
                                                   _handle = GetDC(IntPtr.Zero)
                                               };

            public static implicit operator IntPtr(ScreenDC screenDC) => screenDC._handle;

            public void Dispose() => ReleaseDC(IntPtr.Zero, _handle);

            [DllImport(User32LibraryName, ExactSpelling = true)]
            public static extern IntPtr GetDC(IntPtr hWnd);

            [DllImport(User32LibraryName, ExactSpelling = true)]
            public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct StartupInputEx
        {
            public int GdiplusVersion;             // Must be 1 or 2

            public IntPtr DebugEventCallback;

            public bool SuppressBackgroundThread;     // FALSE unless you're prepared to call
                                                              // the hook/unhook functions properly

            public bool SuppressExternalCodecs;       // FALSE unless you want GDI+ only to use
                                                              // its internal image codecs.
            public int StartupParameters;

            public static StartupInputEx GetDefault()
            {
                OperatingSystem os = Environment.OSVersion;
                StartupInputEx result = default;

                // In Windows 7 GDI+1.1 story is different as there are different binaries per GDI+ version.
                bool isWindows7 = os.Platform == PlatformID.Win32NT && os.Version.Major == 6 && os.Version.Minor == 1;
                result.GdiplusVersion = isWindows7 ? 1 : 2;
                result.SuppressBackgroundThread = false;
                result.SuppressExternalCodecs = false;
                result.StartupParameters = 0;
                return result;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct StartupOutput
        {
            // The following 2 fields won't be used.  They were originally intended
            // for getting GDI+ to run on our thread - however there are marshalling
            // dealing with function *'s and what not - so we make explicit calls
            // to gdi+ after the fact, via the GdiplusNotificationHook and
            // GdiplusNotificationUnhook methods.
            public IntPtr hook; //not used
            public IntPtr unhook; //not used.
        }
        internal enum GraphicsUnit
        {
            /// <summary>Specifies the world coordinate system unit as the unit of measure.</summary>
            World,

            /// <summary>Specifies the unit of measure of the display device. Typically pixels for video displays, and 1/100 inch for printers.</summary>
            Display,

            /// <summary>Specifies a device pixel as the unit of measure.</summary>
            Pixel,

            /// <summary>Specifies a printer's point (1/72 inch) as the unit of measure.</summary>
            Point,

            /// <summary>Specifies the inch as the unit of measure.</summary>
            Inch,

            /// <summary>Specifies the document unit (1/300 inch) as the unit of measure.</summary>
            Document,

            /// <summary>Specifies the millimeter as the unit of measure.</summary>
            Millimeter,
        }
        private static readonly IntPtr s_initToken;
        static Gdip()
        {
            if (PlatformHelper.RunningPlatform() != Platform.Windows)
                return;
            Debug.Assert(s_initToken == IntPtr.Zero, "GdiplusInitialization: Initialize should not be called more than once in the same domain!");
            int status = GdiplusStartup(out s_initToken, StartupInputEx.GetDefault(), out _);
            CheckStatus(status);
        }


        private const string GdipLibraryName = "gdiplus.dll";
        private const string GdiLibraryName = "gdi32.dll";

        [DllImport(GdipLibraryName)]
        internal static extern int GdiplusStartup(out IntPtr token, in StartupInputEx input, out StartupOutput output);

        [DllImport(GdipLibraryName, CharSet = CharSet.Unicode)]
        internal static extern int GdipCreateFontFamilyFromName(string name, IntPtr fontCollection,
            out IntPtr FontFamily);

        [DllImport(GdipLibraryName)]
        internal static extern int GdipGetGenericFontFamilySansSerif(out IntPtr fontfamily);

        [DllImport(GdipLibraryName)]
        internal static extern int GdipCreateFont(IntPtr fontFamily, float emSize, FontStyle style, GraphicsUnit unit,
            out IntPtr font);

        [DllImport(GdipLibraryName)]
        internal static extern int GdipGetLogFontW(IntPtr font, IntPtr graphics, ref LOGFONT lf);

        [DllImport(GdipLibraryName)]
        internal static extern int GdipCreateFromHDC(IntPtr hdc, out IntPtr graphics);

        [DllImport(GdipLibraryName)]
        internal static extern int GdipDeleteGraphics(IntPtr graphics);


        [DllImport(GdiLibraryName, ExactSpelling = true, CharSet = CharSet.Unicode)]
        internal static extern IntPtr CreateFontIndirectW(ref LOGFONT lplf);

        private static IntPtr GetGdipGenericSansSerif()
        {
            IntPtr nativeFamily;
            int status = Gdip.GdipGetGenericFontFamilySansSerif(out nativeFamily);
            Gdip.CheckStatus(status);

            return nativeFamily;
        }

        public static bool TryCreateFontFamily(string name, out IntPtr fontfamily)
        {
            IntPtr nativeFontCollection = IntPtr.Zero;

            int status = Gdip.GdipCreateFontFamilyFromName(name, nativeFontCollection, out fontfamily);

            if (status != Gdip.Ok)
            {
                fontfamily = GetGdipGenericSansSerif(); // This throws if failed.
                return false;
            }

            return true;
        }

        public static IntPtr CreateHFont(IntPtr fontFamily, float emSize, FontStyle fontStyle)
        {
            if (float.IsNaN(emSize) || float.IsInfinity(emSize) || emSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(emSize));
            }

            var gdiFont = CreateNativeFont(fontFamily, emSize, fontStyle);

            

            return ToHfont(gdiFont);
        }

        private static IntPtr CreateNativeFont(IntPtr fontFamily, float fontSize, FontStyle fontStyle)
        {
            // Note: GDI+ creates singleton font family objects (from the corresponding font file) and reference count them so
            // if creating the font object from an external FontFamily, this object's FontFamily will share the same native object.
            int status = Gdip.GdipCreateFont(
                fontFamily,
                fontSize,
                fontStyle,
                GraphicsUnit.Point,
                out var nativeFont);

            // Special case this common error message to give more information
            if (status == Gdip.FontStyleNotFound)
            {
                throw new ArgumentException("Font style not found!");
            }
            else if (status != Gdip.Ok)
            {
                throw Gdip.StatusException(status);
            }

            return nativeFont;
        }

        private static unsafe LOGFONT ToLogFontInternal(IntPtr nativeFont, IntPtr nativeGraphics)
        {
            LOGFONT logFont = default;
            Gdip.CheckStatus(Gdip.GdipGetLogFontW(nativeFont, nativeGraphics, ref logFont));

            if (logFont.lfCharSet == 0)
            {
                logFont.lfCharSet = DEFAULT_CHARSET;
            }

            return logFont;
        }

        public static IntPtr ToHfont(IntPtr nativeFont)
        {
            IntPtr nativeGraphics = IntPtr.Zero;

            using var dc = ScreenDC.Create();
            try
            {
                var status = Gdip.GdipCreateFromHDC(dc, out nativeGraphics);

                Gdip.CheckStatus(status);

                LOGFONT lf = ToLogFontInternal(nativeFont, nativeGraphics);
                IntPtr handle = CreateFontIndirectW(ref lf);
                if (handle == IntPtr.Zero)
                {
                    throw new Win32Exception();
                }

                return handle;
            }
            finally
            {
                if (nativeGraphics != IntPtr.Zero)
                    Gdip.GdipDeleteGraphics(nativeGraphics);
            }
        }

        //----------------------------------------------------------------------------------------
        // Status codes
        //----------------------------------------------------------------------------------------
        internal const int Ok = 0;
        internal const int GenericError = 1;
        internal const int InvalidParameter = 2;
        internal const int OutOfMemory = 3;
        internal const int ObjectBusy = 4;
        internal const int InsufficientBuffer = 5;
        internal const int NotImplemented = 6;
        internal const int Win32Error = 7;
        internal const int WrongState = 8;
        internal const int Aborted = 9;
        internal const int FileNotFound = 10;
        internal const int ValueOverflow = 11;
        internal const int AccessDenied = 12;
        internal const int UnknownImageFormat = 13;
        internal const int FontFamilyNotFound = 14;
        internal const int FontStyleNotFound = 15;
        internal const int NotTrueTypeFont = 16;
        internal const int UnsupportedGdiplusVersion = 17;
        internal const int GdiplusNotInitialized = 18;
        internal const int PropertyNotFound = 19;
        internal const int PropertyNotSupported = 20;

        internal const int DEFAULT_CHARSET = 1;

        internal static void CheckStatus(int status)
        {
            if (status != Ok)
                throw StatusException(status);
        }

        internal static Exception StatusException(int status)
        {
            Debug.Assert(status != Ok, "Throwing an exception for an 'Ok' return code");

            // switch (status)
            // {
            //     case GenericError:
            //         return new ExternalException(SR.GdiplusGenericError, E_FAIL);
            //     case InvalidParameter:
            //         return new ArgumentException(SR.GdiplusInvalidParameter);
            //     case OutOfMemory:
            //         return new OutOfMemoryException(SR.GdiplusOutOfMemory);
            //     case ObjectBusy:
            //         return new InvalidOperationException(SR.GdiplusObjectBusy);
            //     case InsufficientBuffer:
            //         return new OutOfMemoryException(SR.GdiplusInsufficientBuffer);
            //     case NotImplemented:
            //         return new NotImplementedException(SR.GdiplusNotImplemented);
            //     case Win32Error:
            //         return new ExternalException(SR.GdiplusGenericError, E_FAIL);
            //     case WrongState:
            //         return new InvalidOperationException(SR.GdiplusWrongState);
            //     case Aborted:
            //         return new ExternalException(SR.GdiplusAborted, E_ABORT);
            //     case FileNotFound:
            //         return new FileNotFoundException(SR.GdiplusFileNotFound);
            //     case ValueOverflow:
            //         return new OverflowException(SR.GdiplusOverflow);
            //     case AccessDenied:
            //         return new ExternalException(SR.GdiplusAccessDenied, E_ACCESSDENIED);
            //     case UnknownImageFormat:
            //         return new ArgumentException(SR.GdiplusUnknownImageFormat);
            //     case PropertyNotFound:
            //         return new ArgumentException(SR.GdiplusPropertyNotFoundError);
            //     case PropertyNotSupported:
            //         return new ArgumentException(SR.GdiplusPropertyNotSupportedError);
            //
            //     case FontFamilyNotFound:
            //         Debug.Fail("We should be special casing FontFamilyNotFound so we can provide the font name");
            //         return new ArgumentException(SR.Format(SR.GdiplusFontFamilyNotFound, "?"));
            //
            //     case FontStyleNotFound:
            //         Debug.Fail("We should be special casing FontStyleNotFound so we can provide the font name");
            //         return new ArgumentException(SR.Format(SR.GdiplusFontStyleNotFound, "?", "?"));
            //
            //     case NotTrueTypeFont:
            //         Debug.Fail("We should be special casing NotTrueTypeFont so we can provide the font name");
            //         return new ArgumentException(SR.GdiplusNotTrueTypeFont_NoName);
            //
            //     case UnsupportedGdiplusVersion:
            //         return new ExternalException(SR.GdiplusUnsupportedGdiplusVersion, E_FAIL);
            //
            //     case GdiplusNotInitialized:
            //         return new ExternalException(SR.GdiplusNotInitialized, E_FAIL);
            // }
            //
            // return new ExternalException($"{SR.GdiplusUnknown} [{status}]", E_UNEXPECTED);
            return new Exception();
        }
    }
}