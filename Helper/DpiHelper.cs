using System.Drawing;

namespace engenious.Pipeline.Helper
{
    /// <summary>
    ///     Helper class to get dpi resolution.
    /// </summary>
    public static class DpiHelper
    {

        private static readonly Bitmap DummyBitmap;
        
        private static readonly System.Drawing.Graphics G;

        static DpiHelper()
        {
            DummyBitmap = new Bitmap(1,1);
            G = System.Drawing.Graphics.FromImage(DummyBitmap);
        }

        /// <summary>
        ///     Gets the graphics dpi resolution on the x axis.
        /// </summary>
        public static float DpiX => G.DpiX;

        /// <summary>
        ///     Gets the graphics dpi resolution on the y axis.
        /// </summary>
        public static float DpiY => G.DpiY;
    }
}