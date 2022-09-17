using System;
using SixLabors.ImageSharp;
namespace engenious.Content.Pipeline
{
    /// <summary>
    ///     <see cref="ContentImporter{T}"/> used to import <see cref="Image"/> files from(.bmp, .jpg, .png).
    /// </summary>
    [ContentImporter(".bmp", ".jpg", ".png", DisplayName = "Bitmap Importer", DefaultProcessor = "BitmapProcessor")]
    public class BitmapImporter : ContentImporter<Image>
    {
        /// <inheritdoc />
        public override Image? Import(string filename, ContentImporterContext context)
        {
            //if (!System.IO.File.Exists(filename))
            //    return null;
            try
            {
                return Image.Load(ImageSharpHelper.Config, filename);
            }
            catch (Exception ex)
            {
                context.RaiseBuildMessage(filename ,  ex.Message, BuildMessageEventArgs.BuildMessageType.Error);
            }
            return null;
        }
    }
}

