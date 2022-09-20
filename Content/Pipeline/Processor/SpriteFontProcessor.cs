using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using engenious.Content.Pipeline;
using engenious.Content.Serialization;
using engenious.Graphics;
using engenious.Pipeline.Helper;

using MsdfGen;

using SharpFont;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace engenious.Pipeline
{
    /// <summary>
    ///     Processor for spritefont content files.
    /// </summary>
    [ContentProcessor(DisplayName = "Font Processor")]
    public class SpriteFontProcessor : ContentProcessor<SpriteFontContent, CompiledSpriteFont>
    {
        private const float SDF_PRESCALE = 100;
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool SetDllDirectory(string lpPathName);

        private readonly unsafe struct BitmapData
        {
            private readonly Action _cleanup;
            private readonly CopyDelegate _copy;

            public BitmapData(void* data, CopyDelegate copyMethod, Action cleanup, int width, int height, Vector2 scale)
            {
                Data = data;
                _copy = copyMethod;
                _cleanup = cleanup;
                Width = width;
                Height = height;
                Scale = scale;
            }

            public delegate void CopyDelegate(BitmapData bmp, Span<Rgba32> targetPtr, int offsetX, int offsetY, int targetWidth,
                int width, int height);

            public int Width { get; }
            public int Height { get; }
            public void* Data { get; }
            
            public Vector2 Scale { get; }


            public void Copy(Span<Rgba32> targetPtr, int offsetX, int offsetY, int targetWidth, int width, int height)
            {
                _copy(this, targetPtr, offsetX, offsetY, targetWidth, width, height);
            }

            public void Cleanup()
            {
                _cleanup();
            }
        }

        private static unsafe Image<Rgba32> testRenderMTSDF(IntPtr pixels, int width, int height)
        {
            static byte median(byte r, byte g, byte b)
            {
                return Math.Max(Math.Min(r, g), Math.Min(Math.Max(r, g), b));
            }

            var outImg = new Image<Rgba32>(width, height);

            var memGroup = outImg.GetPixelMemoryGroup();
            
            Vector2 msdfUnit = new Vector2(4) / new Vector2(width, height);
            byte* inputPtr = (byte*) pixels;
            for (int y = 0; y < memGroup.Count; y++)
            {
                var row = memGroup[y].Span;
                for (int x = 0; x < row.Length; x++)
                {
                    int index = (y * width + x) * 4;
                    var sigDist = median(inputPtr[index + 2], inputPtr[index + 1], inputPtr[index + 0]) / 255.0 - 0.5;
                    byte opacity = (byte)(Math.Clamp(sigDist + 0.5, 0.0, 1.0) * 255);
                    row[x] = new Rgba32(255, 255, 255, opacity);
                    // outputPtr[index + 0] = 255;
                    // outputPtr[index + 1] = 255;
                    // outputPtr[index + 2] = 255;
                    // outputPtr[index + 3] = opacity;
                }
            }
            return outImg;
        }
        
        #region implemented abstract members of ContentProcessor

        /// <inheritdoc />
        public override CompiledSpriteFont? Process(SpriteFontContent input, string filename, ContentProcessorContext context)
        {
            var game = (IGame)context.Game;
            if (input.FontName == null)
                throw new ArgumentException("SpriteFontContent did not have a valid FontName", nameof(input));
            if (!FontConfig.Instance.GetFontFile(input.FontName, input.Size, input.Style, out var fontFile))
                context.RaiseBuildMessage(filename, $"'{input.FontName}' was not found, using fallback font", BuildMessageEventArgs.BuildMessageType.Warning);

            if (fontFile == null)
            {
                context.RaiseBuildMessage(filename, $"'{input.FontName}' was not found, no fallback font provided", BuildMessageEventArgs.BuildMessageType.Error);
                return null;
            }

            //Initialization



            var sharpFontLib = typeof(SharpFont.Library).Assembly;
            try
            {
                NativeLibrary.SetDllImportResolver(sharpFontLib, (name, assembly, path) =>
                {
                    if (name == "freetype6")
                    {
                        return NativeLibrary.Load("freetype", assembly, path);
                    }

                    return IntPtr.Zero;
                });
            }
            catch
            {
                // ignored
            }

            
            Library lib = new Library();
            var face = lib.NewFace(fontFile, 0);
            var paletteManager = new PaletteManager(face);
            var fontPreScale = input.FontType == SpriteFontType.BitmapFont ? 1 : SDF_PRESCALE;

            bool SetSize()
            {
                Fixed26Dot6 desiredYSize = input.Size * fontPreScale;
                try
                {
                    face.SetCharSize(new Fixed26Dot6(0), desiredYSize, (uint)DpiHelper.DpiX, (uint)DpiHelper.DpiY);

                }
                catch (FreeTypeException e)
                {
                    if (e.Error == Error.InvalidPixelSize)
                    {
                        Fixed26Dot6 bestXSize = 0, bestYSize = 0;
                        unsafe
                        {
                            bool magnify = true;
                            Fixed26Dot6 minDistance = Fixed26Dot6.FromRawValue(int.MaxValue);
                            foreach (var ftBitmap in face.AvailableSizes)
                            {
                                Fixed26Dot6 ySize = ftBitmap.NominalHeight;
                                Fixed26Dot6 distance = ySize - desiredYSize;

                                var absoluteDistance = distance < 0 ? (distance * -1) : distance;
                                if ((magnify && distance >= 0) || absoluteDistance <= minDistance) {
                                    magnify = distance < 0;
                                    minDistance = absoluteDistance;
                                    bestXSize = ftBitmap.NominalWidth;
                                    bestYSize = ySize;
                                }
                            }
                        }

                        if (bestXSize == 0 && bestYSize == 0)
                        {
                            context.RaiseBuildMessage(filename, $"'{input.FontName}' has no determinable font size", BuildMessageEventArgs.BuildMessageType.Error);
                            return false;
                        }
                        face.SetCharSize(bestXSize, bestYSize, 0, 0);

                        var scale = bestXSize.ToDouble() / desiredYSize.ToDouble();
                        fontPreScale = (float)(scale * (72.0 / 96.0));
                    }
                }

                return true;
            }

            if (!SetSize())
                return null;

            CompiledSpriteFont compiled = new CompiledSpriteFont();
            compiled.Spacing = input.Spacing;
            compiled.DefaultCharacter = input.DefaultCharacter;
            compiled.FontType = input.FontType;
            
            var glyphs = new Dictionary<Rune, GlyphSlot>();

            
            

            static (int paletteIndex, uint glyphIndex)[] GetGlyphs(Face face, Dictionary<uint, int> paletteIndices, List<uint> paletteList, Palette? palette, uint baseGlyph)
            {
                if (baseGlyph == 0)
                    return Array.Empty<(int, uint)>();
                var lst = new List<(int, uint)> { (-1, baseGlyph) };

                foreach (var sub in new GlyphLayerEnumerable(face, palette, baseGlyph))
                {
                    int paletteIndex;
                    if (sub.Color.ColorIndex == 0xFFFF)
                    {
                        paletteIndex = -1;
                    }
                    else if (!paletteIndices.TryGetValue(sub.Color.ColorIndex, out paletteIndex))
                    {
                        paletteIndex = paletteIndices.Count;
                        paletteIndices.Add(sub.Color.ColorIndex, paletteIndex);
                        paletteList.Add(sub.Color.ColorIndex);
                    }

                    lst.Add((paletteIndex, baseGlyph));
                    //lst.Add((sub.Color.IsForegroundColor ? null : new Color(sub.Color.R, sub.Color.G, sub.Color.B, sub.Color.A), sub.GlyphIndex));
                }
                
                return lst.ToArray();
            }

            var palette = paletteManager.FirstOrDefault();
            var paletteIndices = new Dictionary<uint, int>();
            var paletteList = new List<uint>();
            
            var characters = input.CharacterRegions.SelectMany(
                r => r.GetCharacters().Select(c => (characterRange: c, glyphInfo: GetGlyphs(face, paletteIndices, paletteList, palette, face.GetCharIndex(unchecked((uint)c.Value))))).Where(x=> x.glyphInfo.Length != 0)).ToList();

            var bitmaps = new List<(Rune character, List<(int glyphColorIndex, BitmapData? bmpData, GlyphMetrics)> glyphData, float advance)>();

            //Loading Glyphs, Calculate Kernings and Create Bitmaps
            int totalWidth=0,maxWidth=0,maxHeight=0;
            int bitmapCount = 0;

            var preloadedGlyphs = new List<(Rune characterRange, (int paletteIndex, uint glyphIndex)[] glyphInfo, GlyphSlot glyph)>();

            foreach (var l in characters)
            {
                var (character, glyphInfo) = l;
                
                //Load Glyphs
                face.LoadGlyph(glyphInfo[0].glyphIndex, LoadFlags.Color, LoadTarget.Normal);

                if (face.Glyph.Format == GlyphFormat.Bitmap)
                {
                    if (compiled.FontType != SpriteFontType.BitmapFont)
                    {
                        context.RaiseBuildMessage(filename, $"{filename}: Warning: Can not create {compiled.FontType} from font {face.FamilyName}, because it contains characters which have only bitmap font data! Falling back to bitmap font!", BuildMessageEventArgs.BuildMessageType.Warning);
                        compiled.FontType = SpriteFontType.BitmapFont;
                        input.FontType = SpriteFontType.BitmapFont;
                        fontPreScale = 1f;
                        if (!SetSize())
                            return null;
                    }
                }
                
                preloadedGlyphs.Add((character, glyphInfo, face.Glyph));
            }
            compiled.LineSpacing = (float)face.Size.Metrics.Height / fontPreScale;
            compiled.BaseLine = (float)face.Size.Metrics.Ascender / fontPreScale;
            
            foreach (var l in preloadedGlyphs)
            {
                var (character, glyphInfo, glyph) = l;
                glyph.Tag = character;
                glyphs.Add(character, glyph);

                //Calculate Kernings
                if (input.UseKerning)
                {
                    foreach (var r in characters)
                    {
                        var kerning = face.GetKerning(l.glyphInfo[0].glyphIndex, r.glyphInfo[0].glyphIndex, KerningMode.Default);
                        if (kerning == default(FTVector26Dot6)) continue;
                        compiled.Kernings[new RunePair(l.characterRange, r.characterRange)] = (float)kerning.X / fontPreScale;
                    }
                }

                var bitmapDatas = new List<(int glyphColorIndex, BitmapData? bmpData, GlyphMetrics metrics)>();
                bool isFirst = true;
                foreach (var (glyphColor, glyphIndex) in glyphInfo)
                {
                    BitmapData? bitmapData = null;
                    GlyphMetrics? metrics = null;
                    //Create bitmaps
                    switch (compiled.FontType)
                    {
                        case SpriteFontType.BitmapFont:
                            (bitmapData, metrics) = CreateBitmapFont(glyph, glyphIndex, ref totalWidth, ref maxWidth, ref maxHeight);
                            break;
                        case SpriteFontType.PSDF:
                        case SpriteFontType.SDF:
                        case SpriteFontType.MSDF:
                        case SpriteFontType.MTSDF:
                            (bitmapData, metrics) = CreateSdfFont(compiled.FontType, glyph, glyphIndex, ref totalWidth, ref maxWidth, ref maxHeight, input.Size);
                            break;
                    }

                    if (metrics is not null)
                    {
                        bitmapDatas.Add((glyphColor, bitmapData, metrics));
                        bitmapCount++;
                        isFirst = false;
                    }
                    else if (isFirst)
                        break;
                }
                if (bitmapDatas.Count > 0)
                    bitmaps.Add((character, bitmapDatas, (float)glyph.Advance.X / fontPreScale));
            }
            int cellCount = (int)Math.Ceiling(Math.Sqrt(bitmapCount));

            int spacingX = 2, spacingY = 2;
            var target = new Image<Rgba32>(ImageSharpHelper.Config, cellCount*(maxWidth+ spacingX), cellCount*(maxHeight + spacingY));
            var targetRectangle = new Rectangle(0, 0, target.Width, target.Height);
            int offsetX = 0,offsetY=0;
            target.Mutate((x) => x.BackgroundColor(SixLabors.ImageSharp.Color.Transparent));

            var overlay = new Image<Rgba32>(target.Width, target.Height);

            //Create Glyph Atlas
            foreach (var bmpKvp in bitmaps)
            {
                var character = bmpKvp.character;

                var toAdd = new List<(Rectangle textureRegionPx, Vector2 offset, Vector2 size, int glyphColorIndex)>();

                foreach (var (glyphColor, bmpData, metrics) in bmpKvp.glyphData)
                {
                    if (bmpData == null)
                    {
                        toAdd.Add((new Rectangle(offsetX,offsetY,1,1),new Vector2((float)metrics.HorizontalBearingX / fontPreScale,compiled.BaseLine - ((float)metrics.HorizontalBearingY / fontPreScale)), new Vector2((float)metrics.Width / fontPreScale, (float)metrics.Height / fontPreScale), glyphColor));
                        // compiled.CharacterMap.Add(character, new FontCharacter(character,targetRectangle,new Rectangle(offsetX,offsetY,1,1),new Vector2((float)bmpKvp.metrics.HorizontalBearingX / fontPreScale,compiled.BaseLine - ((float)bmpKvp.metrics.HorizontalBearingY / fontPreScale)), new Vector2((float)bmpKvp.metrics.Width / fontPreScale, (float)bmpKvp.metrics.Height / fontPreScale), bmpKvp.advance));
                        if (offsetX++ > target.Width)
                        {
                            offsetY += maxHeight + spacingY;
                            offsetX = 0;
                        }
                        continue;
                    }

                    var bmp = bmpData.Value;
                    var scale = bmp.Scale;
                    var relScale = scale / MathF.Min(scale.X, scale.Y);
                    int width = bmp.Width;
                    int height = bmp.Height;
                    if (offsetX + width + spacingX > target.Width)
                    {
                        offsetY += maxHeight + spacingX;
                        offsetX = 0;
                    }
                    //TODO divide width by 3?
                    
                    overlay.Mutate((x) => 
                                       x.Draw(SixLabors.ImageSharp.Color.Red, 1f, new SixLabors.ImageSharp.RectangleF(offsetX, offsetY, width, height)));
                    
                    var destSize = new Vector2((float) metrics.Width / fontPreScale,
                        (float) metrics.Height / fontPreScale) * scale;
                    var destSizeDiff = destSize - new Vector2((float) metrics.Width / fontPreScale,
                        (float) metrics.Height / fontPreScale);
                    toAdd.Add((new Rectangle(offsetX,offsetY,width,height),new Vector2((float)metrics.HorizontalBearingX / fontPreScale - destSizeDiff.X / 2,(compiled.BaseLine - (float)metrics.HorizontalBearingY / fontPreScale - destSizeDiff.Y / 2)), destSize, glyphColor));

                    unsafe
                    {
                        if (!target.DangerousTryGetSinglePixelMemory(out var mem))
                            throw new InvalidOperationException();
                        var span = mem.Span[(offsetX + offsetY * target.Width)..];
                        bmp.Copy(span, offsetX, offsetY,
                            target.Width, width, height);
                    }
                    offsetX += width + spacingX;
                    bmp.Cleanup();
                    
                }

                var mainGlyph = new FontGlyph(targetRectangle, toAdd[0].textureRegionPx, toAdd[0].offset, toAdd[0].size, toAdd[0].glyphColorIndex);

                var layers = toAdd.Count == 1 ? Array.Empty<FontGlyph>() : toAdd.Skip(1).Select(x => new FontGlyph(targetRectangle, x.textureRegionPx, x.offset, x.size, x.glyphColorIndex)).ToArray();
                compiled.CharacterMap.Add(character, new FontCharacter(character, mainGlyph, bmpKvp.advance, layers));
            }
            compiled.Texture = new TextureContent(game.GraphicsDevice,false,1, target,TextureContentFormat.Png,TextureContentFormat.Png);
            compiled.Spacing = input.Spacing;
            compiled.DefaultCharacter = input.DefaultCharacter;
            compiled.Palettes = new FontPalette[paletteManager.Count];
            for (int i = 0; i < paletteManager.Count; i++)
            {
                var fontPalette = new FontPalette(paletteIndices.Count);
                var origPalette = paletteManager[i];
                for (int j = 0; j < fontPalette.Colors.Length; j++)
                {
                    var ftColor = origPalette[(int)paletteList[j]];
                    fontPalette.Colors[j] = new Color(ftColor.Red, ftColor.Green, ftColor.Blue, ftColor.Alpha);
                }
                compiled.Palettes[i] = fontPalette;
            }
            target.Dispose();

            return compiled;
        }

        private static IntPtr GetReference(Face face)
        {
            var propInfo = typeof(Face).GetProperty("Reference", BindingFlags.Instance | BindingFlags.NonPublic);
            if (propInfo == null)
                throw new InvalidOperationException("SharpFont Face type should contain the non public \"Reference\" property.");
            return (IntPtr)(propInfo.GetValue(face) ??
                            throw new InvalidOperationException(
                                "SharpFont Face.Reference property should never return null."));
        }

        private static Vector2 AutoFrame(Msdf msdf, int pxRange, double destX, double destY)
        {
            var frame = new Vector2d(msdf.Bitmap.Width, msdf.Bitmap.Height);
            double m = .5f + 0;
            frame -= new Vector2d(2*m*pxRange);//range == 1
            
            msdf.Shape.GetBounds(out var l, out var r, out var t, out var b);
            if (l >= r || b >= t)
            {
                l = 0;
                b = 0;
                r = 1;
                t = 1;
            }

            if (frame.X <= 0 || frame.Y <= 0)
                throw new Exception("Range does not fit");
            var dims = new Vector2d(r-l, t-b);

            // double texelX = 0;
            // double texelY = 0;
            Vector2 scale = new Vector2((float)((msdf.Bitmap.Width - 2) / destX), (float)((msdf.Bitmap.Height - 2) / destY));
            //scale /= MathF.Min(scale.X, scale.Y);
            if (dims.X * frame.Y < dims.Y * frame.X)
            {
                msdf.SetTranslation(-l, -b); // .5 * 
                msdf.SetScale((frame.Y) / dims.Y, (frame.Y) / dims.Y);

            }
            else
            {
                msdf.SetTranslation(-l,  -b); // 
                msdf.SetScale((frame.X) / dims.X, (frame.X) / dims.X);

            }

            

            //scale = scale / Math.Min(scale.X, scale.Y);

            //scale = Vector2.One;
            
            msdf.GetScale(out var sX, out var sY);
            //scale /= new Vector2((float) sX, (float) sY);
            var tr = new Vector2d(m * pxRange) / new Vector2d(sX, sY);


            msdf.GetTranslation(out var tX, out var tY);

            msdf.SetTranslation(tX + tr.X, tY + tr.Y);

            return scale;
        }
        
        private static (BitmapData?, GlyphMetrics) CreateSdfFont(SpriteFontType fontType, GlyphSlot glyph, uint glyphIndex, ref int totalWidth, ref int maxWidth,
            ref int maxHeight, int fontSize)
        {
            glyph.Face.LoadGlyph(glyphIndex, LoadFlags.Monochrome, LoadTarget.Normal);
            var loadedGlyph = glyph.Face.Glyph;
            int pxRange = 4;
            var mode = fontType switch
            {
                SpriteFontType.PSDF => MsdfMode.Pseudo,
                SpriteFontType.SDF => MsdfMode.Single,
                SpriteFontType.MSDF => MsdfMode.Multi,
                SpriteFontType.MTSDF => MsdfMode.MultiAndTrue,
                _ => throw new ArgumentException("Invalid SDF font type", nameof(fontType))
            };
            //const int inchToPoint = 72;
            //maxWidth = (int)MathF.Ceiling((float)glyph.Metrics.Width * DpiHelper.DpiX / inchToPoint);
            //maxHeight = (int)MathF.Ceiling((float)glyph.Metrics.Height * DpiHelper.DpiY / inchToPoint);
            var width = (int)MathF.Ceiling((float)loadedGlyph.Metrics.Width / SDF_PRESCALE);
            var height = (int)MathF.Ceiling((float)loadedGlyph.Metrics.Height / SDF_PRESCALE);
            if (width == 0 || height == 0)
            {
                maxWidth = Math.Max(maxWidth, pxRange + 2);
                maxHeight = Math.Max(maxHeight, pxRange + 2);
                totalWidth += 2 + 1;
                return (null, loadedGlyph.Metrics);
            }
            maxWidth = Math.Max(maxWidth, width + pxRange);
            maxHeight = Math.Max(maxHeight, height + pxRange);
            var msdf = new Msdf(mode, width + pxRange, height + pxRange);
            bool skipColoring = false;
            var fontHandle = new MsdfNative.FontHandle(GetReference(glyph.Face));
            unsafe
            {
                if (!msdf.Shape.LoadFromFreetypeFont(&fontHandle, glyphIndex, 0))
                    throw new Exception("Failed loading shape!");
            }
            // if (!msdf.Shape.LoadFromDescriptionFile("test.shape", ref skipColoring))
            //     throw new Exception("Failed loading shape!");

            if (!msdf.Shape.Validate())
                throw new Exception("Failed validating shape!");

            bool geometryPreprocess = false;

            if (geometryPreprocess)
            {
                if (!msdf.Shape.PreprocessGeometry())
                    throw new Exception("Shape preprocessing failed!");
            }

            msdf.Shape.Normalize();
            
            msdf.Shape.GetBounds(out var left, out var right, out var top, out var bottom);
            msdf.Range = pxRange;

            if (glyph.Tag.ToString() == "T" || glyph.Tag.ToString() == "a" || glyph.Tag.ToString() == "x" || glyph.Tag.ToString() == "." || glyph.Tag.ToString() == "i")
            {
                
            }

            var scale = AutoFrame(msdf, pxRange, loadedGlyph.Metrics.Width / SDF_PRESCALE, loadedGlyph.Metrics.Height / SDF_PRESCALE);

            unsafe
            {
                msdf.ColorEdges(Msdf.GetColoringStrategy(MsdfColoringStrategy.Simple), 0, skipColoring, null, 3.0);
            }
            
            if (!msdf.Generate())
                throw new Exception("Generating failed!");

            if (!msdf.ApplyOrientation())
                throw new Exception("Apply orientation failed!");

            if (!msdf.ApplyOutputDistanceShift())
                throw new Exception("Apply output distance shift failed!");

            unsafe
            {
                int channelCount = msdf.Bitmap.ChannelCount;
                BitmapData.CopyDelegate del = (bmp, ptr, x, y, targetWidth, width, height) =>
                    CopyMsdfToAtlas_BGRA(bmp, ptr, x, y, targetWidth, width, height, channelCount, pxRange);
                var bmpData = new BitmapData(msdf.Bitmap.PixelData, del, () => msdf.Dispose(), msdf.Bitmap.Width, msdf.Bitmap.Height, scale);
                
                totalWidth += msdf.Bitmap.Width;
                return (bmpData, loadedGlyph.Metrics);
            }
        }

        private static (BitmapData?, GlyphMetrics) CreateBitmapFont(GlyphSlot glyph, uint glyphIndex, ref int totalWidth, ref int maxWidth,
            ref int maxHeight)
        {
            glyph.OwnBitmap();
            glyph.Face.LoadGlyph(glyphIndex, glyph.Face.HasColor ? LoadFlags.Color : LoadFlags.Monochrome, LoadTarget.Normal);
            var loadedGlyph = glyph.Face.Glyph;
            var glyphActual = loadedGlyph.GetGlyph();
            glyphActual.ToBitmap(RenderMode.Normal, default(FTVector26Dot6), false);

            var bmg = glyphActual.ToBitmapGlyph();
            if (bmg.Bitmap.Width == 0 || bmg.Bitmap.Rows == 0)
            {
                totalWidth += 2 + 1;
                maxWidth = Math.Max(maxWidth, 1 + 2);
                maxHeight = Math.Max(maxHeight, 1 + 2);
                return (null, loadedGlyph.Metrics);
            }
            else
            {
                var bmp = bmg.Bitmap;
                totalWidth += bmp.Width;
                maxWidth = Math.Max(maxWidth, bmp.Width); //TODO: divide by 3?
                maxHeight = Math.Max(maxHeight, bmp.Rows);
                
                unsafe
                {
                    BitmapData.CopyDelegate del = bmp.PixelMode switch //TODO: divide width by 3?
                    {
                        PixelMode.Mono => CopyFTBitmapToAtlas_Mono,
                        PixelMode.Gray => CopyFTBitmapToAtlas_Gray,
                        PixelMode.Lcd => CopyFTBitmapToAtlas_LcdBGR,
                        PixelMode.Bgra => CopyFTBitmapToAtlas_BGRA,
                        _ => throw new NotImplementedException("Pixel Mode not supported")
                    };
                    var bmpData = new BitmapData((void*) bmp.Buffer, del, () => bmp.Dispose(), bmp.Width, bmp.Rows, Vector2.One);
                
                    return (bmpData, loadedGlyph.Metrics);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void CopyFTBitmapToAtlas_Mono(BitmapData bmp, Span<Rgba32> targetPtr,int offsetX,int offsetY,int targetWidth,int width,int height)
        {
            var bmpPtr = (byte*)bmp.Data;
            int subIndex = 0;
            int offset = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++, offset++, subIndex++,bmpPtr++)
                {
                    if ((((*bmpPtr) >> subIndex) & 0x1) != 0)
                        targetPtr[offset] = new Rgba32(0xFF, 0xFF, 0xFF, 0xFF); 
                    if (subIndex == 8)
                        subIndex = 0;
                }
                offset += targetWidth - width;
            }
        }

        #endregion


        #region Copy Implementations
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void CopyFTBitmapToAtlas_Gray4(BitmapData bmp, Span<Rgba32> targetPtr, int offsetX, int offsetY, int targetWidth, int width, int height, int padding)
        {
            //TODO: implement
            var bmpPtr = (byte*)bmp.Data;
            int offset = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++, offset++, bmpPtr++)
                {
                    byte value = *bmpPtr;
                    targetPtr[offset] = new Rgba32(0xFF, 0xFF, 0xFF, value); //value > 0 ? 255<< 24: 0;
                }
                offset += targetWidth - width;
                bmpPtr += padding;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void CopyFTBitmapToAtlas_Gray(BitmapData bmp, Span<Rgba32> targetPtr, int offsetX, int offsetY, int targetWidth, int width, int height)
        {
            var bmpPtr = (byte*)bmp.Data;
            int offset = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++, offset++, bmpPtr++)
                {
                    byte value = *bmpPtr;
                    targetPtr[offset] = new Rgba32(0xFF, 0xFF, 0xFF, value);
                }
                offset += targetWidth - width;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void CopyFTBitmapToAtlas_LcdRGB(BitmapData bmp, Span<Rgba32> targetPtr, int offsetX, int offsetY, int targetWidth, int width, int height)
        {
            var bmpPtr = (byte*)bmp.Data;
            int offset = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++, offset++, bmpPtr+=3)
                {
                    uint value = *(uint*)(bmpPtr) >> 8;
                    targetPtr[offset] = new Rgba32(0xFF, 0xFF, (byte)(value & 0xFF), 0xFF);
                }
                offset += targetWidth - width;
            }
        }
        //TODO: verify direction
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void CopyFTBitmapToAtlas_LcdBGR(BitmapData bmp, Span<Rgba32> targetPtr, int offsetX, int offsetY, int targetWidth, int width, int height)
        {
            var bmpPtr = (byte*)bmp.Data;
            int offset = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++, offset++, bmpPtr+=3)
                {
                    uint value = *(uint*)(bmpPtr);
                    //A | R | G | B
                    targetPtr[offset] = new Rgba32((byte)((value >> 16) & 0xFF), (byte)((value >> 8) & 0xFF), (byte)((value >> 0) & 0xFF), 0xFF);
                }
                offset += targetWidth - width;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void CopyFTBitmapToAtlas_BGRA(BitmapData bmp, Span<Rgba32> targetPtr, int offsetX, int offsetY, int targetWidth, int width, int height)
        {
            var bmpPtr = (uint*)bmp.Data;
            int offset = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++, offset++, bmpPtr++)
                {
                    uint value = *bmpPtr;
                    //A | R | G | B
                    targetPtr[offset] = new Rgba32((byte)((value >> 16) & 0xFF), (byte)((value >> 8) & 0xFF), (byte)((value >> 0) & 0xFF), (byte)((value >> 24) & 0xFF));
                }
                offset += targetWidth - width;
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void CopyMsdfToAtlas_BGRA(BitmapData bmp, Span<Rgba32> targetPtr, int offsetX, int offsetY, int targetWidth, int width, int height, int channelCount, int pxRange)
        {
            delegate*<float*, Rgba32> getColor = null;

            // width += pxRange;
            // height += pxRange;
            static byte FloatToByte(float val)
            {
                return (byte)Math.Clamp((int) (val * 256), 0, 255);
            }
            switch (channelCount)
            {
                case 1:
                {
                    static Rgba32 GetArgb(float* sdf)
                    {
                        var value = FloatToByte(*sdf);
                        return new Rgba32(value, value, value, 0xFF);
                    }
                    getColor = &GetArgb;
                    break;
                }
                case 3:
                {
                    static Rgba32 GetArgb(float* sdf)
                    {
                        var r = FloatToByte(*(sdf + 2));
                        var g = FloatToByte(*(sdf + 1));
                        var b = FloatToByte(*(sdf + 0));
                        return new Rgba32(r, g, b, 0xFF);
                    }

                    getColor = &GetArgb;
                    break;
                }
                case 4:
                {
                    static Rgba32 GetArgb(float* sdf)
                    {
                        var r = FloatToByte(*(sdf + 2));
                        var g = FloatToByte(*(sdf + 1));
                        var b = FloatToByte(*(sdf + 0));
                        var a = FloatToByte(*(sdf + 3));
                        return new Rgba32(r, g, b, a);
                    }

                    getColor = &GetArgb;
                    break;
                }
                default:
                    throw new InvalidOperationException($"Channel count {channelCount} not supported!");
            }
            var bmpPtr = (float*)bmp.Data;
            int offset = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++, offset++)
                {
                    //uint value = *bmpPtr;
                    int indexSrc = ((height - y - 1) * width) + x;
                    //A | R | G | B
                    targetPtr[offset] = getColor(&bmpPtr[indexSrc * channelCount]);
                }
                offset += targetWidth - width;
            }
        }
        #endregion
    }
}

