﻿using System;
using engenious.Content.Pipeline;

namespace engenious.Content.Serialization
{
    [ContentTypeWriter]
    public class SpriteFontTypeWriter : ContentTypeWriter<CompiledSpriteFont>
    {
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
                writer.Write(kerning.Key);
                writer.Write(kerning.Value);
            }
            writer.Write(value.CharacterMap.Count);
            foreach (var character in value.CharacterMap)
            {
                writer.Write(character.Key);
                writer.Write(character.Value.Offset);
                writer.Write(character.Value.Size);
                writer.Write(character.Value.TextureRegion.X);
                writer.Write(character.Value.TextureRegion.Y);
                writer.Write(character.Value.TextureRegion.Width);
                writer.Write(character.Value.TextureRegion.Height);
                writer.Write(character.Value.Advance);
            }
        }

        public override string RuntimeReaderName => typeof(SpriteFontTypeReader).FullName!;

        public SpriteFontTypeWriter() : base(1)
        {
        }
    }
}

