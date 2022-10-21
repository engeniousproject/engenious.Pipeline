using System;
using System.Collections.Generic;

namespace engenious.Content.Pipeline
{
    /// <summary>
    ///     Base class for generic <see cref="IContentImporter"/>.
    /// </summary>
    /// <typeparam name="T">The type the content importer exports to.</typeparam>
    public abstract class ContentImporter<T> : ContentImporterBase<T, ValueTuple>
    {
        
        /// <inheritdoc />
        public override ValueTuple DependencyImport(string filename, ContentImporterContext context, ICollection<string> dependencies)
        {
            return default;
        }

        /// <inheritdoc />
        public override T? Import(string filename, ContentImporterContext context, ValueTuple dependencyImport)
        {
            return Import(filename, context);
        }

        /// <inheritdoc />
        public override object? Import(string filename, ContentImporterContext context, object? dependencyImport)
        {
            return Import(filename, context);
        }
    }

    /// <summary>
    ///     Base class for generic <see cref="IContentImporter"/>.
    /// </summary>
    /// <typeparam name="TExport">The type the content importer exports to.</typeparam>
    /// <typeparam name="TDependency">The type the content importer imports with.</typeparam>
    public abstract class ContentImporter<TExport, TDependency> : ContentImporterBase<TExport, TDependency>
    {
        /// <inheritdoc />
        public override TExport? Import(string filename, ContentImporterContext context)
        {
            return Import(filename, context, default);
        }

        /// <inheritdoc />
        public override object? Import(string filename, ContentImporterContext context, object? dependencyImport)
        {
            return Import(filename, context, (TDependency?)dependencyImport);
        }
    }

    /// <summary>
    ///     Base class for generic <see cref="IContentImporter"/>.
    /// </summary>
    /// <typeparam name="TExport">The type the content importer exports to.</typeparam>
    /// <typeparam name="TDependency">The type the content importer imports with.</typeparam>
    public abstract class ContentImporterBase<TExport, TDependency> : IContentImporter<TExport, TDependency>
    {
        /// <inheritdoc />
        public Type? DependencyType => _dependencyType;

        /// <inheritdoc />
        public Type ExportType => _exportType;

        /// <inheritdoc />
        public abstract TDependency? DependencyImport(string filename, ContentImporterContext context, ICollection<string> dependencies);

        /// <inheritdoc />
        public object? DependencyImportBase(string filename, ContentImporterContext context, ICollection<string> dependencies)
        {
            return DependencyImport(filename, context, dependencies);
        }

        /// <inheritdoc />
        public abstract object? Import(string filename, ContentImporterContext context,
            object? dependencyImport);

        /// <summary>
        ///     The type of <typeparamref name="TDependency"/>, the <see cref="IContentImporter"/> imports with.
        /// </summary>
        protected static readonly Type? _dependencyType = typeof(TExport) == typeof(ValueTuple) ? null : typeof(TExport);

        /// <summary>
        ///     The type of <typeparamref name="TExport"/>, the <see cref="IContentImporter"/> exports to.
        /// </summary>
        protected static readonly Type _exportType = typeof (TExport);

        /// <inheritdoc />
        public abstract TExport? Import(string filename, ContentImporterContext context);

        /// <inheritdoc />
        public abstract TExport? Import(string filename, ContentImporterContext context, TDependency? dependencyImport);
    }

    /// <summary>
    ///     Base interface for generic <see cref="IContentImporter"/>.
    /// </summary>
    /// <typeparam name="TExport">The type the content importer exports to.</typeparam>
    /// <typeparam name="TDependency">The type the content importer imports with.</typeparam>
    public interface IContentImporter<out TExport, TDependency> : IContentImporter
    {
        /// <summary>
        ///     Imports a content file.
        /// </summary>
        /// <param name="filename">The name of the content file to import.</param>
        /// <param name="context">The context for the importing.</param>
        /// <returns>The imported content of type <typeparamref name="TExport"/> or <c>null</c> if importing failed.</returns>
        /// <seealso cref="IContentImporter.Import"/>
        TExport? Import(string filename, ContentImporterContext context);

        /// <summary>
        ///     Imports a content file.
        /// </summary>
        /// <param name="filename">The name of the content file to import.</param>
        /// <param name="context">The context for the importing.</param>
        /// <param name="dependencyImport">The dependency type instance to import with.</param>
        /// <returns>The imported content of type <typeparamref name="TExport"/> or <c>null</c> if importing failed.</returns>
        /// <seealso cref="IContentImporter.Import"/>
        TExport? Import(string filename, ContentImporterContext context, TDependency? dependencyImport);

        /// <summary>
        ///     Imports dependencies for a content file.
        /// </summary>
        /// <param name="filename">The name of the content file to import dependencies from.</param>
        /// <param name="context">The context for the importing.</param>
        /// <param name="dependencies">The list to add collected dependencies to.</param>
        /// <returns>The imported dependency import of type <typeparamref name="TDependency"/> or <c>null</c> if importing failed.</returns>
        /// <seealso cref="IContentImporter.DependencyImportBase"/>
        TDependency? DependencyImport(string filename, ContentImporterContext context, ICollection<string> dependencies);
    }
}

