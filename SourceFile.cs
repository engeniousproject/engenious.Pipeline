using System;
using Mono.Cecil;

namespace engenious.Pipeline
{
    [Serializable]
    public class SourceFile
    {
        public SourceFile(string name, Action<AssemblyDefinition> sourceWriter)
        {
            Name = name;
            SourceWriter = sourceWriter;
        }
        
        public string Name { get; }
        
        public Action<AssemblyDefinition> SourceWriter { get; }
    }
}