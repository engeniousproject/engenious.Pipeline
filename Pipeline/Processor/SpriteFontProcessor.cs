using System;
using engenious.Content.Pipeline;
using System.Drawing;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using engenious.Content;
using engenious.Graphics;
using SharpFont;

namespace engenious.Pipeline
{
    [ContentProcessor(DisplayName = "Font Processor")]
    public class SpriteFontProcessor : ContentProcessor<SpriteFontContent, CompiledSpriteFont>
    {


        #region implemented abstract members of ContentProcessor
        public override CompiledSpriteFont Process(SpriteFontContent input, string filename, ContentProcessorContext context)
        {
            string fontFile;
            if (!FontConfig.Instance.GetFontFile(input.FontName, input.Size, input.Style, out fontFile))
                context.RaiseBuildMessage(filename, $"'{input.FontName}' was not found, using fallback font", BuildMessageEventArgs.BuildMessageType.Warning);

            if (fontFile == null)
            {
                context.RaiseBuildMessage(filename, $"'{input.FontName}' was not found, no fallback font provided", BuildMessageEventArgs.BuildMessageType.Error);
                return null;
            }

            //Initialization
            SharpFont.Library lib = new SharpFont.Library();
            var face = lib.NewFace(fontFile, 0);
            System.Drawing.Graphics g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero);
            face.SetCharSize(new Fixed26Dot6(0), new Fixed26Dot6(input.Size), (uint)g.DpiX, (uint)g.DpiY);

            CompiledSpriteFont compiled = new CompiledSpriteFont();
            compiled.Spacing = input.Spacing;
            compiled.DefaultCharacter = input.DefaultCharacter;

            
            var glyphs = new Dictionary<uint, GlyphSlot>();

            var characters = input.CharacterRegions.SelectMany(
                r => r.GetChararcters().Select(c => Tuple.Create(c, face.GetCharIndex(c)))).Where(x=>x.Item2 != 0).ToList();

            var bitmaps = new List<Tuple<char, Bitmap,int,GlyphMetrics>>();

            compiled.LineSpacing = face.Size.Metrics.Height.Value>>6;
            compiled.BaseLine = face.Size.Metrics.Ascender.Value>>6;
            //Loading Glyphs, Calculate Kernings and Create Bitmaps
            foreach (var l in characters)
            {
                var character = l.Item1;
                var glyphIndex = l.Item2;
                
                //Load Glyphs
                face.LoadGlyph(glyphIndex, LoadFlags.Color, LoadTarget.Normal);
                var glyph = face.Glyph;
                glyph.Tag = character;
                glyphs.Add(character, glyph);

                //Calculate Kernings
                if (input.UseKerning)
                {
                    foreach (var r in characters)
                    {
                        var kerning = face.GetKerning(l.Item2, r.Item2, KerningMode.Default);
                        if (kerning == default(FTVector26Dot6)) continue;
                        compiled.kernings[l.Item1<<16|r.Item1] = kerning.X.Value>>6;
                    }
                }

                //Create bitmaps
                glyph.OwnBitmap();
                var glyphActual = glyph.GetGlyph();
                glyphActual.ToBitmap(RenderMode.Normal, default(FTVector26Dot6), false);

                var bmg = glyphActual.ToBitmapGlyph();
                Bitmap bmp;
                if (bmg.Bitmap.Width == 0 || bmg.Bitmap.Rows == 0)
                {
                    bmp = new Bitmap(1,1);
                }
                else
                {
                    bmp = (Bitmap) bmg.Bitmap.ToGdipBitmap(System.Drawing.Color.Black);

                }

                bitmaps.Add(Tuple.Create(character, bmp, glyph.Advance.X.Value>>6,glyph.Metrics));
            }
            g.Dispose();
            var totalWidth = bitmaps.Sum(kvp => kvp.Item2.Width+2);
            var maxHeight = bitmaps.Max(kvp => kvp.Item2.Height);

            var target = new Bitmap(totalWidth,maxHeight);
            var targetRectangle = new Rectangle(0, 0, target.Width, target.Height);
            var targetData = target.LockBits(new System.Drawing.Rectangle(0, 0, target.Width, target.Height), ImageLockMode.WriteOnly, target.PixelFormat);
            var offset = 0;

            //Create Glyph Atlas
            foreach (var bmpKvp in bitmaps)
            {
                var bmp = bmpKvp.Item2;
                var character = bmpKvp.Item1;
                
                var bmpData = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly,
                    bmp.PixelFormat);
                
                compiled.characterMap.Add(character, new FontCharacter(character,targetRectangle,new Rectangle(offset,0,bmp.Width,bmp.Height),new Vector2(bmpKvp.Item4.HorizontalBearingX.Value >> 6,bmpKvp.Item4.HorizontalBearingY.Value>>6), bmpKvp.Item3));
                var padding = bmp.Width%4 == 0 ? 0 : 4- bmp.Width%4;
                unsafe{
                    
                    int* targetPtr = (int*) targetData.Scan0+offset; //Pointer zum Pixel im Target Bitmap
                    byte* bmpPtr = (byte*)bmpData.Scan0;
                    for (int x = 0; x < bmp.Height; x++)
                    {
                        for (int y = 0; y < bmp.Width; y++, targetPtr++, bmpPtr++)
                        {
                            byte value = *bmpPtr;
                            *targetPtr = value<<24 | 0xFFFFFF; //value > 0 ? 255<< 24: 0;
                        }
                        targetPtr += target.Width - bmp.Width;
                        bmpPtr += padding;
                    }
                }
                offset += bmp.Width;

                bmp.UnlockBits(bmpData);
                bmp.Dispose();
            }
            compiled.texture = new TextureContent(false,1,targetData.Scan0,target.Width,target.Height,TextureContentFormat.Png,TextureContentFormat.Png);
            compiled.Spacing = input.Spacing;
            compiled.DefaultCharacter = input.DefaultCharacter;
            
            target.UnlockBits(targetData);
            
            //Saving files
            target.Save("test.png",ImageFormat.Png);
            target.Dispose();
            //System.Diagnostics.Process.Start("test.png"); //TODO: Remove later


            return compiled;
        }

        #endregion
    }
}

