using engenious.Graphics;

namespace engenious.Content.Serialization
{
    /// <summary>
    ///     Content type writer to serialize engenious rasterizer states.
    /// </summary>
    [ContentTypeWriter]
    public class RasterizerStateTypeWriter  : ContentTypeWriter<RasterizerState>
    {
        #region implemented abstract members of ContentTypeWriter

        /// <inheritdoc />
        public override void Write(ContentWriter writer, RasterizerState? value)
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
            
            writer.Write(value.DepthBias);
            writer.Write(value.SlopeScaleDepthBias);
        }

        /// <inheritdoc />
        public override string RuntimeReaderName => typeof(RasterizerStateTypeReader).FullName!;

        #endregion

        /// <summary>
        ///     Initializes a new instance of the <see cref="RasterizerStateTypeWriter"/> class.
        /// </summary>
        public RasterizerStateTypeWriter()
            : base(0)
        {
        }
    }

    /// <summary>
    ///     Content type writer to serialize engenious depth stencil states.
    /// </summary>
    [ContentTypeWriter]
    public class DepthStencilStateTypeWriter : ContentTypeWriter<DepthStencilState>
    {
        #region implemented abstract members of ContentTypeWriter

        /// <inheritdoc />
        public override void Write(ContentWriter writer, DepthStencilState? value)
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
            
            writer.Write(value.TwoSidedStencilMode);
            writer.Write((ushort)value.CounterClockwiseStencilFunction);
            writer.Write((ushort)value.CounterClockwiseStencilDepthBufferFail);
            writer.Write((ushort)value.CounterClockwiseStencilFail);
            writer.Write((ushort)value.CounterClockwiseStencilPass);
        }

        /// <inheritdoc />
        public override string RuntimeReaderName => typeof(DepthStencilStateTypeReader).FullName!;

        #endregion

        /// <summary>
        ///     Initializes a new instance of the <see cref="DepthStencilStateTypeWriter"/> class.
        /// </summary>
        public DepthStencilStateTypeWriter()
            : base(0)
        {
        }
    }

    /// <summary>
    ///     Content type writer to serialize engenious blend states.
    /// </summary>
    [ContentTypeWriter]
    public class BlendStateTypeWriter  : ContentTypeWriter<BlendState>
    {
        #region implemented abstract members of ContentTypeWriter

        /// <inheritdoc />
        public override void Write(ContentWriter writer, BlendState? value)
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

            writer.Write((byte)value.ColorWriteChannels);
            writer.Write((byte)value.ColorWriteChannels1);
            writer.Write((byte)value.ColorWriteChannels2);
            writer.Write((byte)value.ColorWriteChannels3);
        }

        /// <inheritdoc />
        public override string RuntimeReaderName => typeof(BlendStateTypeReader).FullName!;

        #endregion

        /// <summary>
        ///     Initializes a new instance of the <see cref="BlendStateTypeWriter"/> class.
        /// </summary>
        public BlendStateTypeWriter()
            : base(0)
        {
        }
    }
}

