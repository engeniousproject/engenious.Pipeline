using System;

namespace engenious.Content.Serialization
{
	/// <summary>
	///     Interface for implementing writers for a specific type to write to <see cref="ContentWriter"/>.
	/// </summary>
	public interface IContentTypeWriter
	{
        /// <summary>
        ///     Serializes the data of <paramref name="value"/> to the <see cref="ContentWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="ContentWriter"/> to write the data to.</param>
        /// <param name="value">The value to serialize.</param>
		void Write (ContentWriter writer, object? value);

		/// <summary>
		///     The type name of the <see cref="IContentTypeReader"/>
		///     to use to read data written by this <see cref="IContentTypeWriter"/>.
		/// </summary>
		string RuntimeReaderName{ get; }

		/// <summary>
		///     Gets the type this <see cref="IContentTypeWriter"/> can serialize.
		/// </summary>
		Type RuntimeType{ get; }
		
		/// <summary>
		///     Gets the version of the content type, for serializing different versions of types.
		/// </summary>
		public uint ContentVersion { get; }
	}
}

