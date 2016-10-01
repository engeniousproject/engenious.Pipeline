using System;
using engenious.Content.Pipeline;
using System.Drawing;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;

namespace engenious.Pipeline
{
    [ContentProcessor(DisplayName = "Font Processor")]
    public class SpriteFontProcessor : ContentProcessor<SpriteFontContent, CompiledSpriteFont>
    {


        #region implemented abstract members of ContentProcessor
        public override CompiledSpriteFont Process(SpriteFontContent input, string filename, ContentProcessorContext context)
        {
            CompiledSpriteFont compiled = new CompiledSpriteFont();
            compiled.Spacing = input.Spacing;
            compiled.DefaultCharacter = input.DefaultCharacter;
            List<char> characters = new List<char>();
            foreach(var region in input.CharacterRegions)
            {
                characters.AddRange(region.GetChararcters());
            }
            Bitmap bmp = new Bitmap(100,100);
            Font fnt = new Font(input.FontName,input.Size,input.Style);
            compiled.LineSpacing = fnt.Height;
            float emHeight = fnt.FontFamily.GetEmHeight(input.Style);
            float lineSpace = fnt.FontFamily.GetLineSpacing(fnt.Style);
            float ascent = fnt.FontFamily.GetCellAscent(fnt.Style);
            compiled.BaseLine =  (int)Math.Round(fnt.Height * ascent / lineSpace);
            //ListFonts();
            string fontFile = FontConfig.Instance.GetFontFile(input.FontName,input.Size,input.Style);
            unsafe{
                SharpFont.Library lib = new SharpFont.Library();
                var face = lib.NewFace(fontFile,0);

            }

            return compiled;
        }

        #endregion
    }
}

