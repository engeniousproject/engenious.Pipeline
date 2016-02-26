using System;

namespace engenious.Content.Pipeline
{
	public abstract class ContentImporter<T> : IContentImporter
	{
		public ContentImporter ()
		{
		}

		public abstract T Import (string filename, ContentImporterContext context);

		object IContentImporter.Import (string filename, ContentImporterContext context)
		{
			return Import (filename, context);
		}

	}
}

