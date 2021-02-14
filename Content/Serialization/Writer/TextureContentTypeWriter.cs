using System;
using engenious.Content.Pipeline;

namespace engenious.Content.Serialization
{
    [ContentTypeWriterAttribute()]
    public class TextureContentTypeWriter : ContentTypeWriter<TextureContent>
    {
        #region implemented abstract members of ContentTypeWriter

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

        public override string RuntimeReaderName => typeof(Texture2DTypeReader).FullName!;

        #endregion
    }
}

