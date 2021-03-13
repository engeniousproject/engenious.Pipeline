using System;
using engenious.Pipeline;

namespace engenious.Content.Serialization
{
    [ContentTypeWriterAttribute()]
    public class AudioContentTypeWriter : ContentTypeWriter<AudioContent>
    {
        public override void Write(ContentWriter writer, AudioContent? value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value), "Cannot write null AudioContent");
            writer.Write((byte)value.OutputFormat);
            writer.Write(value.Data);
        }

        public override string RuntimeReaderName => typeof(Texture2DTypeReader).FullName!;

        public AudioContentTypeWriter()
            : base(0)
        {
        }
    }
}