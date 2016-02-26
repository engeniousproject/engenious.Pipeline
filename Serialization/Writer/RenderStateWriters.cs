using System;
using engenious.Graphics;

namespace engenious.Content.Serialization
{
    [ContentTypeWriterAttribute()]
    public class RasterizerStateTypeWriter  : ContentTypeWriter<RasterizerState>
    {
        public RasterizerStateTypeWriter()
        {
        }

        #region implemented abstract members of ContentTypeWriter

        public override void Write(ContentWriter writer, RasterizerState value)
        {
            if (value == null)
            {
                writer.Write(true);
                return;
            }
            writer.Write(false);
            writer.Write((ushort)value.CullMode);
            writer.Write((ushort)value.FillMode);

            writer.Write(value.MultiSampleAntiAlias);
            writer.Write(value.ScissorTestEnable);
        }

        public override string RuntimeReaderName
        {
            get
            {
                return typeof(RasterizerStateTypeReader).FullName;
            }
        }

        #endregion
    }

    [ContentTypeWriterAttribute()]
    public class DepthStencilStateTypeWriter  : ContentTypeWriter<DepthStencilState>
    {
        public DepthStencilStateTypeWriter()
        {
        }

        #region implemented abstract members of ContentTypeWriter

        public override void Write(ContentWriter writer, DepthStencilState value)
        {
            if (value == null)
            {
                writer.Write(true);
                return;
            }
            writer.Write(false);
            writer.Write(value.DepthBufferEnable);
            writer.Write(value.DepthBufferWriteEnable);
            writer.Write(value.StencilEnable);

            writer.Write(value.ReferenceStencil);
            writer.Write(value.StencilMask);

            writer.Write((ushort)value.DepthBufferFunction);
            writer.Write((ushort)value.StencilFunction);
            writer.Write((ushort)value.StencilDepthBufferFail);
            writer.Write((ushort)value.StencilFail);
            writer.Write((ushort)value.StencilPass);
        }

        public override string RuntimeReaderName
        {
            get
            {
                return typeof(DepthStencilStateTypeReader).FullName;
            }
        }

        #endregion
    }

    [ContentTypeWriterAttribute()]
    public class BlendStateTypeWriter  : ContentTypeWriter<BlendState>
    {
        public BlendStateTypeWriter()
        {
        }

        #region implemented abstract members of ContentTypeWriter

        public override void Write(ContentWriter writer, BlendState value)
        {
            if (value == null)
            {
                writer.Write(true);
                return;
            }
            writer.Write(false);
            writer.Write((ushort)value.AlphaBlendFunction);
            writer.Write((ushort)value.AlphaDestinationBlend);
            writer.Write((ushort)value.AlphaSourceBlend);

            writer.Write((ushort)value.ColorBlendFunction);
            writer.Write((ushort)value.ColorDestinationBlend);
            writer.Write((ushort)value.ColorSourceBlend);

            writer.Write(value.BlendFactor);
        }

        public override string RuntimeReaderName
        {
            get
            {
                return typeof(BlendStateTypeReader).FullName;
            }
        }

        #endregion
    }
}

