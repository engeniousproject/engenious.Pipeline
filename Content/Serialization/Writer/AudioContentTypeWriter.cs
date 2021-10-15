using System;
using engenious.Pipeline;

namespace engenious.Content.Serialization
{
    /// <summary>
    ///     Content type writer to serialize engenious audio content.
    /// </summary>
    [ContentTypeWriterAttribute()]
    public class AudioContentTypeWriter : ContentTypeWriter<AudioContent>
    {
        /// <inheritdoc />
        public override void Write(ContentWriter writer, AudioContent? value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value), "Cannot write null AudioContent");
            writer.Write((byte)value.OutputFormat);
            writer.Write(value.Data);
        }

        /// <inheritdoc />
        public override string RuntimeReaderName => typeof(Texture2DTypeReader).FullName!;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AudioContentTypeWriter"/> class.
        /// </summary>
        public AudioContentTypeWriter()
            : base(0)
        {
        }
    }
}