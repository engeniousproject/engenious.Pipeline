using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Drawing.Text;
using System.Drawing;
using System.Text;
using System.Collections.Generic;

namespace engenious.Pipeline
{
    /// Helper to enable loading the full font name as specified in TrueType font specs.
    /// http://www.microsoft.com/typography/otspec/name.htm
    /// </summary>
    internal static class FontNameExtractor
    {
        public static Dictionary<int,string> langs= new Dictionary<int, string>(){{0x0,"Default"},{0x0436 ,"af-ZA"},
            {0x041C ,"sq-AL"},
            {0x0484 ,"Alsatian-FR"},
            {0x045E ,"Amharic-Ethiopia"},
            {0x1401 ,"ar-DZ"},
            {0x3C01 ,"ar-BH"},
            {0x0C01 ,"ar-EG"},
            {0x0801 ,"ar-IQ"},
            {0x2C01 ,"ar-JO"},
            {0x3401 ,"ar-KW"},
            {0x3001 ,"ar-LB"},
            {0x1001 ,"ar-LY"},
            {0x1801 ,"ar-MA"},
            {0x2001 ,"ar-OM"},
            {0x4001 ,"ar-QA"},
            {0x0401 ,"ar-SA"},
            {0x2801 ,"ar-SY"},
            {0x1C01 ,"ar-TN"},
            {0x3801 ,"ar-AE"},
            {0x2401 ,"ar-YE"},
            {0x042B ,"hy-AM"},
            {0x044D ,"Assamese-IN"},
            {0x082C ,"az-AZ"},
            {0x042C ,"az-AZ"},
            {0x046D ,"Bashkir-Russia"},
            {0x042D ,"eu-ES"},
            {0x0423 ,"Belarusian-Belarus"},
            {0x0845 ,"Bengali-Bangladesh"},
            {0x0445 ,"Bengali-IN"},
            {0x201A ,"Bosnian-(Cyrillic)-en-BA"},
            {0x141A ,"Bosnian-(Latin)-en-BA"},
            {0x047E ,"Breton-FR"},
            {0x0402 ,"Bulgarian-Bulgaria"},
            {0x0403 ,"Catalan-Catalan"},
            {0x0C04 ,"zh-HK"},
            {0x1404 ,"zh-MO"},
            {0x0804 ,"zh-CN"},
            {0x1004 ,"zh-SG"},
            {0x0404 ,"zh-TW"},
            {0x0483 ,"Corsican-FR"},
            {0x041A ,"hr-HR"},
            {0x101A ,"hr-(Latin)-en-BA"},
            {0x0405 ,"Czech-Czech-Republic"},
            {0x0406 ,"da-DK"},
            {0x048C ,"Dari-Afghanistan"},
            {0x0465 ,"Divehi-Maldives"},
            {0x0813 ,"nl-BE"},
            {0x0413 ,"nl-NL"},
            {0x0C09 ,"en-AU"},
            {0x2809 ,"en-BZ"},
            {0x1009 ,"en-CA"},
            {0x2409 ,"en-CB"},
            {0x4009 ,"en-IN"},
            {0x1809 ,"en-IE"},
            {0x2009 ,"en-JM"},
            {0x4409 ,"en-Malaysia"},
            {0x1409 ,"en-NZ"},
            {0x3409 ,"en-PH"},
            {0x4809 ,"en-Singapore"},
            {0x1C09 ,"en-ZA"},
            {0x2C09 ,"en-TT"},
            {0x0809 ,"en-GB"},
            {0x0409 ,"en-US"},
            {0x3009 ,"en-ZW"},
            {0x0425 ,"et-EE"},
            {0x0438 ,"Faroese-Faroe-Islands"},
            {0x0464 ,"Filipino-Philippines"},
            {0x040B ,"fi-FI"},
            {0x080C ,"fr-Belgium"},
            {0x0C0C ,"fr-Canada-CC"},
            {0x040C ,"fr-FR"},
            {0x140c ,"fr-Luxembourg-c"},
            {0x180C ,"fr-Principality-of-Monoco"},
            {0x100C ,"fr-Switzerland"},
            {0x0462 ,"Frisian-Netherlands"},
            {0x0456 ,"gl-ES"},
            {0x0437 ,"Georgian-Georgia"},
            {0x0C07 ,"de-Austria"},
            {0x0407 ,"de-DE"},
            {0x1407 ,"de-Liechtenstein"},
            {0x1007 ,"de-Luxembourg"},
            {0x0807 ,"de-Switzerland"},
            {0x0408 ,"el-GR"},
            {0x046F ,"Greenlandic-Greenland"},
            {0x0447 ,"gu-IN"},
            {0x0468 ,"Hausa-(Latin)-Nigeria"},
            {0x040D ,"Hebrew-Israel"},
            {0x0439 ,"Hindi-IN"},
            {0x040E ,"Hungarian-Hungary"},
            {0x040F ,"Icelandic-Iceland"},
            {0x0470 ,"Igbo-Nigeria"},
            {0x0421 ,"Indonesian-Indonesia"},
            {0x045D ,"Inuktitut-Canada"},
            {0x085D ,"Inuktitut-(Latin)-Canada"},
            {0x083C ,"Irish-Ireland"},
            {0x0434 ,"isiXhosa-South-Africa"},
            {0x0435 ,"isiZulu-South-Africa"},
            {0x0410 ,"Italian-Italy"},
            {0x0810 ,"Italian-Switzerland"},
            {0x0411 ,"Japanese-Japan"},
            {0x044B ,"Kannada-IN"},
            {0x043F ,"Kazakh-Kazakhstan"},
            {0x0453 ,"Khmer-Cambodia"},
            {0x0486 ,"K'iche-Guatemala"},
            {0x0487 ,"Kinyarwanda-Rwanda"},
            {0x0441 ,"Kiswahili-Kenya"},
            {0x0457 ,"Konkani-IN"},
            {0x0412 ,"Korean-Korea"},
            {0x0440 ,"Kyrgyz-Kyrgyzstan"},
            {0x0454 ,"Lao-Lao-P.D.R."},
            {0x0426 ,"Latvian-Latvia"},
            {0x0427 ,"Lithuanian-Lithuania"},
            {0x082E ,"Lower-Sorbian-DE"},
            {0x046E ,"Luxembourgish-Luxembourg"},
            {0x042F ,"Macedonian-(FYROM)-Former-Yugoslav-Republic-of-Macedonia"},
            {0x083E ,"Malay-Brunei-Darussalam"},
            {0x043E ,"Malay-Malaysia"},
            {0x044C ,"Malayalam-IN"},
            {0x043A ,"Maltese-Malta"},
            {0x0481 ,"Maori-New-Zealand"},
            {0x047A ,"Mapudungun-Chile"},
            {0x044E ,"Marathi-IN"},
            {0x047C ,"Mohawk-Mohawk"},
            {0x0450 ,"Mongolian-(Cyrillic)-Mongolia"},
            {0x0850 ,"Mongolian-(Traditional)-People's-Republic-of-China"},
            {0x0461 ,"Nepali-Nepal"},
            {0x0414 ,"Norwegian-(Bokmal)-Norway"},
            {0x0814 ,"Norwegian-(Nynorsk)-Norway"},
            {0x0482 ,"Occitan-FR"},
            {0x0448 ,"Odia-(formerly-Oriya)-IN"},
            {0x0463 ,"Pashto-Afghanistan"},
            {0x0415 ,"Polish-Poland"},
            {0x0416 ,"Portuguese-Brazil"},
            {0x0816 ,"Portuguese-Portugal"},
            {0x0446 ,"Punjabi-IN"},
            {0x046B ,"Quechua-Bolivia"},
            {0x086B ,"Quechua-Ecuador"},
            {0x0C6B ,"Quechua-Peru-CB"},
            {0x0418 ,"Romanian-Romania"},
            {0x0417 ,"Romansh-Switzerland"},
            {0x0419 ,"Russian-Russia"},
            {0x243B ,"se-FI"},
            {0x103B ,"se-NO"},
            {0x143B ,"se-SE"},
            {0x0C3B ,"se-FI"},
            {0x043B ,"se-NO"},
            {0x083B ,"se-SE"},
            {0x203B ,"se-FI"},
            {0x183B ,"se-NO"},
            {0x1C3B ,"se-SE"},
            {0x044F ,"Sanskrit-IN"},
            {0x1C1A ,"sr-BA"},
            {0x0C1A ,"sr-SP"},
            {0x181A ,"sr-BA"},
            {0x081A ,"sr-SP"},
            {0x046C ,"Sesotho-sa-Leboa-South-Africa"},
            {0x0432 ,"Setswana-South-Africa"},
            {0x045B ,"Sinhala-Sri-Lanka"},
            {0x041B ,"Slovak-Slovakia"},
            {0x0424 ,"Slovenian-Slovenia"},
            {0x2C0A ,"es-AR"},
            {0x400A ,"es-BO"},
            {0x340A ,"es-CL"},
            {0x240A ,"es-CO"},
            {0x140A ,"es-CR"},
            {0x1C0A ,"es-DO"},
            {0x300A ,"es-EC"},
            {0x440A ,"es-SV"},
            {0x100A ,"es-GT"},
            {0x480A ,"es-HN"},
            {0x080A ,"es-MX"},
            {0x4C0A ,"es-NI"},
            {0x180A ,"es-PA"},
            {0x3C0A ,"es-PY"},
            {0x280A ,"es-PE"},
            {0x500A ,"es-PR"},
            {0x0C0A ,"es-(Modern-Sort)-Spain-CA"},
            {0x040A ,"es-(Traditional-Sort)-Spain"},
            {0x540A ,"es-US"},
            {0x380A ,"es-UY"},
            {0x200A ,"es-VE"},
            {0x081D ,"Sweden-Finland"},
            {0x041D ,"Swedish-Sweden"},
            {0x045A ,"Syriac-Syria"},
            {0x0428 ,"Tajik-(Cyrillic)-Tajikistan"},
            {0x085F ,"Tamazight-(Latin)-Algeria"},
            {0x0449 ,"Tamil-IN"},
            {0x0444 ,"Tatar-Russia"},
            {0x044A ,"Telugu-IN"},
            {0x041E ,"Thai-Thailand"},
            {0x0451 ,"Tibetan-PRC"},
            {0x041F ,"Turkish-Turkey"},
            {0x0442 ,"Turkmen-Turkmenistan"},
            {0x0480 ,"Uighur-PRC"},
            {0x0422 ,"Ukrainian-Ukraine"},
            {0x042E ,"Upper-Sorbian-DE"},
            {0x0420 ,"Urdu-Islamic-Republic-of-Pakistan"},
            {0x0843 ,"Uzbek-(Cyrillic)-Uzbekistan"},
            {0x0443 ,"Uzbek-(Latin)-Uzbekistan"},
            {0x042A ,"Vietnamese-Vietnam"},
            {0x0452 ,"Welsh-United-Kingdom"},
            {0x0488 ,"Wolof-Senegal"},
            {0x0485 ,"Yakut-Russia"},
            {0x0478 ,"Yi-PRC"},
            {0x046A ,"Yoruba-Nigeria-A"}};
        #region PInvoke gdi32.dll

        /// <summary>
        /// Selects the graphics object (here a font handle) into the device context.
        /// </summary>
        /// <param name="hdc">A handle to the device context.</param>
        /// <param name="hgdiobj">A handle to the object to be selected.
        ///  The specified object must have been created by using one of the following functions.</param>
        /// <returns>If the selected object is not a region and the function succeeds, the return value is a handle to the object being replaced.
        /// If an error occurs and the selected object is not a region, the return value is NULL. Otherwise, it is HGDI_ERROR.</returns>
        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        /// <summary>
        /// Deletes a logical pen, brush, font, bitmap, region, or palette handle freeing all system resources associated with the object.
        /// After the object is deleted, the specified handle is no longer valid.
        /// </summary>
        /// <param name="hgdiobj">The handle to the logical GDI object.</param>
        /// <returns><c>true</c>, if the handle to the GDI object was successfully released; otherwise <c>false</c> is returned.</returns>
        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hgdiobj);

        /// <summary>
        /// Gets the font data from specified font metric table to be accessed by key defined as <paramref name="dwTable"/>.
        /// </summary>
        /// <param name="hdc">The device context, the font file was loaded into.
        ///  Use <see cref="SelectObject"/> for loading the font into a device context.</param>
        /// <param name="dwTable">The key of the font metric table as <see cref="uint"/>
        /// assembled by ASCII HEX code of the single characters of the table in reversed order.
        /// <code>uint table = 0x656D616E</code>, represents the font table keyword character sequence 'name' in reversed order (i.e. 'eman').</param>
        /// <param name="dwOffset">Specifies the dwOffset from the beginning of the font metric table to the location where the function should begin retrieving information.
        /// If this parameter is zero, the information is retrieved starting at the beginning of the table specified by the <paramref name="dwTable"/> parameter.
        /// If this value is greater than or equal to the size of the table, an error occurs.</param>
        /// <param name="lpvBuffer">Points to a lpvBuffer to receive the font information. If this parameter is NULL, the function returns the size of the <paramref name="lpvBuffer"/> required for the font data.</param>
        /// <param name="cbData">Specifies the length, in bytes, of the information to be retrieved.
        /// If this parameter is zero, GetFontData returns the size of the font metrics table specified in the <paramref name="dwTable"/> parameter. </param>
        /// <returns>If the function succeeds, the return value is the number of bytes returned. If the function fails, the return value is GDI_ERROR.</returns>
        [DllImport("gdi32.dll")]
        private static extern uint GetFontData(IntPtr hdc, uint dwTable, uint dwOffset, [Out] byte[] lpvBuffer, uint cbData);

        #endregion

        private static uint nameTableKey = 0x656D616E;   // Represents the ASCII character sequence 'eman' => reversed name table key 'name'

        /// <summary>
        /// Gets the full name directly out of specified True Type font file.
        /// </summary>
        /// <param name="fontFile">The True Type font file.</param>
        /// <returns>The full font name.</returns>
        internal static string GetFullFontName(IntPtr hFont)
        {
            string fontName = string.Empty;

            byte[] fontData = LoadFontMetricsNameTable(hFont);

            fontName = ExtractFullName(fontData);


            return fontName;
        }

        #region Private Helper

        /// <summary>
        /// Extracts the full font name from raw bytes.
        /// </summary>
        /// <param name="fontData">The font data as raw bytes.</param>
        /// <returns>The extracted full font name.</returns>
        private static string ExtractFullName(byte[] fontData)
        {
            string fontName = string.Empty;

            using (BinaryReader br = new BinaryReader(new MemoryStream(fontData)))
            {
                // Read selector (always = 0) to advance reader position by 2 bytes

                ushort selector = ToLittleEndian(br.ReadUInt16());

                // Get number of records and offset byte value, from where font descriptions start

                ushort records = ToLittleEndian(br.ReadUInt16());
                ushort offset = ToLittleEndian(br.ReadUInt16());

                // Get the correct name record

                NameRecord nameRecord = SeekCorrectNameRecord(br, records);

                if (nameRecord != null)
                {
                    // Get the full font name for the record

                    fontName = nameRecord.ProvideFullName(br, offset);
                }
                else
                {
                    //TODO: Exception handling
                }

                br.Close();
            }

            return fontName;
        }

        /// <summary>
        /// Seeks the correct <see cref="NameRecord"/>.
        /// </summary>
        /// <param name="br">The <see cref="BinaryReader"/> to be used for reading the font metrics name table.</param>
        /// <param name="recordCount">The count of name records in the font metrics name table.</param>
        /// <returns>The <see cref="NameRecord"/> providing access to the correct full font name.</returns>
        private static NameRecord SeekCorrectNameRecord(BinaryReader br, int recordCount)
        {
            for (int i = 0; i < recordCount; i++)
            {
                NameRecord record = new NameRecord(br);

                if (record.IsWindowsUnicodeFullFontName && FontNameExtractor.langs[record.languageId].Contains(System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName))
                {
                    return record;
                }
            }

            return null;
        }
        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hWnd);
        /// <summary>
        /// Loads the font metrics name table as raw bytes from the font in the <paramref name="fontCollection"/>.
        /// </summary>
        /// <param name="fontCollection">The font <see cref="PrivateFontCollection"/>.</param>
        /// <returns>The metrics table as raw bytes.</returns>
        public static byte[] LoadFontMetricsNameTable(IntPtr dc)
        {
            byte[] fontData = null;

            // First determine the amount of bytes in the font metrics name table

            uint byteCount = GetFontData(dc, nameTableKey, 0, fontData, 0);
            // Now init the byte array and load the data by calling GetFontData once again

            fontData = new byte[byteCount];
            GetFontData(dc, nameTableKey, 0, fontData, byteCount);

            return fontData;
        }

        /// <summary>
        /// Helper function to convert any <see cref="ushort"/> value in font metrics name table to little endian,
        ///  because all stored in big endian.
        /// </summary>
        /// <param name="value">The value to be converted into little endian byte order.</param>
        /// <returns>The corresponding <see cref="ushort"/> value in big endian byte order.</returns>
        private static ushort ToLittleEndian(ushort value)
        {
            if (BitConverter.IsLittleEndian)
            {
                byte[] bytes = BitConverter.GetBytes(value);
                Array.Reverse(bytes);

                return BitConverter.ToUInt16(bytes, 0);
            }

            return value;
        }

        /// <summary>
        /// Converts the raw bytes to a <see cref="string"/> by using UTF-16BE Encoding (code page 1201).
        /// </summary>
        /// <param name="bytes">The raw bytes.</param>
        /// <returns>The converted <see cref="string"/>.</returns>
        private static string BytesToString(byte[] bytes)
        {
            // Use UTF-16BE (Unicode big endian) code page

            return Encoding.GetEncoding(1201).GetString(bytes);
        }

        #endregion

        #region TTF NameRecord Class

        /// <summary>
        /// Encapsulates a name record as specified in True Type font specification for the font metrics name table
        /// http://www.microsoft.com/typography/otspec/name.htm
        /// </summary>
        private class NameRecord
        {
           
            private ushort platformId;
            private ushort encodingId;
            public ushort languageId;
            private ushort nameId;

            /// <summary>
            /// Gets length of the full font name as specified in the font file.
            /// </summary>
            /// <value>
            /// The length of the full font name name.
            /// </value>
            internal ushort NameLength { get; private set; }

            /// <summary>
            /// Gets the byte offset value, where the full font name information is stored within the font metrics table .
            /// </summary>
            /// <value>
            /// The byte offset value.
            /// </value>
            internal ushort ByteOffset { get; private set; }

            /// <summary>
            /// Gets a value indicating whether this <see cref="NameRecord"/> represents a Windows Unicode full font name.
            /// </summary>
            /// <value>
            ///     <c>true</c> if this <see cref="NameRecord"/> represents a Windows Unicode full font name; otherwise, <c>false</c>.
            /// </value>
            internal bool IsWindowsUnicodeFullFontName
            {
                get
                {
                    // platformId = 3 => Windows
                    // encodingId = 1 => Unicode BMP (UCS-2)
                    // nameId = 4 => full font name

                    return platformId == 3 && encodingId == 1 && nameId == 4;
                }
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="NameRecord"/> class.
            /// </summary>
            /// <param name="br">The <see cref="BinaryReader"/> for interpretation of the bytes.</param>
            internal NameRecord(BinaryReader br)
            {
                // Read the unsigned 16-bit integers and convert to little endian

                platformId = ToLittleEndian(br.ReadUInt16());
                encodingId = ToLittleEndian(br.ReadUInt16());

                // Only read to advance reader position by 2 bytes

                languageId = ToLittleEndian(br.ReadUInt16());
                Console.WriteLine("lang:" + FontNameExtractor.langs[languageId]);

                nameId = ToLittleEndian(br.ReadUInt16());
                NameLength = ToLittleEndian(br.ReadUInt16());
                ByteOffset = ToLittleEndian(br.ReadUInt16());
            }

            internal string ProvideFullName(BinaryReader br, int recordOffset)
            {
                // Search the start position of the font name

                int totalOffset = recordOffset + ByteOffset;
                br.BaseStream.Seek(totalOffset, SeekOrigin.Begin);

                // Now read the amount of bytes specified in the name record
                // and convert to a string

                byte[] nameBytes = br.ReadBytes(NameLength);
                return BytesToString(nameBytes);
            }

            /// <summary>
            /// Returns a <see cref="System.String"/> that represents this instance.
            /// </summary>
            /// <returns>
            /// A <see cref="System.String"/> that represents this instance.
            /// </returns>
            public override string ToString()
            {
                return string.Format("NameRecord - Key; Platform ID = {0}, Encoding ID = {1}, Name ID = {2}",
                    platformId.ToString(), encodingId.ToString(), nameId.ToString());
            }

        }

        #endregion
    }
}