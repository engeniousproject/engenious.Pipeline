using System;

namespace engenious.Content.Pipeline
{
	public interface IContentImporter
	{
		object Import (string filename, ContentImporterContext context);
	}
}

