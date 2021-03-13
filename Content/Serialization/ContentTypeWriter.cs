using System;

namespace engenious.Content.Serialization
{
	public abstract class ContentTypeWriter<T> : IContentTypeWriter
	{
		protected ContentTypeWriter(uint contentVersion)
		{
			ContentVersion = contentVersion;
		}

		public abstract void Write (ContentWriter writer, T? value);

		void IContentTypeWriter.Write (ContentWriter writer, object? value)
		{
			Write (writer, (T?)value);
		}

		public uint ContentVersion { get; }

		public abstract string RuntimeReaderName{ get; }

		public Type RuntimeType => typeof(T);
	}
}

