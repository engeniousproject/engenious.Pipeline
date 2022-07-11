using System;
using engenious.Content.Pipeline;
using engenious.Graphics;

namespace engenious.Content.Serialization
{
    /// <summary>
    ///     Content type writer to serialize engenious spritefont content.
    /// </summary>
    [ContentTypeWriter]
    public class SpriteFontTypeWriter : ContentTypeWriter<CompiledSpriteFont>
    {
        private void Write(ContentWriter writer, FontGlyph glyph)
        {
            writer.Write(glyph.TextureRegion.X);
            writer.Write(glyph.TextureRegion.Y);
            writer.Write(glyph.TextureRegion.Width);
            writer.Write(glyph.TextureRegion.Height);
            
            writer.Write(glyph.Offset);
            writer.Write(glyph.Size);
            writer.Write(glyph.Color is not null);
            if (glyph.Color is not null)
                writer.Write(glyph.Color.Value);
        }
        /// <inheritdoc />
        public override void Write(ContentWriter writer, CompiledSpriteFont? value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value), "Cannot write null CompiledSpriteFont");
            writer.WriteObject(value.Texture);

            writer.Write(value.Spacing);
            writer.Write(value.LineSpacing);
            writer.Write(value.BaseLine);
            writer.Write((byte)value.FontType);
            writer.Write(value.DefaultCharacter.HasValue);
            if (value.DefaultCharacter.HasValue)
                writer.Write(value.DefaultCharacter.Value);

            writer.Write(value.Kernings.Count);
            foreach (var kerning in value.Kernings)
            {
                writer.Write(kerning.Key.First);
                writer.Write(kerning.Key.Second);
                writer.Write(kerning.Value);
            }
            writer.Write(value.CharacterMap.Count);
            foreach (var character in value.CharacterMap)
            {
                writer.Write(character.Key);
                writer.Write(character.Value.Advance);
                Write(writer, character.Value.Glyph);
                writer.Write(character.Value.GlyphLayers.Length);
                foreach(var layer in character.Value.GlyphLayers)
                    Write(writer, layer);
            }
        }

        /// <inheritdoc />
        public override string RuntimeReaderName => typeof(SpriteFontTypeReader).FullName!;

        /// <summary>
        ///     Initializes a new instance of the <see cref="SpriteFontTypeWriter"/> class.
        /// </summary>
        public SpriteFontTypeWriter()
            : base(2)
        {
        }
    }
}

