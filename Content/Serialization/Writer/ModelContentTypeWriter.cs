using System;
using engenious.Graphics;

namespace engenious.Content.Serialization
{
    /// <summary>
    ///     Content type writer to serialize engenious model content.
    /// </summary>
    [ContentTypeWriter]
    public class ModelContentTypeWriter : ContentTypeWriter<ModelContent>
    {
        /// <inheritdoc />
        public override string RuntimeReaderName => typeof(ModelTypeReader).FullName!;

        private static void WriteTree(ContentWriter writer, ModelContent value, NodeContent node)
        {
            int index = value.Nodes.IndexOf(node);
            writer.Write(index);
            writer.Write(node.Children.Count);
            foreach (var c in node.Children)
                WriteTree(writer, value, c);
        }

        /// <inheritdoc />
        public override void Write(ContentWriter writer, ModelContent? value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value), "Cannot write null ModelContent");
            writer.Write(value.Meshes.Length);
            foreach (var m in value.Meshes)
            {
                writer.Write(m.PrimitiveCount);
                writer.Write(m.Vertices.HasPositions);
                writer.Write(m.Vertices.HasColors);
                writer.Write(m.Vertices.HasNormals);
                writer.Write(m.Vertices.HasTextureCoordinates);
                writer.Write(m.Vertices.Length);
                for (int i = 0; i < m.Vertices.Length; i++)
                {
                    if (m.Vertices.HasPositions)
                    {
                        writer.Write(m.Vertices.AsPosition![i]);
                    }
                    if (m.Vertices.HasColors)
                    {
                        writer.Write(m.Vertices.AsColor![i]);
                    }

                    if (m.Vertices.HasNormals)
                    {
                        writer.Write(m.Vertices.AsNormal![i]);
                    }

                    if (m.Vertices.HasTextureCoordinates)
                    {
                        writer.Write(m.Vertices.AsTextureCoordinate![i]);
                    }
                }
            }
            writer.Write(value.Nodes.Count);
            foreach (var n in value.Nodes)
            {
                writer.Write(n.Name);
                writer.Write(n.Transformation);
                writer.Write(n.Meshes.Count);
                foreach (var m in n.Meshes)
                    writer.Write(m);
            }
            WriteTree(writer, value, value.RootNode ?? throw new ArgumentException("Cannot write a model without a valid RootNode", nameof(value)));

            writer.Write(value.Animations.Count);
            foreach(var anim in value.Animations){
                writer.Write(anim.MaxTime);
                writer.Write(anim.Channels.Count);
                foreach (var c in anim.Channels)
                {
                    int nodeIndex = value.Nodes.IndexOf(c.Node);
                    writer.Write(nodeIndex);
                    writer.Write(c.Frames.Count);
                    foreach (var f in c.Frames)
                    {
                        writer.Write(f.Frame);
                        writer.Write(f.Transform.Location);
                        writer.Write(f.Transform.Scale);
                        writer.Write(f.Transform.Rotation);
                    }
                }
            }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ModelContentTypeWriter"/> class.
        /// </summary>
        public ModelContentTypeWriter()
            : base(0)
        {
        }
    }
}

