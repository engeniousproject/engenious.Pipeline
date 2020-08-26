using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using engenious.Pipeline.Extensions;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace engenious.Content.Pipeline
{
    public class AssemblyCreatedContent
    {
        public class CreatedTypeContainer
        {
            public CreatedTypeContainer(Guid buildId, string buildFile)
            {
                BuildId = buildId;
                BuildFile = buildFile;
                
                Types = new ObservableCollection<TypeDefinition>();
            }

            public Guid BuildId { get; set; }
            
            public string BuildFile { get; }
            
            public ObservableCollection<TypeDefinition> Types { get; }
        }

        private readonly Dictionary<string, CustomAttribute> _buildCacheAttributes;
        private readonly MethodDefinition _buildCacheCtor;
        private Dictionary<string, CreatedTypeContainer> TypeContainers { get; }
        private Dictionary<string, TypeDefinition> Types { get; }
        public AssemblyDefinition AssemblyDefinition { get; }
        
        public Dictionary<string, Guid?> MostRecentBuildFileBuildIdMapping { get; }
        public Guid BuildId { get; }

        public AssemblyCreatedContent(AssemblyDefinition assemblyDefinition, Guid buildId)
        {
            AssemblyDefinition = assemblyDefinition;
            BuildId = buildId;
            TypeContainers = new Dictionary<string, CreatedTypeContainer>();
            Types = new Dictionary<string, TypeDefinition>();
            
            _buildCacheAttributes = new Dictionary<string, CustomAttribute>();
            MostRecentBuildFileBuildIdMapping = new Dictionary<string, Guid?>();

            var mainModule = assemblyDefinition.MainModule;

            foreach (var t in mainModule.Types)
            {
                Types.Add(t.FullName, t);
                if (t.Namespace != "engenious.Pipeline" || t.Name != "BuildCacheTypeAttribute")
                    continue;
                _buildCacheCtor = t.GetConstructors().First();

                foreach (var attr in mainModule.CustomAttributes)
                {
                    if (attr.AttributeType != t)
                        continue;
                    var attrBuildId = Guid.Parse(attr.ConstructorArguments[0].Value as string ?? throw new NotSupportedException());
                    var attrBuildFile = attr.ConstructorArguments[1].Value as string ?? throw new NotSupportedException();
                    var attrBuildType = attr.ConstructorArguments[2].Value as string ?? throw new NotSupportedException();

                    if (MostRecentBuildFileBuildIdMapping.TryGetValue(attrBuildFile, out var previousBuildId))
                    {
                        if (previousBuildId != attrBuildId)
                        {
                            MostRecentBuildFileBuildIdMapping[attrBuildFile] = null;
                        }
                    }
                    else
                    {
                        MostRecentBuildFileBuildIdMapping[attrBuildFile] = attrBuildId;
                    }
                    
                    _buildCacheAttributes.Add(attrBuildFile + "/" + attrBuildType, attr);
                }
            }

            if (_buildCacheCtor != null)
                return;

            var attributeBaseType = assemblyDefinition.MainModule.ImportReference(typeof(Attribute));
            var buildCacheAttribute = new TypeDefinition("engenious.Pipeline", "BuildCacheTypeAttribute", TypeAttributes.Class | TypeAttributes.NotPublic, attributeBaseType);

            var (_, buildIdBackingField) = buildCacheAttribute.AddAutoProperty(assemblyDefinition.MainModule.ImportReference(typeof(Guid)), "BuildId", MethodAttributes.Public);
            var (_, buildFileBackingField) = buildCacheAttribute.AddAutoProperty(mainModule.TypeSystem.String, "BuildFile", MethodAttributes.Public);
            var (_, typeNameBackingField) = buildCacheAttribute.AddAutoProperty(mainModule.TypeSystem.String, "TypeName", MethodAttributes.Public);

            var attrUsageAttr = new CustomAttribute(mainModule.ImportReference(typeof(AttributeUsageAttribute).GetConstructor(new [] { typeof(AttributeTargets) })));
            attrUsageAttr.ConstructorArguments.Add(new CustomAttributeArgument(mainModule.ImportReference(typeof(AttributeTargets)), AttributeTargets.Assembly));
            buildCacheAttribute.CustomAttributes.Add(attrUsageAttr);

            _buildCacheCtor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig, mainModule.TypeSystem.Void);

            _buildCacheCtor.Parameters.Add(new ParameterDefinition("buildId", ParameterAttributes.None, mainModule.TypeSystem.String));
            _buildCacheCtor.Parameters.Add(new ParameterDefinition("buildFile", ParameterAttributes.None, buildFileBackingField.FieldType));
            _buildCacheCtor.Parameters.Add(new ParameterDefinition("typeName", ParameterAttributes.None, typeNameBackingField.FieldType));
            
            var processor = _buildCacheCtor.Body.GetILProcessor();
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldarg_1);
            processor.Emit(OpCodes.Call,
                mainModule.ImportReference(typeof(Guid).GetMethod("ToString", BindingFlags.Public | BindingFlags.Instance, null, Array.Empty<Type>(), null)));
            processor.Emit(OpCodes.Stfld, buildIdBackingField);

            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldarg_2);
            processor.Emit(OpCodes.Stfld, buildFileBackingField);
            
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldarg_3);
            processor.Emit(OpCodes.Stfld, typeNameBackingField);
            
            processor.Emit(OpCodes.Ret);
            
            buildCacheAttribute.Methods.Add(_buildCacheCtor);

            mainModule.Types.Add(buildCacheAttribute);
        }
        private void RemoveBuildCacheAttribute(string buildFile, TypeDefinition t)
        {
            if (_buildCacheAttributes.TryGetValue(buildFile + "/" + t.FullName, out var buildCacheAttr))
            {
                AssemblyDefinition.MainModule.CustomAttributes.Remove(buildCacheAttr);
            }
            else
            {
                throw new KeyNotFoundException();
            }
        }
        private void UpdateBuildCacheAttribute(string buildFile, TypeDefinition t)
        {
            if (_buildCacheAttributes.TryGetValue(buildFile + "/" + t.FullName, out var buildCacheAttr))
            {
                buildCacheAttr.ConstructorArguments[0] = new CustomAttributeArgument(buildCacheAttr.ConstructorArguments[0].Type, BuildId.ToString());
            }
            else
            {
                buildCacheAttr = new CustomAttribute(_buildCacheCtor);
                
                buildCacheAttr.ConstructorArguments.Add(new CustomAttributeArgument(_buildCacheCtor.Parameters[0].ParameterType, BuildId.ToString()));
                buildCacheAttr.ConstructorArguments.Add(new CustomAttributeArgument(_buildCacheCtor.Parameters[1].ParameterType, buildFile));
                buildCacheAttr.ConstructorArguments.Add(new CustomAttributeArgument(_buildCacheCtor.Parameters[2].ParameterType, t.FullName));
                    
                AssemblyDefinition.MainModule.CustomAttributes.Add(buildCacheAttr);
            }
        }

        public bool CreatesUserContent(string buildFile)
        {
            return TypeContainers.ContainsKey(buildFile);
        }

        public CreatedTypeContainer AddOrUpdateTypeContainer(string buildFile, Guid guid)
        {
            if (!TypeContainers.TryGetValue(buildFile, out var createdTypeContainer))
            {
                createdTypeContainer = new CreatedTypeContainer(guid, buildFile);
                createdTypeContainer.Types.CollectionChanged += (sender, args) =>
                {
                    switch (args.Action)
                    {
                        case NotifyCollectionChangedAction.Add:
                            foreach(var newType in args.NewItems.OfType<TypeDefinition>())
                            {
                                if (Types.TryGetValue(newType.FullName, out var existingType))
                                {
                                    AssemblyDefinition.MainModule.Types.Remove(existingType);
                                }
                                UpdateBuildCacheAttribute(buildFile, newType);
                                AssemblyDefinition.MainModule.Types.Add(newType);
                                Types[newType.FullName] = newType;
                            }
                            break;
                        case NotifyCollectionChangedAction.Remove:
                            foreach(var removedTypes in args.OldItems.OfType<TypeDefinition>())
                            {
                                RemoveBuildCacheAttribute(buildFile, removedTypes);
                                AssemblyDefinition.MainModule.Types.Remove(removedTypes);
                                Types.Remove(removedTypes.FullName);
                            }
                            break;
                        case NotifyCollectionChangedAction.Replace:
                        case NotifyCollectionChangedAction.Move:
                        case NotifyCollectionChangedAction.Reset:
                        default:
                            throw new NotSupportedException();
                    }
                };
                TypeContainers.Add(buildFile, createdTypeContainer);
            }

            return createdTypeContainer;
        }
    }
}