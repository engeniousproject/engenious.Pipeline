using System;
using System.IO;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace engenious.Content.Serialization
{
    /// <summary>
    ///     Content type writer to serialize engenious bitmap content.
    /// </summary>
    [ContentTypeWriter]
    public class BitmapTypeWriter : ContentTypeWriter<Image>
    {
        private readonly bool _usePng = true;

        /// <inheritdoc />
        public override void Write(ContentWriter writer, Image? bmp)
        {
            if (bmp == null)
                throw new ArgumentNullException(nameof(bmp), "Cannot write null Bitmap");
            if (_usePng)
            {
                writer.Write((byte)1);
                using var str = new MemoryStream();
                bmp.Save(str, new PngEncoder());

                writer.Write((int)str.Position);
                str.Position = 0;
                writer.Write(str);
            }
            else
            {
                writer.Write((byte)0);
                writer.Write(bmp.Width);
                writer.Write(bmp.Height);
                var memory = bmp.ToContinuousImage<Rgba32>().GetPixelMemoryGroup();
                foreach (var row in memory)
                {
                    var span = row.Span;
                    foreach (var i in span)
                        writer.Write(i.Rgba);
                }
            }
        }

        /// <inheritdoc />
        public override string RuntimeReaderName => typeof(Texture2DTypeReader).FullName!;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BitmapTypeWriter"/> class.
        /// </summary>
        public BitmapTypeWriter()
            : base(0)
        {
        }
    }
}

