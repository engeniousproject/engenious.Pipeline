using System;
using System.Drawing;

namespace engenious.Content.Pipeline
{
    /// <summary>
    ///     <see cref="ContentImporter{T}"/> used to import <see cref="Bitmap"/> files from(.bmp, .jpg, .png).
    /// </summary>
    [ContentImporter(".bmp", ".jpg", ".png", DisplayName = "Bitmap Importer", DefaultProcessor = "BitmapProcessor")]
    public class BitmapImporter : ContentImporter<Bitmap>
    {
        /// <inheritdoc />
        public override Bitmap? Import(string filename, ContentImporterContext context)
        {
            //if (!System.IO.File.Exists(filename))
            //    return null;
            try
            {
                return new Bitmap(filename);
            }
            catch (Exception ex)
            {
                context.RaiseBuildMessage(filename ,  ex.Message, BuildMessageEventArgs.BuildMessageType.Error);
            }
            return null;
        }
    }
}

