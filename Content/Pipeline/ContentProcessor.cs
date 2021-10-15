using System;

namespace engenious.Content.Pipeline
{
    /// <summary>
    ///     Base class for generic processors with custom settings.
    /// </summary>
    /// <typeparam name="TInput">The input type the processor consumes.</typeparam>
    /// <typeparam name="TOutput">The output type the processor produces.</typeparam>
    /// <typeparam name="TSettings">The type of the settings used by this processor.</typeparam>
    public abstract class ContentProcessor<TInput, TOutput, TSettings>
        : IContentProcessor where TSettings : ProcessorSettings, new()
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ContentProcessor{TInput,TOutput,TSettings}"/> class.
        /// </summary>
        protected ContentProcessor()
        {
            _settings = new TSettings();
        }

        /// <summary>
        ///     The <see cref="ImportType"/> used by the pipeline
        /// </summary>
        protected static readonly Type _importType =  typeof(TInput);

        /// <summary>
        ///     The <see cref="ExportType"/> used by the pipeline
        /// </summary>
        protected static readonly Type _exportType = typeof(TOutput);

        /// <inheritdoc />
        public Type ImportType => _importType;

        /// <inheritdoc />
        public Type ExportType => _exportType;
        
        /// <summary>
        ///     The generic <see cref="ProcessorSettings"/> of type <typeparamref name="TSettings"/>.
        /// </summary>
        [System.Xml.Serialization.XmlIgnore()]
        protected TSettings _settings;

        /// <inheritdoc />
        public ProcessorSettings? Settings
        {
            get => _settings;
            set => _settings = (TSettings?)value ?? new TSettings();
        }

        /// <summary>
        ///     Processes imported input from <see cref="IContentImporter"/>.
        /// </summary>
        /// <param name="input">The imported input of type <typeparamref name="TInput"/> to process.</param>
        /// <param name="filename">The name of the content file to process.</param>
        /// <param name="context">The context for the processing.</param>
        /// <returns>The processed content of type <typeparamref name="TOutput"/> or <c>null</c> if processing failed.</returns>
        /// <seealso cref="IContentProcessor.Process"/>
        public abstract TOutput? Process(TInput input, string filename, ContentProcessorContext context);

        object? IContentProcessor.Process(object input, string filename, ContentProcessorContext context)
        {
            return Process((TInput)input,filename, context);
        }
    }

    /// <summary>
    ///     Base class for generic processors without custom settings.
    /// </summary>
    /// <typeparam name="TInput">The input type the processor consumes.</typeparam>
    /// <typeparam name="TOutput">The output type the processor produces.</typeparam>
    public abstract class ContentProcessor<TInput, TOutput> : ContentProcessor<TInput, TOutput, ProcessorSettings>
    {
    }
}