using System;

namespace engenious.Content.Serialization
{
	/// <summary>
	///     Attribute to apply to <see cref="IContentTypeWriter"/> implementations.
	/// </summary>
	[AttributeUsageAttribute (AttributeTargets.Class)]
	public sealed class ContentTypeWriterAttribute : Attribute
	{
	}
}

