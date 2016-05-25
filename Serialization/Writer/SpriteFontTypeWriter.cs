using System;
using engenious.Graphics;
using engenious.Content.Pipeline;

namespace engenious.Content.Serialization
{
    [ContentTypeWriterAttribute()]
    public class SpriteFontTypeWriter : ContentTypeWriter<CompiledSpriteFont>
    {
        public SpriteFontTypeWriter()
        {

        }


        public override void Write(ContentWriter writer, CompiledSpriteFont value)
        {
            writer.WriteObject<TextureContent>(value.texture);

            writer.Write(value.Spacing);
            writer.Write(value.LineSpacing);
            writer.Write(value.BaseLine);
            writer.Write(value.DefaultCharacter.HasValue);
            if (value.DefaultCharacter.HasValue)
                writer.Write(value.DefaultCharacter.Value);

            writer.Write(value.kernings.Count);
            foreach (var kerning in value.kernings)
            {
                writer.Write(kerning.Key);
                writer.Write(kerning.Value);
            }
            writer.Write(value.characterMap.Count);
            foreach (var character in value.characterMap)
            {
                writer.Write(character.Key);
                writer.Write(character.Value.Offset);
                writer.Write(character.Value.TextureRegion.X);
                writer.Write(character.Value.TextureRegion.Y);
                writer.Write(character.Value.TextureRegion.Width);
                writer.Write(character.Value.TextureRegion.Height);
                writer.Write(character.Value.Advance);
            }
        }

        public override string RuntimeReaderName{ get { return typeof(SpriteFontTypeReader).FullName; } }
    }
}

