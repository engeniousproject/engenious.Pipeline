using System;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.IO;


namespace engenious.Pipeline
{
    public class FontConfigWindows : FontConfig
	{
		public FontConfigWindows()
		{
		}

        #region implemented abstract members of FontConfig
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
            }

            public RECT(System.Drawing.Rectangle r) : this(r.Left, r.Top, r.Right, r.Bottom) { }

            public int X
            {
                get { return Left; }
                set { Right -= (Left - value); Left = value; }
            }

            public int Y
            {
                get { return Top; }
                set { Bottom -= (Top - value); Top = value; }
            }

            public int Height
            {
                get { return Bottom - Top; }
                set { Bottom = value + Top; }
            }

            public int Width
            {
                get { return Right - Left; }
                set { Right = value + Left; }
            }

            public System.Drawing.Point Location
            {
                get { return new System.Drawing.Point(Left, Top); }
                set { X = value.X; Y = value.Y; }
            }

            public System.Drawing.Size Size
            {
                get { return new System.Drawing.Size(Width, Height); }
                set { Width = value.Width; Height = value.Height; }
            }

            public static implicit operator System.Drawing.Rectangle(RECT r)
            {
                return new System.Drawing.Rectangle(r.Left, r.Top, r.Width, r.Height);
            }

            public static implicit operator RECT(System.Drawing.Rectangle r)
            {
                return new RECT(r);
            }

            public static bool operator ==(RECT r1, RECT r2)
            {
                return r1.Equals(r2);
            }

            public static bool operator !=(RECT r1, RECT r2)
            {
                return !r1.Equals(r2);
            }

            public bool Equals(RECT r)
            {
                return r.Left == Left && r.Top == Top && r.Right == Right && r.Bottom == Bottom;
            }

            public override bool Equals(object obj)
            {
                if (obj is RECT)
                    return Equals((RECT)obj);
                else if (obj is System.Drawing.Rectangle)
                    return Equals(new RECT((System.Drawing.Rectangle)obj));
                return false;
            }

            public override int GetHashCode()
            {
                return ((System.Drawing.Rectangle)this).GetHashCode();
            }

            public override string ToString()
            {
                return string.Format(System.Globalization.CultureInfo.CurrentCulture, "{{Left={0},Top={1},Right={2},Bottom={3}}}", Left, Top, Right, Bottom);
            }
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }

            public POINT(System.Drawing.Point pt) : this(pt.X, pt.Y) { }

            public static implicit operator System.Drawing.Point(POINT p)
            {
                return new System.Drawing.Point(p.X, p.Y);
            }

            public static implicit operator POINT(System.Drawing.Point p)
            {
                return new POINT(p.X, p.Y);
            }
        }
        [Serializable, StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
        private struct TEXTMETRIC
        {
            public int tmHeight;
            public int tmAscent;
            public int tmDescent;
            public int tmInternalLeading;
            public int tmExternalLeading;
            public int tmAveCharWidth;
            public int tmMaxCharWidth;
            public int tmWeight;
            public int tmOverhang;
            public int tmDigitizedAspectX;
            public int tmDigitizedAspectY;
            public byte tmFirstChar;    // for compatibility issues it must be byte instead of char (see the comment for further details above)
            public byte tmLastChar;    // for compatibility issues it must be byte instead of char (see the comment for further details above)
            public byte tmDefaultChar;    // for compatibility issues it must be byte instead of char (see the comment for further details above)
            public byte tmBreakChar;    // for compatibility issues it must be byte instead of char (see the comment for further details above)
            public byte tmItalic;
            public byte tmUnderlined;
            public byte tmStruckOut;
            public byte tmPitchAndFamily;
            public byte tmCharSet;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PANOSE
        {
            public byte bFamilyType;
            public byte bSerifStyle;
            public byte bWeight;
            public byte bProportion;
            public byte bContrast;
            public byte bStrokeVariation;
            public byte bArmStyle;
            public byte bLetterform;
            public byte bMidline;
            public byte bXHeight;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct OUTLINETEXTMETRIC
        {
            public uint     otmSize;
            public TEXTMETRIC otmTextMetrics;
            public byte     otmFiller;
            public PANOSE      otmPanoseNumber;
            public uint     otmfsSelection;
            public uint     otmfsType;
            public int    otmsCharSlopeRise;
            public int     otmsCharSlopeRun;
            public int     otmItalicAngle;
            public uint     otmEMSquare;
            public int     otmAscent;
            public int     otmDescent;
            public uint     otmLineGap;
            public uint     otmsCapEmHeight;
            public uint     otmsXHeight;
            public RECT     otmrcFontBox;
            public int        otmMacAscent;
            public int        otmMacDescent;
            public uint        otmMacLineGap;
            public uint        otmusMinimumPPEM;
            public POINT       otmptSubscriptSize;
            public POINT       otmptSubscriptOffset;
            public POINT       otmptSuperscriptSize;
            public POINT       otmptSuperscriptOffset;
            public uint        otmsStrikeoutSize;
            public int        otmsStrikeoutPosition;
            public int        otmsUnderscoreSize;
            public int        otmsUnderscorePosition;
            public IntPtr      otmpFamilyName;
            public IntPtr      otmpFaceName;
            public IntPtr      otmpStyleName;
            public IntPtr      otmpFullName;
        }
        // if we specify CharSet.Auto instead of CharSet.Ansi, then the string will be unreadable
        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)]
        public class LOGFONT
        {
            public int lfHeight = 0;
            public int lfWidth = 0;
            public int lfEscapement = 0;
            public int lfOrientation = 0;
            public int lfWeight = 0;
            public byte lfItalic = 0;
            public byte lfUnderline = 0;
            public byte lfStrikeOut = 0;
            public byte lfCharSet = 0;
            public byte lfOutPrecision = 0;
            public byte lfClipPrecision = 0;
            public byte lfQuality = 0;
            public byte lfPitchAndFamily = 0;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst=32)]
            public string lfFaceName = string.Empty;
        }
        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("user32.dll")]
        static extern bool ReleaseDC(IntPtr hWnd, IntPtr hDC);
        [DllImport("gdi32.dll")]
        static extern uint GetOutlineTextMetrics(IntPtr hdc, uint cbData, IntPtr ptrZero);
        [DllImport("gdi32.dll", EntryPoint = "SelectObject")]
        public static extern IntPtr SelectObject([In] IntPtr hdc, [In] IntPtr hgdiobj);

        private static void GetOutlineMetrics(IntPtr hdc)
        {
            uint cbBuffer = GetOutlineTextMetrics(hdc, 0, IntPtr.Zero);
            if (cbBuffer == 0)
                return;
            IntPtr buffer = Marshal.AllocHGlobal((int)cbBuffer);
            try
            {
                if (GetOutlineTextMetrics(hdc, cbBuffer, buffer) != 0)
                {
                    OUTLINETEXTMETRIC otm = new OUTLINETEXTMETRIC();
                    otm = (OUTLINETEXTMETRIC)Marshal.PtrToStructure(buffer, typeof(OUTLINETEXTMETRIC));
                    string otmpFamilyName = Marshal.PtrToStringAnsi(buffer + otm.otmpFamilyName.ToInt32());
                    string otmpFaceName = Marshal.PtrToStringAnsi(buffer + otm.otmpFaceName.ToInt32());
                    string otmpStyleName = Marshal.PtrToStringAnsi(buffer + otm.otmpStyleName.ToInt32());
                    string otmpFullName = Marshal.PtrToStringAnsi(buffer + otm.otmpFullName.ToInt32());
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        private IEnumerable<uint> checksums(System.IO.Stream stream)
        {
            byte[] buffer = new byte[6];
            stream.Read(buffer,0,buffer.Length);
            ushort numTables = BitConverter.ToUInt16(buffer,4);
            buffer = new byte[16*numTables];
            stream.Read(buffer,0,buffer.Length);
            for (int i=0;i<numTables;i++)
            {
                yield return BitConverter.ToUInt32(buffer,i*16+4);
            }
            yield break;
        }
        [DllImport("gdi32.dll")]
        private static extern uint GetFontData(IntPtr hdc, uint dwTable, uint dwOffset, [Out] byte[] lpvBuffer, uint cbData);

        public override string GetFontFile(string fontName, int fontSize, System.Drawing.FontStyle style)
        {
            IntPtr dc = GetDC(IntPtr.Zero);

            System.Drawing.Font fnt = new System.Drawing.Font("Arial", fontSize, style, System.Drawing.GraphicsUnit.Point);
            IntPtr hFont = fnt.ToHfont();
            SelectObject(dc, hFont);
            //GetOutlineMetrics(dc);

            //ReleaseDC(IntPtr.Zero, dc);

            var name = FontNameExtractor.GetFullFontName(dc);
            Console.WriteLine(name);
            return @"C:\Windows\Fonts\arial.ttf";
        }

        #endregion
	}
}

