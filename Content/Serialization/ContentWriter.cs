using System;
using System.IO;
using System.Text;
using engenious.Graphics;

namespace engenious.Content.Serialization
{
    /// <summary>
    ///     An extended binary writer able to read some basic engenious types.
    /// </summary>
    public sealed class ContentWriter : BinaryWriter
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ContentWriter"/> class.
        /// </summary>
        /// <param name="output">The stream to write the output to.</param>
        public ContentWriter(Stream output)
            : base(output)
        {
        }

        /// <summary>
        ///     Writes an object to the <see cref="ContentWriter"/>.
        /// </summary>
        /// <param name="value">The object value to write.</param>
        /// <exception cref="ArgumentException">
        ///     Thrown when no matching writer was found for the <paramref name="value"/>.
        /// </exception>
        public void WriteObject(object value)
        {
            var typeWriter = SerializationManager.Instance.GetWriter(value.GetType());
            if (typeWriter == null)
                throw new ArgumentException("No valid type writer found for object.", nameof(value));
            Write(typeWriter.RuntimeReaderName);
            typeWriter.Write(this, value);
        }

        /// <summary>
        ///     Writes an object to the <see cref="ContentWriter"/>.
        /// </summary>
        /// <param name="value">The object value to write.</param>
        /// <typeparam name="T">The type of the value to write.</typeparam>
        /// <exception cref="ArgumentException">
        ///     Thrown when no matching writer was found for the <paramref name="value"/>.
        /// </exception>
        public void WriteObject<T>(T value)
        {
            var typeWriter = SerializationManager.Instance.GetWriter(typeof(T));
            if (typeWriter == null)
                throw new ArgumentException("No valid type writer found for object.", nameof(value));
            WriteObject(value, typeWriter);
        }

        /// <summary>
        ///     Writes an object to the <see cref="ContentWriter"/> using a specific <see cref="IContentTypeWriter"/>.
        /// </summary>
        /// <param name="value">The object value to write.</param>
        /// <param name="typeWriter">
        ///     The <see cref="IContentTypeWriter"/> to use for writing the <paramref name="value"/>.
        /// </param>
        /// <typeparam name="T">The type of the value to write.</typeparam>
        public void WriteObject<T>(T value, IContentTypeWriter typeWriter)
        {
            // if (value == null) throw new ArgumentNullException(nameof(value));
            Write(typeWriter.RuntimeReaderName);
            typeWriter.Write(this, value);
        }

        /// <summary>
        ///     Writes the content of a stream to the content writer.
        /// </summary>
        /// <param name="stream">The stream to write.</param>
        /// <param name="length">
        ///     The length to write of the stream; or <c>-1</c> to write all content of the stream.
        /// </param>
        public void Write(Stream stream, int length = -1)
        {
            Stream buffered = new BufferedStream(stream);
            byte[] buffer = new byte[1024*1024];
            int readLen = length == -1 ? buffer.Length : (int)stream.Length;
            int toRead = Math.Min(readLen, buffer.Length);
            int read;
            while ((read = buffered.Read(buffer, 0, toRead)) > 0)
            {
                Write(buffer, 0, read);
            }
            if (read > 0)
                Write(buffer, 0, read);
            buffered.Close();
            buffered.Dispose();
        }

        /// <summary>
        ///     Writes a <see cref="VertexPositionNormalTexture"/> to this stream. The current position of the stream is advanced by 64 byte.
        /// </summary>
        /// <param name="v">The vertex element to write.</param>
        public void Write(VertexPositionNormalTexture v)
        {
            Write(v.Position);
            Write(v.Normal);
            Write(v.TextureCoordinate);
        }

        /// <summary>
        ///     Writes a <see cref="VertexPositionColor"/> to this stream. The current position of the stream is advanced by 28 byte.
        /// </summary>
        /// <param name="v">The vertex element to write.</param>
        public void Write(VertexPositionColor v)
        {
            Write(v.Position);
            Write(v.Color);
        }

        /// <summary>
        ///     Writes a <see cref="VertexPositionColorTexture"/> to this stream. The current position of the stream is advanced by 36 byte.
        /// </summary>
        /// <param name="v">The vertex element to write.</param>
        public void Write(VertexPositionColorTexture v)
        {
            Write(v.Position);
            Write(v.Color);
            Write(v.TextureCoordinate);
        }

        /// <summary>
        ///     Writes a <see cref="VertexPositionTexture"/> to this stream. The current position of the stream is advanced by 20 byte.
        /// </summary>
        /// <param name="v">The vertex element to write.</param>
        public void Write(VertexPositionTexture v)
        {
            Write(v.Position);
            Write(v.TextureCoordinate);
        }

        /// <summary>
        ///     Writes a <see cref="Matrix"/> to this stream. The current position of the stream is advanced by 64 byte.
        /// </summary>
        /// <param name="matrix">The matrix to write.</param>
        public void Write(Matrix matrix)
        {
            Write(matrix.Row0);//TODO: perhaps better saving per Column?
            Write(matrix.Row1);
            Write(matrix.Row2);
            Write(matrix.Row3);
        }

        /// <summary>
        ///     Writes a <see cref="Quaternion"/> to this stream. The current position of the stream is advanced by 16 byte.
        /// </summary>
        /// <param name="quaternion">The quaternion to write.</param>
        public void Write(Quaternion quaternion)
        {
            Write(quaternion.X);
            Write(quaternion.Y);
            Write(quaternion.Z);
            Write(quaternion.W);
        }

        /// <summary>
        ///     Writes a <see cref="Vector2"/> to this stream. The current position of the stream is advanced by 8 byte.
        /// </summary>
        /// <param name="vector">The 2 dimensional vector to write.</param>
        public void Write(Vector2 vector)
        {
            Write(vector.X);
            Write(vector.Y);
        }

        /// <summary>
        ///     Writes a <see cref="Vector3"/> to this stream. The current position of the stream is advanced by 12 byte.
        /// </summary>
        /// <param name="vector">The 3 dimensional vector to write.</param>
        public void Write(Vector3 vector)
        {
            Write(vector.X);
            Write(vector.Y);
            Write(vector.Z);
        }

        /// <summary>
        ///     Writes a <see cref="Vector4"/> to this stream. The current position of the stream is advanced by 16 byte.
        /// </summary>
        /// <param name="vector">The 4 dimensional vector to write.</param>
        public void Write(Vector4 vector)
        {
            Write(vector.X);
            Write(vector.Y);
            Write(vector.Z);
            Write(vector.W);
        }

        /// <summary>
        ///     Writes a <see cref="Color"/> to this stream. The current position of the stream is advanced by 16 byte.
        /// </summary>
        /// <param name="color">The RGBA color to write.</param>
        public void Write(Color color)
        {
            Write(color.R);
            Write(color.G);
            Write(color.B);
            Write(color.A);
        }

        /// <summary>
        ///     Write a single <see cref="Rune"/> to this stream. The current position of the stream is advanced by 32 byte.
        /// </summary>
        /// <param name="rune">The rune to write.</param>
        public void Write(Rune rune)
        {
            Write(rune.Value);
        }
    }
}

