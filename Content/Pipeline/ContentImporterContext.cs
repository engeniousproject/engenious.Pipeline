using System;
using Mono.Cecil;

namespace engenious.Content.Pipeline
{
    public class ContentImporterContext : ContentContext
    {
        public ContentImporterContext(Guid buildId, AssemblyCreatedContent assemblyCreatedContent, string contentDirectory)
            : base(buildId, assemblyCreatedContent, contentDirectory)
        {
        }

        public override void Dispose()
        {
        }
    }
}

