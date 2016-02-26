using System;

namespace engenious.Content.Pipeline
{
    [ContentImporterAttribute(".fnt", DisplayName = "FontImporter Importer", DefaultProcessor = "FontProcessor")]
    public class FontImporter : ContentImporter<FontContent>
    {
        public FontImporter()
        {
        }

        public override FontContent Import(string filename, ContentImporterContext context)
        {
            string content = System.IO.File.ReadAllText(filename, System.Text.Encoding.Default);
            string toFind = "page id=0 file=\"";
            int start = content.IndexOf(toFind);
            if (start == -1)
                throw new Exception("Not a valid font file");
            int end = content.IndexOf('\"', start + toFind.Length);
            if (end == -1)
                throw new Exception("Not a valid font file");
            string texture = content.Substring(start + toFind.Length, end - (start + toFind.Length));
            start = content.IndexOf("common ");
            if (start == -1)
                throw new Exception("Not a valid font file");
            
            content = content.Substring(start);

            return new FontContent(filename, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(filename), texture), content);
        }
    }

    public class FontContent
    {
        public FontContent(string fileName, string textFile, string content)
        {
            FileName = fileName;
            TextureFile = textFile;
            Content = content;
        }

        public string FileName{ get; set; }

        public string TextureFile{ get; set; }

        public string Content{ get; set; }
    }
}
