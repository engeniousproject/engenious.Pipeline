using System;
using engenious.Content.Pipeline;
using engenious.Graphics;

namespace engenious.Content.Serialization
{
    [ContentTypeWriterAttribute()]
    public class EffectTypeWriter : ContentTypeWriter<EffectContent>
    {
        public EffectTypeWriter()
        {
        }

        #region implemented abstract members of ContentTypeWriter

        public override void Write(ContentWriter writer, EffectContent value)
        {
            writer.Write(value.Techniques.Count);
            foreach (var technique in value.Techniques)
            {
                writer.Write(technique.Name);
                writer.Write(technique.Passes.Count);
                foreach (var pass in technique.Passes)
                {
                    writer.Write(pass.Name);

                    writer.WriteObject<BlendState>(pass.BlendState);
                    writer.WriteObject<DepthStencilState>(pass.DepthStencilState);
                    writer.WriteObject<RasterizerState>(pass.RasterizerState);
                    writer.Write((byte)pass.Shaders.Count);
                    foreach (var shader in pass.Shaders)
                    {
                        writer.Write((ushort)shader.Key);
                        writer.Write(shader.Value);
                    }

                    writer.Write((byte)pass.Attributes.Count);
                    foreach (var attr in pass.Attributes)
                    {
                        writer.Write((byte)attr.Key);
                        writer.Write(attr.Value);
                    }
                }
            }
        }

        public override string RuntimeReaderName
        {
            get
            {
                return typeof(EffectTypeReader).FullName;
            }
        }

        #endregion
    }
}

