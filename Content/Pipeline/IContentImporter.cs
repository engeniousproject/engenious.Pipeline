using System;

namespace engenious.Content.Pipeline
{
	public interface IContentImporter
	{
        Type ExportType { get; }

        object Import (string filename, ContentImporterContext context);
	}
}

