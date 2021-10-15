using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using engenious.Content.Serialization;
using engenious.Graphics;
using engenious.Helper;
using OpenTK.Graphics.OpenGL;

namespace engenious.Content.Pipeline
{
    /// <summary>
    ///     Processed texture content to create texture content files.
    /// </summary>
    public class TextureContent
    {
        private readonly GraphicsDevice _graphicsDevice;
        private int _texture;

        private TextureContent(GraphicsDevice graphicsDevice)
        {
            MipMaps = new List<TextureContentMipMap>();
            _graphicsDevice = graphicsDevice;
        }

        /// <summary>
        ///     Initializes a ne instance of the <see cref="TextureContent"/> class.
        /// </summary>
        /// <param name="graphicsDevice">The graphics device used to load the texture and create mip maps with.</param>
        /// <param name="generateMipMaps"></param>
        /// <param name="mipMapCount">The number of mip maps to create.</param>
        /// <param name="inputData">The bitmap input data.</param>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="inputFormat">The input format of the texture.</param>
        /// <param name="outputFormat">The format of the texture to use on GPU side.</param>
        public TextureContent(GraphicsDevice graphicsDevice, bool generateMipMaps, int mipMapCount, byte[] inputData, int width, int height, TextureContentFormat inputFormat, TextureContentFormat outputFormat)
            : this(graphicsDevice)
        {
            GCHandle handle = GCHandle.Alloc(inputData, GCHandleType.Pinned);
            CreateTexture(graphicsDevice, generateMipMaps, mipMapCount, handle.AddrOfPinnedObject(), width, height, inputFormat, outputFormat);
            handle.Free();
        }

        /// <summary>
        ///     Initializes a ne instance of the <see cref="TextureContent"/> class.
        /// </summary>
        /// <param name="graphicsDevice">The graphics device used to load the texture and create mip maps with.</param>
        /// <param name="generateMipMaps"></param>
        /// <param name="mipMapCount">The number of mip maps to create.</param>
        /// <param name="inputData">A pointer to the bitmap input data.</param>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="inputFormat">The input format of the texture.</param>
        /// <param name="outputFormat">The format of the texture to use on GPU side.</param>
        public TextureContent(GraphicsDevice graphicsDevice,bool generateMipMaps, int mipMapCount, IntPtr inputData, int width, int height, TextureContentFormat inputFormat, TextureContentFormat outputFormat)
            : this(graphicsDevice)
        {
            CreateTexture(graphicsDevice, generateMipMaps, mipMapCount, inputData, width, height, inputFormat, outputFormat);
        }

        private void CreateTexture(GraphicsDevice graphicsDevice, bool generateMipMaps, int mipMapCount, IntPtr inputData, int width, int height, TextureContentFormat inputFormat, TextureContentFormat outputFormat)
        {
            Width = width;
            Height = height;
            Format = outputFormat;
            bool hwCompressedInput = inputFormat == TextureContentFormat.DXT1 || inputFormat == TextureContentFormat.DXT3 || inputFormat == TextureContentFormat.DXT5;
            bool hwCompressedOutput = outputFormat == TextureContentFormat.DXT1 || outputFormat == TextureContentFormat.DXT3 || outputFormat == TextureContentFormat.DXT5;
            graphicsDevice.ValidateUiGraphicsThread();

            _texture = GL.GenTexture();

            GL.BindTexture(TextureTarget.Texture2D, _texture);
            bool doGenerate = generateMipMaps && mipMapCount > 1;

            setDefaultTextureParameters();
            //GL.TexStorage2D(TextureTarget2d.Texture2D,(GenerateMipMaps ? 1 : MipMapCount),SizedInternalFormat.Rgba8,width,height);
            //GL.TexSubImage2D(TextureTarget.Texture2D,0,0,0,width,height,
            if (doGenerate)
            {
                if (_graphicsDevice.DriverVersion != null && _graphicsDevice.DriverVersion.Major < 3 &&
                    ((_graphicsDevice.DriverVersion.Major == 1 && _graphicsDevice.DriverVersion.Minor >= 4) ||
                     _graphicsDevice.DriverVersion.Major > 1))
                    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.GenerateMipmap, 1);
                else if (_graphicsDevice.DriverVersion == null || _graphicsDevice.DriverVersion.Major < 3)
                    throw new NotSupportedException("Can't generate MipMaps on this Hardware");
            }
            GL.TexImage2D(TextureTarget.Texture2D, 0, (hwCompressedOutput ? (OpenTK.Graphics.OpenGL.PixelInternalFormat)outputFormat : OpenTK.Graphics.OpenGL.PixelInternalFormat.Rgba), width, height, 0, (hwCompressedInput ? (OpenTK.Graphics.OpenGL.PixelFormat)inputFormat : OpenTK.Graphics.OpenGL.PixelFormat.Bgra), PixelType.UnsignedByte, inputData);
            if (doGenerate)
            {
                //TOODO non power of 2 Textures?
                GL.TexParameter(TextureTarget.Texture2D,TextureParameterName.TextureMaxLevel,mipMapCount);
                GL.Hint(HintTarget.GenerateMipmapHint,HintMode.Nicest);
                if (_graphicsDevice.DriverVersion != null && _graphicsDevice.DriverVersion.Major >= 3)
                    GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            }

            PreprocessMipMaps(graphicsDevice);
            
            GL.DeleteTexture(_texture);
        }

        private void setDefaultTextureParameters()
        {
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        }

        private void PreprocessMipMaps(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.ValidateUiGraphicsThread();
            bool hwCompressed = Format == TextureContentFormat.DXT1 || Format == TextureContentFormat.DXT3 || Format == TextureContentFormat.DXT5;
            int width=Width, height=Height;
            int realCount=0;
            for (int i = 0; i < (GenerateMipMaps ? 1 : MipMapCount); i++)
            {
                if (hwCompressed)
                {
                    int dataSize=0;
                    byte[] data;

                    GL.BindTexture(TextureTarget.Texture2D,_texture);
                    GL.GetTexLevelParameter(TextureTarget.Texture2D,i,GetTextureParameter.TextureCompressedImageSize,out dataSize);
                    data = new byte[dataSize];
                    GL.GetCompressedTexImage(TextureTarget.Texture2D,i,data);
                    MipMaps.Add(new TextureContentMipMap(width, height, Format, data));
                }
                else
                {
                    var bmp = new Bitmap(width,height);

                    var bmpData = bmp.LockBits(new System.Drawing.Rectangle(0,0,width,height),ImageLockMode.WriteOnly,System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                    GL.BindTexture(TextureTarget.Texture2D,_texture);
                    GL.GetTexImage(TextureTarget.Texture2D,i,OpenTK.Graphics.OpenGL.PixelFormat.Bgra,PixelType.UnsignedByte,bmpData.Scan0);

                    bmp.UnlockBits(bmpData);

                    MipMaps.Add(new TextureContentMipMap(width, height, Format, bmp));

                }
                width/=2;
                height/=2;
                realCount++;
                if (width == 0 || height == 0)
                    break;
            }
            if (!GenerateMipMaps)
                MipMapCount = realCount;
        }

        /// <summary>
        ///     Gets a value indicating the width of the texture.
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        ///     Gets a value indicating the height of the texture.
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        ///     Gets a value indicating the format of the texture content.
        /// </summary>
        public TextureContentFormat Format{ get; private set; }

        /// <summary>
        ///     Gets a value indicating whether mip maps should be generated on load.
        /// </summary>
        public bool GenerateMipMaps { get; } = false;

        /// <summary>
        ///     Gets a value indicating the number of mip maps.
        /// </summary>
        public int MipMapCount { get; private set; } = 1;

        /// <summary>
        ///     Gets a collection of all the mip map levels.
        /// </summary>
        public List<TextureContentMipMap> MipMaps { get; }
    }

    /// <summary>
    ///     Processed mip map level class for <see cref="TextureContent"/> mip maps.
    /// </summary>
    public class TextureContentMipMap
    {
        private readonly Bitmap? _bitmap;
        private readonly byte[]? _data;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TextureContentMipMap"/> instance.
        /// </summary>
        /// <param name="width">The width of the mip map level.</param>
        /// <param name="height">The height of the mip map level.</param>
        /// <param name="format">The format of the mip map level.</param>
        /// <param name="data">The pixel data of the mip map level.</param>
        public TextureContentMipMap(int width, int height, TextureContentFormat format, byte[] data)
            : this(width, height, format)
        {
            _data = data;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TextureContentMipMap"/> instance.
        /// </summary>
        /// <param name="width">The width of the mip map level.</param>
        /// <param name="height">The height of the mip map level.</param>
        /// <param name="format">The format of the mip map level.</param>
        /// <param name="data">The bitmap data of the mip map level.</param>
        public TextureContentMipMap(int width, int height, TextureContentFormat format, Bitmap data)
            : this(width, height, format)
        {
            
            _bitmap = data;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="TextureContentMipMap"/> instance.
        /// </summary>
        /// <param name="width">The width of the mip map level.</param>
        /// <param name="height">The height of the mip map level.</param>
        /// <param name="format">The format of the mip map level.</param>
        protected TextureContentMipMap(int width, int height, TextureContentFormat format)
        {
            Width = width;
            Height = height;
            Format = format;
        }

        /// <summary>
        ///     Gets the width of this mip map level.
        /// </summary>
        public int Width{ get; }

        /// <summary>
        ///     Gets the height of this mip map level.
        /// </summary>
        public int Height{ get; }

        /// <summary>
        ///     Gets a value indicating the format of the texture content.
        /// </summary>
        public TextureContentFormat Format{ get; }

        /// <summary>
        ///     Writes this mip map level to a <see cref="ContentWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="ContentWriter"/> to write to.</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void Save(ContentWriter writer)
        {
            writer.Write(Width);
            writer.Write(Height);
            writer.Write((int)Format);
            if (_bitmap != null)
            {
                using MemoryStream str = new();
                switch (Format)
                {
                    case TextureContentFormat.Png:
                        _bitmap.Save(str, ImageFormat.Png);
                        break;
                    case TextureContentFormat.Jpg:
                        _bitmap.Save(str, ImageFormat.Jpeg);
                        break;
                }

                writer.Write((int)str.Position);
                str.Position = 0;
                writer.Write(str);
            }
            else if(_data != null)
            {
                writer.Write(_data.Length);
                writer.Write(_data);
            }
            else
                throw new InvalidOperationException("_data and _bitmap where both null, which should never happen");
        }
    }
}

