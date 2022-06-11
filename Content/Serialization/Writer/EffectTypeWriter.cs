using System;
using System.IO;
using engenious.Content.Pipeline;

namespace engenious.Content.Serialization
{
    /// <summary>
    ///     Content type writer to serialize engenious effects content.
    /// </summary>
    [ContentTypeWriter]
    public class EffectTypeWriter : ContentTypeWriter<EffectContent>
    {
        #region implemented abstract members of ContentTypeWriter

        private static int CountLines(string source)
        {
            int curInd = -1;
            var count = 0;
            while (true)
            {
                curInd = source.IndexOf('\n', curInd + 1);
                if (curInd == -1)
                    break;
                count++;
            }

            return count;
        }
        private static (int headLineCount, string head, string source) ProcessShader(string source)
        {
            int versionPos = source.IndexOf("#version", StringComparison.Ordinal);
            if (versionPos == -1)
            {
                return (1, "#version {0}\r\n", source);
            }
            var newLinePos = source.IndexOf('\n', versionPos);
            if (newLinePos == -1)
                newLinePos = source.Length - 1;
            var head = source[..(newLinePos + 1)];
            source = source[(newLinePos + 1)..];

            return (CountLines(head) + 1, head, source);
        }

        /// <inheritdoc />
        public override void Write(ContentWriter writer, EffectContent? value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value), "Cannot write null EffectContent");
            writer.Write(value.CreateUserEffect);
            if (value.CreateUserEffect)
            {
                writer.Write(value.UserEffectName ?? throw new Exception("UserEffectName should not be null"));
            }
            writer.Write(value.Techniques.Count);
            foreach (var technique in value.Techniques)
            {
                writer.Write(technique.Name);
                if (value.CreateUserEffect)
                {
                    writer.Write(technique.UserTechniqueName ?? string.Empty);
                }
                writer.Write(technique.Passes.Count);
                foreach (var pass in technique.Passes)
                {
                    writer.Write(pass.Name);

                    writer.WriteObject(pass.BlendState);
                    writer.WriteObject(pass.DepthStencilState);
                    writer.WriteObject(pass.RasterizerState);
                    writer.Write((byte)pass.Shaders.Count);
                    foreach (var shader in pass.Shaders)
                    {
                        writer.Write((ushort)shader.Key);
                        var (headLineCount, head, source) = ProcessShader(File.ReadAllText(shader.Value));
                        writer.Write(headLineCount);
                        writer.Write(head);
                        writer.Write(source);
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

        /// <inheritdoc />
        public override string RuntimeReaderName => typeof(EffectTypeReader).FullName!;

        #endregion

        /// <summary>
        ///     Initializes a new instance of the <see cref="EffectTypeWriter"/> class.
        /// </summary>
        public EffectTypeWriter()
            : base(1)
        {
        }
    }
}

