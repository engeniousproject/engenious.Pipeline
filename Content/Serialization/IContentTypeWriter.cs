using System;

namespace engenious.Content.Serialization
{
	public interface IContentTypeWriter
	{
		void Write (ContentWriter writer, object? value);

		string RuntimeReaderName{ get; }

		Type RuntimeType{ get; }
	}
}

