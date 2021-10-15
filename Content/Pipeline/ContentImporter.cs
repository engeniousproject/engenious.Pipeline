using System;

namespace engenious.Content.Pipeline
{
    /// <summary>
    ///     Base class for generic <see cref="IContentImporter"/>.
    /// </summary>
    /// <typeparam name="T">The type the content importer exports to.</typeparam>
    public abstract class ContentImporter<T> : IContentImporter
    {
        /// <inheritdoc />
        public Type ExportType => _exportType;

        /// <summary>
        ///     The type of <typeparamref name="T"/>, the <see cref="IContentImporter"/> exports to.
        /// </summary>
        protected static readonly Type _exportType = typeof (T);
        
        /// <summary>
        ///     Imports a content file.
        /// </summary>
        /// <param name="filename">The name of the content file to import.</param>
        /// <param name="context">The context for the importing.</param>
        /// <returns>The imported content of type <typeparamref name="T"/> or <c>null</c> if importing failed.</returns>
        /// <seealso cref="IContentImporter.Import"/>
        public abstract T? Import(string filename, ContentImporterContext context);

        object? IContentImporter.Import(string filename, ContentImporterContext context)
        {
            return Import(filename, context);
        }

    }
}

