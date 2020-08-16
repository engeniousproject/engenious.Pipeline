using System.Collections.Generic;
using engenious.Pipeline;
using Mono.Cecil;

namespace engenious.Content.Pipeline
{
    public class ContentImporterContext : ContentContext
    {
        public ContentImporterContext(AssemblyDefinition createdContentAssembly)
        {
            CreatedContentAssembly = createdContentAssembly;
        }

        public override void Dispose()
        {
        }
        public AssemblyDefinition CreatedContentAssembly { get; }
    }
}

