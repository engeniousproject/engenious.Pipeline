using System;
using engenious.Content.Pipeline;

namespace engenious.Content.Serialization
{
    /// <summary>
    ///     Content type writer to serialize engenious texture content.
    /// </summary>
    [ContentTypeWriterAttribute()]
    public class TextureContentTypeWriter : ContentTypeWriter<TextureContent>
    {
        #region implemented abstract members of ContentTypeWriter

        /// <inheritdoc />
        public override void Write(ContentWriter writer, TextureContent? value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value), "Cannot write null TextureContent");
            writer.Write(value.GenerateMipMaps);
            writer.Write(value.MipMapCount);
            foreach(var map in value.MipMaps)
            {
                map.Save(writer);
            }
        }

        /// <inheritdoc />
        public override string RuntimeReaderName => typeof(Texture2DTypeReader).FullName!;

        #endregion

        /// <summary>
        ///     Initializes a new instance of the <see cref="TextureContentTypeWriter"/> class.
        /// </summary>
        public TextureContentTypeWriter()
            : base(0)
        {
        }
    }
}

