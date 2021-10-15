using System;
using System.IO;
using System.Text;

namespace engenious.Content.Pipeline
{    /// <summary>
    ///     Deprecated <see cref="ContentImporter{T}"/> used to import <see cref="FontContent"/> files from(.fnt).
    /// </summary>
    [ContentImporter(".fnt", DisplayName = "FontImporter", DefaultProcessor = "FontProcessor")]
    public class FontImporter : ContentImporter<FontContent>
    {
        /// <inheritdoc />
        public override FontContent? Import(string filename, ContentImporterContext context)
        {
            try
            {
                string content = File.ReadAllText(filename, Encoding.UTF8);
                string toFind = "page id=0 file=\"";
                int start = content.IndexOf(toFind, StringComparison.Ordinal);
                if (start == -1)
                    throw new Exception("Not a valid font file");
                int end = content.IndexOf('\"', start + toFind.Length);
                if (end == -1)
                    throw new Exception("Not a valid font file");
                string texture = content.Substring(start + toFind.Length, end - (start + toFind.Length));
                start = content.IndexOf("common ", StringComparison.Ordinal);
                if (start == -1)
                    throw new Exception("Not a valid font file");

                content = content.Substring(start);

                var dirName = Path.GetDirectoryName(filename);

                return new FontContent(filename, dirName == null ? texture : Path.Combine(dirName, texture), content);
            }
            catch (Exception ex)
            {
                context.RaiseBuildMessage(filename,ex.Message, BuildMessageEventArgs.BuildMessageType.Error);
            }
            return null;
        }
    }

    /// <summary>
    ///     Class for content of processing fnt files to content files.
    /// </summary>
    public class FontContent
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="FontContent"/> class.
        /// </summary>
        /// <param name="fileName">The filename of the character region mappings of the font.</param>
        /// <param name="textFile">The filename of the bitmap texture of the font.</param>
        /// <param name="content">The content of the loaded <paramref name="fileName"/>.</param>
        public FontContent(string fileName, string textFile, string content)
        {
            FileName = fileName;
            TextureFile = textFile;
            Content = content;
        }

        /// <summary>
        ///     Gets the filename of the character region mappings of the font.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        ///     Gets the filename of the bitmap texture of the font.
        /// </summary>
        public string TextureFile { get; }

        /// <summary>
        ///     Gets the content of the loaded <see cref="FileName"/>.
        /// </summary>
        public string Content { get; }
    }
}
