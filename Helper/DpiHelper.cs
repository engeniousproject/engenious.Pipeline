using System.Drawing;

namespace engenious.Pipeline.Helper
{
    public static class DpiHelper
    {

        private static readonly Bitmap _dummyBitmap;
        
        private static readonly System.Drawing.Graphics _g;

        static DpiHelper()
        {
            _dummyBitmap = new Bitmap(1,1);
            _g = System.Drawing.Graphics.FromImage(_dummyBitmap);
        }

        public static float DpiX => _g.DpiX;
        public static float DpiY => _g.DpiY;
    }
}