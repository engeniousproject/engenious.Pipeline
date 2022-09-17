using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using engenious.Content.Serialization;
using SixLabors.ImageSharp;

namespace engenious.Content.Pipeline
{
    /// <summary>
    ///     Processor class used for creating texture content files from bitmaps.
    /// </summary>
    [ContentProcessor(DisplayName = "Bitmap Processor")]

    public class BitmapProcessor : ContentProcessor<Image, TextureContent,BitmapProcessorSettings>
    {
        #region implemented abstract members of ContentProcessor

        /// <inheritdoc />
        public override TextureContent Process(Image input, string filename, ContentProcessorContext context)
        {
            var game = (IGame)context.Game;
            TextureContent content = new TextureContent(game.GraphicsDevice,!_settings.AutoGenerateMipMaps,_settings.MipMapCount,input,TextureContentFormat.Png,_settings.Format);
            return content;
        }
        #endregion
    }

    /// <summary>
    ///     Settings used for influencing <see cref="BitmapProcessor"/>.
    /// </summary>
    [Serializable]
    public class BitmapProcessorSettings : ProcessorSettings
    {
        /// <summary>
        ///     Gets or sets a value indicating whether mip maps should be generated automatically.
        /// </summary>
        [DefaultValue(false)]
        public bool AutoGenerateMipMaps { get; set; } = false;
        /// <summary>
        ///     Gets a value indicating the number of mip maps to be generated.
        /// </summary>
        [DefaultValue(1)]
        public int MipMapCount { get; set; } = 1;

        /// <summary>
        ///     Gets or sets a value indicating the format to be used for saving the texture in the content file.
        /// </summary>
        [DefaultValue(TextureContentFormat.Png)]
        public TextureContentFormat Format { get; set; } = TextureContentFormat.Png;
    }
}

