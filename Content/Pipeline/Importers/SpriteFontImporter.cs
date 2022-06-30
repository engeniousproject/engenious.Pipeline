using System;
using System.Buffers;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Xml;
using engenious.Content.Pipeline;
using engenious.Graphics;

namespace engenious.Pipeline
{
    /// <summary>
    ///     <see cref="ContentImporter{T}"/> used to import <see cref="SpriteFontContent"/> files from(.spritefont).
    /// </summary>
    [ContentImporter(".spritefont", DisplayName = "SpriteFontImporter", DefaultProcessor = "SpriteFontProcessor")]
    public class SpriteFontImporter : ContentImporter<SpriteFontContent>
    {
        #region implemented abstract members of ContentImporter

        /// <inheritdoc />
        public override SpriteFontContent Import(string filename, ContentImporterContext context)
        {
            return new(filename);
        }

        #endregion
    }

    /// <summary>
    ///     Sprite font content for engenious font content files.
    /// </summary>
    public class SpriteFontContent
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="SpriteFontContent"/> class.
        /// </summary>
        /// <param name="fileName">The filename of the spritefont xml file.</param>
        /// <exception cref="FormatException">Thrown when the spritefont xml was badly formatted.</exception>
        public SpriteFontContent(string fileName)
        {
            CharacterRegions = new List<CharacterRegion>();
            var doc = new XmlDocument();
            doc.Load(fileName);
            XmlElement? rootNode = null;
            foreach (var node in doc.ChildNodes.OfType<XmlElement>())
            {
                if (node.Name == "EngeniousFont")
                {
                    rootNode = node;
                    break;
                }
            }
            if (rootNode == null)
                throw new FormatException("Not a valid Spritefont file");
            foreach (XmlElement element in rootNode.ChildNodes.OfType<XmlElement>())
            {
                switch (element.Name)
                {
                    case "FontName":
                        FontName = element.InnerText;
                        break;
                    case "FontType":
                        if (!Enum.TryParse(typeof(SpriteFontType), element.InnerText, out var fontType))
                            throw new FormatException($"Invalid FontType: '{element.InnerText}'");
                        FontType = (SpriteFontType?) fontType ?? SpriteFontType.Default;
                        break;
                    case "Size":
                        Size = int.Parse(element.InnerText);
                        break;
                    case "Spacing":
                        Spacing = int.Parse(element.InnerText);
                        break;
                    case "UseKerning":
                        UseKerning = bool.Parse(element.InnerText);
                        break;
                    case "Style":
                        Style = parseStyle(element.InnerText);
                        break;
                    case "DefaultCharacter":
                        if (Rune.DecodeFromUtf16(element.InnerText, out var rune, out _) != OperationStatus.Done)
                            throw new FormatException("Rune not recognized");
                        DefaultCharacter = rune;
                        break;
                    case "CharacterRegions":
                        ParseCharacterRegion(element);
                        break;
                }
            }
        }

        /// <summary>
        ///     Save the spritefont file to a file.
        /// </summary>
        /// <param name="fileName">The file to save to.</param>
        public void Save(string fileName)
        {
            using var xml = new XmlTextWriter(fileName, Encoding.UTF8);
            xml.Formatting = Formatting.Indented;
            xml.WriteStartDocument();

            xml.WriteStartElement("EngeniousFont");

            xml.WriteElementString("FontName", FontName);
            xml.WriteElementString("FontType", FontType.ToString());
            xml.WriteElementString("Size", Size.ToString());
            xml.WriteElementString("Spacing", Spacing.ToString());
            xml.WriteElementString("UseKerning", UseKerning.ToString());
            xml.WriteElementString("Style", styleToString(Style));
            xml.WriteElementString("DefaultCharacter", DefaultCharacter.ToString());


            xml.WriteStartElement("CharacterRegions");

            foreach (var cr in CharacterRegions)
                cr.WriteToXml(xml);

            xml.WriteEndElement();


            xml.WriteEndElement();

            xml.WriteEndDocument();
        }

        private void ParseCharacterRegion(XmlElement rootNode)
        {
            foreach (var region in rootNode.ChildNodes.OfType<XmlElement>())
            {
                if (region.Name == "CharacterRegion")
                {
                    string? start = null, end = null;
                    foreach (XmlElement element in region.ChildNodes.OfType<XmlElement>())
                    {
                        switch (element.Name)
                        {
                            case "Start":
                                start = element.InnerText;
                                break;
                            case "End":
                                end = element.InnerText;
                                break;
                        }
                    }
                    if (start != null && end != null)
                    {
                        CharacterRegions.Add(new CharacterRegion(start, end, DefaultCharacter));
                    }
                }
            }
        }


        private string styleToString(FontStyle fontStyle)
        {
            if (fontStyle == FontStyle.Regular)
                return fontStyle.ToString();
            StringBuilder str = new StringBuilder();
            foreach (var style in Enum.GetValues(typeof(FontStyle)).OfType<FontStyle>())
            {
                if (style == FontStyle.Regular)
                    continue;
                if (fontStyle.HasFlag(style))
                {
                    if (str.Length > 0)
                        str.Append(' ');

                    str.Append(style.ToString());
                }
            }
            return str.ToString();
        }

        private FontStyle parseStyle(string styles)
        {
            FontStyle fontStyle = FontStyle.Regular;
            foreach (var style in styles.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries))
            {
                switch (style)
                {
                    case "Regular":
                        fontStyle |= FontStyle.Regular;
                        break;
                    case "Bold":
                        fontStyle |= FontStyle.Bold;
                        break;
                    case "Italic":
                        fontStyle |= FontStyle.Italic;
                        break;
                    case "Underline":
                        fontStyle |= FontStyle.Underline;
                        break;
                    case "Strikeout":
                        fontStyle |= FontStyle.Strikeout;
                        break;
                }
            }
            return fontStyle;
        }


        /// <summary>
        ///     Gets or sets the name of the font.
        /// </summary>
        public string? FontName { get; set; }
        
        /// <summary>
        ///     Gets or sets a value indicating the type of the font.
        /// </summary>
        public SpriteFontType FontType { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating the size of the font.
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating the spacing between characters.
        /// </summary>
        public int Spacing { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether kerning between characters should be used.
        /// </summary>
        public bool UseKerning { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating the style of the font.
        /// </summary>
        public FontStyle Style { get; set; }

        /// <summary>
        ///     Gets the default character to fall back to for characters that are not available.
        /// </summary>
        /// <remarks><c>null</c> defaults to '*'</remarks> TODO: change on global default
        public Rune? DefaultCharacter { get; }

        /// <summary>
        ///     Gets a list of the character regions this font contains.
        /// </summary>
        public List<CharacterRegion> CharacterRegions { get; }
    }

    /// <summary>
    ///     Represents a region of characters for fonts.
    /// </summary>
    public class CharacterRegion : IEquatable<CharacterRegion>
    {
        private static int ParseAddress(string characterAddress)
        {
            if (characterAddress.StartsWith("0x"))
                return Convert.ToInt32(characterAddress[2..], 16);
            return int.Parse(characterAddress);
        }

        private static Rune ToRune(int characterAddress)
        {
            //char[] value = Encoding.Unicode.GetChars(BitConverter.GetBytes(characterAddress));

            return new Rune(characterAddress);
        }

        private readonly Rune _defaultChar;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CharacterRegion"/> class.
        /// </summary>
        /// <param name="start">The first character the range of characters starts at.</param>
        /// <param name="end">The end character the range of characters ends at(inclusive).</param>
        /// <param name="defaultChar">The default character to use if a specific character is not available.</param>
        public CharacterRegion(string start, string end, Rune? defaultChar = null)
            : this(ParseAddress(start), ParseAddress(end), defaultChar)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="CharacterRegion"/> class.
        /// </summary>
        /// <param name="start">The first character the range of characters starts at.</param>
        /// <param name="end">The end character the range of characters ends at(inclusive).</param>
        /// <param name="defaultChar">The default character to use if a specific character is not available.</param>
        public CharacterRegion(int start, int end, Rune? defaultChar = null)
        {
            Start = start;
            End = end;
            _defaultChar = defaultChar ?? new Rune('*');
        }

        /// <summary>
        ///     Gets an enumeration of all the characters in the character range.
        /// </summary>
        /// <returns>The enumeration of the characters in the range.</returns>
        public IEnumerable<Rune> GetCharacters()
        {
            for (int i = Start; i <= End; i++)
            {
                yield return ToRune(i);
            }
        }

        internal void WriteToXml(XmlTextWriter xml)
        {
            xml.WriteStartElement("CharacterRegion");

            xml.WriteElementString("Start", Start.ToString());
            xml.WriteElementString("End", End.ToString());

            xml.WriteEndElement();
        }
        /// <summary>
        ///     Gets the first character the range of characters starts at.
        /// </summary>
        public int Start { get; }

        /// <summary>
        ///     Gets the end character the range of characters ends at(inclusive).
        /// </summary>
        public int End { get; }

        /// <inheritdoc />
        public bool Equals(CharacterRegion? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _defaultChar == other._defaultChar && Start == other.Start && End == other.End;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((CharacterRegion) obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Start;
                hashCode = (hashCode * 397) ^ End;
                return hashCode;
            }
        }
    }
}