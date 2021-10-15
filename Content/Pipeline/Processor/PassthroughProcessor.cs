using engenious.Content.Pipeline;

namespace engenious.Pipeline
{
    /// <summary>
    ///     Processor that does nothing to its input and just passes it through.
    /// </summary>
    [ContentProcessor(DisplayName = "Passtrough Processor")]
    public class PassthroughProcessor : ContentProcessor<object, object>
    {
        #region implemented abstract members of ContentProcessor

        /// <inheritdoc />
        public override object Process(object input, string filename, ContentProcessorContext context)
        {
            return input;
        }

        #endregion
    }
}

