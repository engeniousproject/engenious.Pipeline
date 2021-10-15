using System;

namespace engenious.Content.Serialization
{
	/// <summary>
	///     Generic base class for content writer for specific type.
	/// </summary>
	/// <typeparam name="T">The type the <see cref="ContentTypeWriter{T}"/> can write.</typeparam>
	public abstract class ContentTypeWriter<T> : IContentTypeWriter
	{
		/// <summary>
		///     Initializes a new instance of the <see cref="ContentTypeWriter{T}"/> class.
		/// </summary>
		/// <param name="contentVersion">
		///     The version of the content type, for serializing different versions of types.
	    /// </param>
		protected ContentTypeWriter(uint contentVersion)
		{
			ContentVersion = contentVersion;
		}

		/// <summary>
		///     Serializes the data of <paramref name="value"/> to the <see cref="ContentWriter"/>.
		/// </summary>
		/// <param name="writer">The <see cref="ContentWriter"/> to write the data to.</param>
		/// <param name="value">The value to serialize.</param>
		public abstract void Write (ContentWriter writer, T? value);

		void IContentTypeWriter.Write (ContentWriter writer, object? value)
		{
			Write (writer, (T?)value);
		}

        /// <inheritdoc />
        public uint ContentVersion { get; }

        /// <inheritdoc />
        public abstract string RuntimeReaderName{ get; }

        /// <inheritdoc />
        public Type RuntimeType => typeof(T);
	}
}

