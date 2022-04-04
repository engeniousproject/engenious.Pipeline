using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using engenious.Graphics;
using engenious.Pipeline.Helper;
using engenious.Content.CodeGenerator;

namespace engenious.Content.Pipeline
{
    /// <summary>
    ///     Processor for processing effect content to content files.
    /// </summary>
    [ContentProcessor(DisplayName = "Effect Processor")]
    public class EffectProcessor : ContentProcessor<EffectContent, EffectContent, EffectProcessorSettings>
    {
        private static BuildMessageEventArgs.BuildMessageType GetMessageType(string line)
        {
            var splt = line.Split(new[] { ':' }, StringSplitOptions.None);
            var messageType = BuildMessageEventArgs.BuildMessageType.None;
            foreach (var s in splt)
            {
                var trimmed = s.Trim();
                if (trimmed.StartsWith("error", StringComparison.InvariantCultureIgnoreCase))
                {
                    messageType = BuildMessageEventArgs.BuildMessageType.Error;
                }
                else if (trimmed.StartsWith("warning", StringComparison.InvariantCultureIgnoreCase))
                {
                    messageType = BuildMessageEventArgs.BuildMessageType.Warning;
                }
                else if (trimmed.StartsWith("info", StringComparison.InvariantCultureIgnoreCase))
                {
                    messageType = BuildMessageEventArgs.BuildMessageType.Information;
                }
            }

            return messageType;
        }

        private int GetMinGreaterZero(int a, int b)
        {
            if (a < 0)
                return b;
            if (b < 0)
                return a;
            return Math.Min(a, b);
        }

        private void PreprocessMessage(ContentProcessorContext context, string file, string msg,
            BuildMessageEventArgs.BuildMessageType messageType)
        {
            string[] lines = msg.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                messageType = GetMessageType(lines[i]);
                if (messageType == BuildMessageEventArgs.BuildMessageType.Error ||
                    messageType == BuildMessageEventArgs.BuildMessageType.Warning)
                {
                    int sInd = GetMinGreaterZero(lines[i].IndexOf("0:", StringComparison.Ordinal),
                        lines[i].IndexOf("0(", StringComparison.Ordinal));
                    string errorLoc = string.Empty;
                    if (sInd != -1)
                    {
                        lines[i] = lines[i].Substring(sInd + 2);
                        int eInd = GetMinGreaterZero(lines[i].IndexOf(':'), lines[i].IndexOf(')'));
                        if (eInd != -1)
                        {
                            errorLoc = lines[i].Substring(0, eInd).Replace('(', ',');
                            if (errorLoc.IndexOf(',') == -1)
                                errorLoc = errorLoc + ",1";
                            lines[i] = lines[i].Substring(eInd + 3).Trim();
                        }
                    }

                    lines[i] = file + "(" + errorLoc + "): " + lines[i];
                }

                context.RaiseBuildMessage(file, lines[i], messageType);
            }
        }

        private void GenerateEffectSource(string filename, EffectContent input, string @namespace, string name,
            ContentProcessorContext context)
        {
            var createdTypeContainer =
                context.CreatedContentCode.AddOrUpdateTypeContainer(context.GetRelativePathToContentDirectory(filename),
                    context.BuildId);
            string typeNamespace = "engenious.UserDefined" +
                                   (string.IsNullOrEmpty(@namespace) ? string.Empty : "." + @namespace);

            input.UserEffectName = $"{typeNamespace}.{name}";
            var baseType = new TypeReference("engenious.Graphics", "Effect");
            var effectTechniqueCollectionType = new TypeReference("engenious.Graphics", "EffectTechniqueCollection");
            var graphicsDeviceType = new TypeReference("engenious.Graphics", "GraphicsDevice");
            var typeDefinition = new TypeDefinition(typeNamespace, TypeModifiers.Class | TypeModifiers.Public | TypeModifiers.Partial, name,
                new[] { baseType }, $"/// <summary>Implementation for the {name} effect.</summary>");
            createdTypeContainer.FileDefinition.Types.Remove(typeDefinition.FullName);
            createdTypeContainer.FileDefinition.Types.Add(typeDefinition.FullName, typeDefinition);
            var ctor = new ConstructorDefinition(typeDefinition, MethodModifiers.Public,
                new[] { new ParameterDefinition(graphicsDeviceType, "graphicsDevice", "The graphics device for the effect.") },
                MethodBodyDefinition.EmptyBody,
                $"/// <summary>Initializes a new instance of the <see cref=\"{name}\"/> class.</summary>",
                new CodeExpressionDefinition[] { new SimpleExpressionDefinition("base(graphicsDevice)") });

            typeDefinition.Methods.Add(ctor);

            var initializeMethod = new ImplementedMethodDefinition(
                new SignatureDefinition(MethodModifiers.Protected | MethodModifiers.Override, TypeSystem.Void,
                    "Initialize",
                    Array.Empty<ParameterDefinition>()), null, "/// <inheritdoc />");

            using var expressionBuilder = new ExpressionBuilder();

            expressionBuilder.Append("base.Initialize();");
            expressionBuilder.Append("var techniques = Techniques;");
            foreach (var technique in input.Techniques)
            {
                var techniqueType = GenerateEffectTechniqueSource(typeDefinition, technique, createdTypeContainer);
                var p = typeDefinition.AddAutoProperty(MethodModifiers.Public, techniqueType, technique.Name,
                    setterModifiers: MethodModifiers.Private,
                    comment: $"/// <summary>Gets the <see cref=\"{technique.Name}\"/> technique.</summary>");

                expressionBuilder.Append($"{p.Name} = techniques[\"{technique.Name}\"] as {techniqueType.Name};");
            }

            initializeMethod = initializeMethod with
                               {
                                   MethodBody = new MethodBodyDefinition(expressionBuilder.ToExpression())
                               };

            typeDefinition.Methods.Add(initializeMethod);
        }

        struct ParameterReference
        {
            public ParameterReference(EffectPass pass, ParameterInfo info)
            {
                Pass = pass;
                ParameterInfo = info;
            }

            public EffectPass Pass;
            public ParameterInfo ParameterInfo;
        }

        private TypeDefinition GenerateEffectTechniqueSource(TypeDefinition parent, EffectTechnique technique,
            CreatedContentCode.CreatedTypeContainer createdTypeContainer)
        {
            technique.UserTechniqueName = $"{technique.Name}Impl";

            var baseType = new TypeReference("engenious.Graphics", "EffectTechnique");
            var effectPassCollectionType = new TypeReference("engenious.Graphics", "EffectPassCollection");
            var graphicsDeviceType = new TypeReference("engenious.Graphics", "GraphicsDevice");
            var typeDefinition = new TypeDefinition(string.Empty, TypeModifiers.Class | TypeModifiers.Public | TypeModifiers.Partial,
                technique.UserTechniqueName,
                new[] { baseType },
                $"/// <summary>Implementation for the {technique.Name} effect technique</summary>");

            parent.NestedTypes.Add(typeDefinition);
            var ctor = new ConstructorDefinition
            (
                typeDefinition, MethodModifiers.Public,
                new[] { new ParameterDefinition(TypeSystem.String, "name", "The name of the effect technique.") }, MethodBodyDefinition.EmptyBody,
                $"/// <summary>Initializes a new instance of the <see cref=\"{technique.UserTechniqueName}\" /> class.</summary>",
                new CodeExpressionDefinition[] { new SimpleExpressionDefinition("base(name)") }
            );
            typeDefinition.Methods.Add(ctor);

            var initializeMethod = new ImplementedMethodDefinition(
                new SignatureDefinition(MethodModifiers.Override | MethodModifiers.Protected, TypeSystem.Void,
                    "Initialize", Array.Empty<ParameterDefinition>()), null, "/// <inheritdoc />");

            using var initializeMethodBuilder = new ExpressionBuilder();
            initializeMethodBuilder.Append("base.Initialize();");
            initializeMethodBuilder.Append("var passes = Passes;");

            var passTypeDefinitions = new Dictionary<string, (TypeDefinition, PropertyDefinition)>();

            foreach (var pass in technique.Passes)
            {
                var passType = GenerateEffectPassSource(typeDefinition, pass, createdTypeContainer);
                if (passType == null)
                {
                    throw new Exception($"Unable to create IL code for {pass.Name} EffectPass");
                }

                var p = typeDefinition.AddAutoProperty(MethodModifiers.Public, passType, pass.Name,
                    setterModifiers: MethodModifiers.Private, comment: $"/// <summary>Gets the {pass.Name} pass.</summary>");

                initializeMethodBuilder.Append($"{p.Name} = passes[\"{pass.Name}\"] as {passType.Name};");

                passTypeDefinitions.Add(pass.Name, (passType, p));
            }

            initializeMethod = initializeMethod with
                               {
                                   MethodBody = new MethodBodyDefinition(initializeMethodBuilder.ToExpression())
                               };
            typeDefinition.Methods.Add(initializeMethod);

            var parameters = new Dictionary<string, List<ParameterReference>?>();
            foreach (var pass in technique.Passes)
            {
                foreach (var param in pass.Parameters)
                {
                    if (param.Name.StartsWith('\''))
                        continue;
                    if (!parameters.TryGetValue(param.Name, out var paramList))
                    {
                        paramList = new List<ParameterReference>();
                    }

                    if (paramList == null)
                        continue;

                    bool isCompatible = true;
                    foreach (var otherParam in paramList)
                    {
                        if (otherParam.ParameterInfo.Type == param.Type)
                            continue;

                        isCompatible = false;
                        break;
                    }

                    if (isCompatible)
                        paramList.Add(new ParameterReference(pass, param));
                    else
                        paramList = null;

                    parameters[param.Name] = paramList;
                }
            }


            foreach (var (name, parameterReferences) in parameters)
            {
                if (parameterReferences == null || parameterReferences.Count == 0)
                    continue;
                var propertyType = new TypeReference(parameterReferences[0].ParameterInfo.Type.Namespace,
                    parameterReferences[0].ParameterInfo.Type.Name);

                bool isPrimitive = (parameterReferences[0].ParameterInfo is not ArrayParameterInfo && parameterReferences[0].ParameterInfo is not StructParameterInfo);

                if (parameterReferences[0].ParameterInfo is ArrayParameterInfo arrayParameterInfo)
                {
                    propertyType = new TypeReference(null, $"{parameterReferences[0].Pass.Name}Impl.{arrayParameterInfo.Name}Array");
                }
                else if (parameterReferences[0].ParameterInfo is StructParameterInfo structParameterInfo)
                {
                    propertyType = new TypeReference(null, $"{parameterReferences[0].Pass.Name}Impl.{structParameterInfo.Name.TrimStart('\'')}Wrapper");
                }

                if (!isPrimitive && parameterReferences.Count > 1)
                    continue;

                ExpressionBuilder? setterWriter = null;
                ExpressionBuilder? getterWriter = null;

                static MethodModifiers GetModifier(MethodModifiers parent, MethodModifiers mod)
                {
                    return mod == MethodModifiers.None ? parent : mod;
                }

                
                foreach (var parameterReference in parameterReferences)
                {
                    var (passType, passProperty) = passTypeDefinitions[parameterReference.Pass.Name];
                    var propInPass = passType.Properties.First(x => x.Name == name);
                    if (isPrimitive)
                    {
                        if ((GetModifier(propInPass.Modifiers, propInPass.SetterModifiers)
                             & (MethodModifiers.Public | MethodModifiers.Internal)) != 0)
                        {
                            setterWriter ??= new();
                            setterWriter.Append($"{passProperty.Name}.{name} = value");
                        }
                        if (getterWriter is null && ((GetModifier(propInPass.Modifiers, propInPass.GetterModifiers)
                                                      & (MethodModifiers.Public | MethodModifiers.Internal)) != 0))
                        {
                            getterWriter = new();
                            getterWriter.Append($"{passProperty.Name}.{name}");
                        }
                    }
                    else if(getterWriter is null && ((GetModifier(propInPass.Modifiers, propInPass.GetterModifiers)
                                                      & (MethodModifiers.Public | MethodModifiers.Internal)) != 0))
                    {
                        getterWriter = new();
                        getterWriter.Append($"{passProperty.Name}.{name}");
                    }
                }

                var setterM = setterWriter is null
                    ? null : new ImplementedPropertyMethodDefinition(new MethodBodyDefinition(setterWriter.ToExpression()),
                    true);
                var getterM = getterWriter is null
                    ? null : new ImplementedPropertyMethodDefinition(new MethodBodyDefinition(getterWriter.ToExpression()),
                    false);
                var prop = new PropertyDefinition(
                    MethodModifiers.Public,
                    propertyType, name,
                    getterM,
                    isPrimitive ? setterM : null,
                    Comment: $"/// <summary>{(isPrimitive ? "Sets or gets" : "Gets")} the {name} parameter.</summary>");

                typeDefinition.Properties.Add(prop);
            }

            return typeDefinition;
        }

        private bool TryGenerateArray(TypeDefinition parent, ParameterInfo parameterInfo, ExpressionBuilder cacheParametersWriter, [MaybeNullWhen(false)] out ArrayParameterInfo arrayParameterInfo)
        {
            arrayParameterInfo = parameterInfo as ArrayParameterInfo;
            if (arrayParameterInfo == null)
                return false;

            var typeDef = new TypeDefinition(string.Empty, TypeModifiers.Class | TypeModifiers.Public | TypeModifiers.Partial,
                $"{arrayParameterInfo.Name}Array", $"/// <summary>Wrapper class for the <c>{arrayParameterInfo.Name}</c> array.</summary>");
            
            bool isPrimitive = !(arrayParameterInfo.Type.Name.EndsWith("Wrapper") || arrayParameterInfo.Type.Name.EndsWith("Array"));
            typeDef.Methods.Add(new ConstructorDefinition(typeDef, MethodModifiers.Public,
                new[]
                {
                    new ParameterDefinition(new TypeReference("engenious.Graphics", "EffectPass"), "pass",
                        "The parent effect pass."),
                    new ParameterDefinition(TypeSystem.Int32, "offset", "The data offset into the buffer.")
                },
                new MethodBodyDefinition(
                    new BlockExpressionDefinition(
                        new MultilineExpressionDefinition(new CodeExpressionDefinition[]
                                                          { "Pass = pass;", "Offset = offset;", isPrimitive ? "" : "_valueAccessor = new(pass, 0);" }))),
                $"/// <summary>Initializes a new instance of the <see cref=\"{typeDef.Name}\"/> class.</summary>"));

            var builder = new ExpressionBuilder();


            var propTypeRef = new TypeReference(parameterInfo.Type.Namespace,
                parameterInfo.Type.Name.TrimStart('\''));
            if (isPrimitive)
            {
                builder.Append($"if (index < 0 || index >= {arrayParameterInfo.Length})");
                builder.Append(new SimpleExpressionDefinition("throw new System.ArgumentOutOfRangeException(nameof(index));", 1));
                builder.Append("engenious.Graphics.EffectPassParameter.SetValue(Pass, Offset + index, value);");
            }
            else
            {
                var fld = new FieldDefinition(GenericModifiers.Private, propTypeRef, $"_valueAccessor");
                typeDef.Fields.Add(fld);
                // cacheParametersWriter.Append("");
                builder.Append($"{fld.Name}.Offset =  Offset + index * {arrayParameterInfo.LayoutSize / arrayParameterInfo.Length};");
                builder.Append($"return _valueAccessor;");
            }
            
            typeDef.Properties.Add(new PropertyDefinition(MethodModifiers.Public,
                new TypeReference("engenious.Graphics", "EffectPass"), "Pass", new SimplePropertyGetter(), null,
                Comment: "/// <summary>Gets the parent effect pass.</summary>"));

            typeDef.Properties.Add(new PropertyDefinition(MethodModifiers.Public, TypeSystem.Int32, "Offset",
                new SimplePropertyGetter(), new SimplePropertySetter(),
                Comment: "/// <summary>Gets or sets the offset into the buffer.</summary>"));

            var m = new ImplementedPropertyMethodDefinition(
                new MethodBodyDefinition(builder.ToExpression()), isPrimitive);
            
            typeDef.Properties.Add(new PropertyDefinition(MethodModifiers.Public, propTypeRef, "this",
                isPrimitive ? null : m, isPrimitive ? m : null,
                IndexerType: TypeSystem.Int32, IndexerName: "index",
                Comment: $"/// <summary>{(isPrimitive ? "Sets" : "Gets")} the value at the given <paramref name=\"index\" />.</summary>\n" +
                         $"/// <param name=\"index\">The index to {(isPrimitive ? "set" : "get")} the value at.</param>."));
            

            parent.NestedTypes.Add(typeDef);

            return true;
        }

        private bool TryGenerateStruct(TypeDefinition parent, ParameterInfo parameterInfo, [MaybeNullWhen(false)] out StructParameterInfo structParameterInfo)
        {
            structParameterInfo = parameterInfo as StructParameterInfo;
            if (structParameterInfo == null)
                return false;
            var structTypeName = structParameterInfo.Name.TrimStart('\'') + "Wrapper";
            var typeDef = new TypeDefinition(string.Empty, TypeModifiers.Class | TypeModifiers.Public | TypeModifiers.Partial,structTypeName
                , $"/// <summary>Wrapper class for the <c>{structTypeName}</c> struct.</summary>");



            typeDef.Properties.Add(new PropertyDefinition(MethodModifiers.Public,
                new TypeReference("engenious.Graphics", "EffectPass"), "Pass", new SimplePropertyGetter(), null,
                Comment: "/// <summary>Gets the parent effect pass.</summary>"));

            typeDef.Properties.Add(new PropertyDefinition(MethodModifiers.Public, TypeSystem.Int32, "Offset",
                new SimplePropertyGetter(), new SimplePropertySetter(),
                Comment: "/// <summary>Gets or sets the offset into the buffer.</summary>"));

            var ctorBuilder = new ExpressionBuilder();
                
            ctorBuilder.Append("Pass = pass;");
            ctorBuilder.Append("Offset = offset;");
            int index = 0;
            foreach (var p in structParameterInfo.SubParameters)
            {
                if (TryGenerateStruct(typeDef, p, out var subStructParam))
                {
                    var subField = new FieldDefinition(GenericModifiers.Private, subStructParam.Type,
                        $"_{subStructParam.Name}");
                    typeDef.Fields.Add(subField);
                    ctorBuilder.Append($"{subField.Name} = new (pass, {subStructParam.AttributeName};");
                }
                var pName = p.Name.TrimStart('\'');
                var (prop, field) = typeDef.CreateEmptyProperty(MethodModifiers.Public, p.Type, pName, $"_{pName}");

                var builder = new ExpressionBuilder();
                
                builder.Append($"{field.Name} = value;");
                builder.Append($"engenious.Graphics.EffectPassParameter.SetValue(Pass, Offset + {index++}, value);");

                
                prop = prop with
                       {
                            GetMethod = new ImplementedPropertyMethodDefinition(new MethodBodyDefinition($"{field.Name}"), false),
                            SetMethod = new ImplementedPropertyMethodDefinition(new MethodBodyDefinition(builder.ToExpression()), true),
                            Comment = $"/// <summary>Gets or sets the {prop.Name} value.</summary>"
                       };
                typeDef.Properties.Add(prop);
                typeDef.Fields.Add(field);
            }

            typeDef.Methods.Add(new ConstructorDefinition(typeDef, MethodModifiers.Public,
                new[]
                {
                    new ParameterDefinition(new TypeReference("engenious.Graphics", "EffectPass"), "pass",
                        "The parent effect pass."),
                    new ParameterDefinition(TypeSystem.Int32, "offset", "The data offset into the buffer.")
                },
                new MethodBodyDefinition(ctorBuilder.ToExpression()), 
                $"/// <summary>Initializes a new instance of the <see cref=\"{typeDef.Name}\"> class.</summary>"));
            parent.NestedTypes.Add(typeDef);

            return true;
        }

        private static readonly HashSet<Assembly> _assemblies = new();

        private static HashSet<Assembly> GetAssemblies(Assembly? asm = null)
        {
            if (asm == null)
            {
                asm = Assembly.GetCallingAssembly();
            }

            if (_assemblies.Contains(asm))
                return _assemblies;

            _assemblies.Add(asm);
            foreach (var aRef in asm.GetReferencedAssemblies())
            {
                GetAssemblies(Assembly.Load(aRef));
            }

            return _assemblies;
        }

        private static Type? GetType(TypeReference typeRef)
        {
            foreach (var asm in GetAssemblies())
            {
                var tp = asm.GetType(typeRef.ToString());
                if (tp != null)
                    return tp;
            }

            return null;
        }

        private TypeDefinition? GenerateEffectPassSource(TypeDefinition parent, EffectPass pass,
            CreatedContentCode.CreatedTypeContainer createdTypeContainer)
        {
            var baseType = new TypeReference("engenious.Graphics", "EffectPass");
            var effectPassParameterCollectionType =
                new TypeReference("engenious.Graphics", "EffectPassParameterCollection");
            var effectPassParameterType = new TypeReference("engenious.Graphics", "EffectPassParameter");
            var graphicsDeviceType = new TypeReference("engenious.Graphics", "GraphicsDevice");
            var typeDefinition =
                new TypeDefinition(string.Empty, TypeModifiers.Class | TypeModifiers.Public | TypeModifiers.Partial, $"{pass.Name}Impl",
                    new[] { baseType }, $"/// <summary>Implementation of the <see cref=\"{pass.Name}\"/>effect pass.</summary>");

            parent.NestedTypes.Add(typeDefinition);

            var ctor = new ConstructorDefinition(typeDefinition,
                MethodModifiers.Public,
                new[]
                {
                    new ParameterDefinition(graphicsDeviceType, "graphicsDevice"),
                    new ParameterDefinition(TypeSystem.String, "name")
                },
                new MethodBodyDefinition(new BlockExpressionDefinition(new MultilineExpressionDefinition())),
                $"/// <summary>Initializes a new instance of the <see cref=\"{typeDefinition.Name}\"/> class.</summary>", 
                new CodeExpressionDefinition[] { new SimpleExpressionDefinition("base(graphicsDevice, name)") });

            typeDefinition.Methods.Add(ctor);

            var cacheParametersMethod = new ImplementedMethodDefinition(
                new SignatureDefinition(MethodModifiers.Override | MethodModifiers.Protected, TypeSystem.Void,
                    "CacheParameters", Array.Empty<ParameterDefinition>()),
                null, "/// <inheritdoc />");

            var cacheParametersWriter = new ExpressionBuilder();
            cacheParametersWriter.Append("base.CacheParameters();");

            cacheParametersWriter.Append("var parameters = Parameters;");

            foreach (var p in pass.Parameters)
            {
                if (TryGenerateStruct(typeDefinition, p, out var structParameterInfo))
                {
                    if (structParameterInfo.Name.StartsWith('\''))
                        continue;
                    var structType = new TypeReference(null, $"{structParameterInfo.Name}Wrapper");
                    var f = new FieldDefinition(GenericModifiers.Private, structType, $"_{p.Name}");
                    
                    var prop = new PropertyDefinition(MethodModifiers.Public, structType, p.Name,
                        new ImplementedPropertyMethodDefinition(new MethodBodyDefinition(f.Name), false),
                        null, Comment: $"/// <summary>Gets the {p.Name} struct value.</summary>");
                    cacheParametersWriter.Append($"{f.Name} = new (this,parameters[\"{structParameterInfo.AttributeName}\"].Location);");
                    
                    typeDefinition.Fields.Add(f);
                    typeDefinition.Properties.Add(prop);
                    continue;
                }
                if (TryGenerateArray(typeDefinition, p, cacheParametersWriter, out var arrayParameterInfo))
                {
                    var arrType = new TypeReference(null, $"{arrayParameterInfo.Name}Array");
                    var f = new FieldDefinition(GenericModifiers.Private, arrType, $"_{p.Name}");
                    var prop = new PropertyDefinition(MethodModifiers.Public, arrType, p.Name,
                        new ImplementedPropertyMethodDefinition(new MethodBodyDefinition(f.Name), false),
                        null, Comment: $"/// <summary>Gets the {p.Name} array values.</summary>");
                    cacheParametersWriter.Append($"{f.Name} = new (this, parameters[\"{arrayParameterInfo.AttributeName}\"].Location);");

                    typeDefinition.Fields.Add(f);
                    typeDefinition.Properties.Add(prop);
                    continue;
                }
                var paramType = new TypeReference(p.Type.Namespace, p.Type.Name);
                if (p.Type.Namespace == typeof(EffectPassParameter).Namespace && p.Type.Name == nameof(EffectPassParameter))
                {
                    var paramProp = typeDefinition.AddAutoProperty(MethodModifiers.Public, paramType, p.Name,
                        setterModifiers: MethodModifiers.Private, comment: $"/// <summary>Gets the {p.Name} parameter value.</summary>");

                    cacheParametersWriter.Append($"{paramProp.Name} = parameters[\"{p.Name}\"];");
                }
                else
                {
                    var effectPassParamField = new FieldDefinition(GenericModifiers.Private, effectPassParameterType,
                        $"_{p.Name}PassParameter");
                    typeDefinition.Fields.Add(effectPassParamField);
                    cacheParametersWriter.Append($"{effectPassParamField.Name} = parameters[\"{p.Name}\"];");


                    var (paramProp, paramField) =
                        typeDefinition.CreateEmptyProperty(MethodModifiers.Public, paramType, p.Name, $"_{p.Name}");

                    paramProp = paramProp with { Comment = $"/// <summary>Gets the {p.Name} parameter value.</summary>" };
                    
                    typeDefinition.Fields.Add(paramField);
                    var getter = new MethodBodyDefinition($"{paramField.Name}");

                    var setWriter = new ExpressionBuilder();

                    static MethodInfo? FindMatchingMethod(Type resolvedParamType,
                        Func<MethodInfo, bool> methodConstraint)
                    {
                        var res = resolvedParamType.GetMethods().FirstOrDefault(methodConstraint);
                        if (res != null)
                            return res;
                        var curParamType = resolvedParamType;
                        while (res == null)
                        {
                            curParamType = curParamType.BaseType;
                            if (curParamType == null)
                                return null;
                            res = curParamType.GetMethods().FirstOrDefault(methodConstraint);
                        }

                        return res;
                    }
                    
                    var resolvedType = GetType(p.Type);
                    if (resolvedType == null)
                        continue;

                    bool isSimple = resolvedType.IsPrimitive || FindMatchingMethod(resolvedType,
                        x =>
                        {
                            var parameters = x.GetParameters();
                            return x.Name == "op_Equality" && x.IsStatic && parameters.Length == 2 &&
                                   parameters[0].ParameterType == parameters[1].ParameterType &&
                                   parameters[0].ParameterType.IsAssignableFrom(resolvedType);
                        }) != null;

                    if (!isSimple)
                        isSimple = FindMatchingMethod(resolvedType,
                            x =>
                            {
                                var parameters = x.GetParameters();
                                return x.Name == "Equals" && !x.IsStatic && parameters.Length == 1
                                       && parameters[0].ParameterType.IsAssignableFrom(resolvedType);
                            }) == null;

                    setWriter.Append(
                        $"if ({paramField.Name} == value{(isSimple ? string.Empty : $" || (value != null && value.Equals({paramField.Name}))")})");

                    setWriter.Append(new SimpleExpressionDefinition("return;", 1));

                    setWriter.Append($"{paramField.Name} = value;");
                    setWriter.Append($"{effectPassParamField.Name}.SetValue(value);");

                    paramProp = paramProp with
                                {
                                    GetMethod = new ImplementedPropertyMethodDefinition(getter, false),
                                    SetMethod = new ImplementedPropertyMethodDefinition(
                                        new MethodBodyDefinition(setWriter.ToExpression()), true),
                                    Comment = $"/// <summary>Gets or sets the {p.Name} parameter.</summary>"
                                };

                    typeDefinition.Properties.Add(paramProp);
                }
            }

            cacheParametersMethod = cacheParametersMethod with
                                    {
                                        MethodBody = new MethodBodyDefinition(cacheParametersWriter.ToExpression())
                                    };
            typeDefinition.Methods.Add(cacheParametersMethod);

            GenerateMaterials(typeDefinition, pass, createdTypeContainer);

            return typeDefinition;
        }
        private void GenerateMaterials(TypeDefinition effectPassType, EffectPass pass,
            CreatedContentCode.CreatedTypeContainer createdTypeContainer)
        {
            // Preprocess: Find matching effect parameters
            foreach (var m in pass.Materials)
            {
                foreach (var (parameterName, (propName, _)) in m.Bindings)
                {
                    var param = pass.Parameters.FirstOrDefault(x => x.Name == parameterName)
                                ?? throw new Exception(
                                    $"No matching parameter for material binding of material {m.Name}");

                    m.Bindings[parameterName] = (propName, param);
                }
            }
            foreach (var m in pass.Materials)
            {
                // TODO: find same materials
                // int materialType = 0; // 1 UBO, 2 uniform 3 -> mixed
                //
                // int paramHash = 0;
                // foreach (var (parameterName, propName) in m.Bindings)
                // {
                //     var param = pass.Parameters.FirstOrDefault(x => x.Name == parameterName)
                //                 ?? throw new Exception($"No matching parameter for material binding of material {m.Name}");
                //     if (param.Type.FullName == null)
                //         continue;
                //     int paramType = 2;// TODO UBO -> 1
                //     if (materialType == 0)
                //     {
                //         materialType = paramType;
                //     }
                //     else if (materialType != paramType)
                //     {
                //         materialType = 3;
                //         //throw new NotSupportedException("Mixed material using uniforms and buffers not supported");
                //     }
                //
                //     paramHash = (paramHash * paramType) ^ param.Type.FullName.GetHashCode();
                // }

                CreateMaterial(effectPassType, pass, m, createdTypeContainer);
            }
        }
        private void CreateMaterial(TypeDefinition effectPassType, EffectPass pass, EffectMaterial material,
            CreatedContentCode.CreatedTypeContainer createdTypeContainer)
        {

                
            var typeDef = new TypeDefinition("engenious.Graphics.UserDefined.Materials",
                TypeModifiers.Class | TypeModifiers.Sealed | TypeModifiers.Public, material.Name, new TypeReference[1],
                $"/// <summary>Implementation for {material.Name} material.</summary>");

            createdTypeContainer.FileDefinition.Types.Remove(typeDef.FullName);
            
            createdTypeContainer.FileDefinition.Types.Add(typeDef.FullName, typeDef);
            var baseType = new TypeReference("engenious.Graphics", $"Material<{typeDef.FullName}>");
            typeDef.BaseTypes![0] = baseType;
            var dirtyField = new FieldDefinition(GenericModifiers.Private, TypeSystem.Boolean, "_dirty");
            typeDef.Fields.Add(dirtyField);


            {
                const string paramName = "materialName";
                var ctor = new ConstructorDefinition(typeDef, MethodModifiers.Public,
                    new[] { new ParameterDefinition(TypeSystem.String, paramName, $"The name of the material.") }, MethodBodyDefinition.EmptyBody,
                    $"/// <summary>Initializes a new instance of the <see cref=\"{typeDef.Name}\"/> class.</summary>",new CodeExpressionDefinition[] { $"base({paramName})" });
                
                typeDef.Methods.Add(ctor);
            }
            
            foreach (var (parameterName, (propName, paramInfo)) in material.Bindings)
            {
                if (paramInfo == null)
                    continue;
                var fieldProp = new FieldDefinition(GenericModifiers.Private, paramInfo.Type, $"_{propName}");

                var setter = new ImplementedPropertyMethodDefinition(
                    new MethodBodyDefinition(new BlockExpressionDefinition(
                        new MultilineExpressionDefinition(new CodeExpressionDefinition[]
                                                          { $"{dirtyField.Name} = true;", $"{fieldProp.Name} = value;" }))), true);

                
                var matProp = new PropertyDefinition(MethodModifiers.Public, paramInfo.Type, propName, 
                    new ImplementedPropertyMethodDefinition(new MethodBodyDefinition($"{fieldProp.Name}"), false),
                    setter, Comment: $"/// <summary>Gets or sets the {propName} property.</summary>");

                typeDef.Fields.Add(fieldProp);
                typeDef.Properties.Add(matProp);
            }

            var updateMethod = new ImplementedMethodDefinition(new SignatureDefinition(
                    MethodModifiers.Public | MethodModifiers.Override, TypeSystem.Void, "Update",
                    Array.Empty<ParameterDefinition>()),
                new MethodBodyDefinition(
                    new BlockExpressionDefinition(new MultilineExpressionDefinition(new CodeExpressionDefinition[]
                        { $"if (!{dirtyField.Name})", new SimpleExpressionDefinition("return;", 1),"base.Update();" }))),
                "/// <inheritdoc />");
            typeDef.Methods.Add(updateMethod);

            CreateMaterialRef(effectPassType, pass, material, typeDef);
        }
        
        private void CreateMaterialRef(TypeDefinition effectPassType, EffectPass pass, EffectMaterial material, TypeDefinition materialTypeDef)
        {
            var matRef = new TypeReference("engenious.Graphics", $"MaterialRef<{materialTypeDef.FullName}>");

            var actionMatRefGen = new TypeReference("System", $"Action<{matRef}>");
            var updateActionField = new FieldDefinition(GenericModifiers.Private, actionMatRefGen, "_updateMaterialAction");

            effectPassType.Fields.Add(updateActionField);
            var lines = new List<CodeExpressionDefinition>();
            lines.Add("var mat = matRef.Material;");
            
            foreach (var (uniformName, (propName, paramInfo)) in material.Bindings)
            {
                if (paramInfo == null)
                    continue;
                var prop = effectPassType.Properties.First(
                    x => x.Name == paramInfo.Name && x.Type == paramInfo.Type);
                
                lines.Add($"{prop.Name} = mat.{propName};");
            }
            var updateMethod = new ImplementedMethodDefinition(new SignatureDefinition(MethodModifiers.Private,
                    TypeSystem.Void, "UpdateMaterial", new[] { new ParameterDefinition(matRef, "matRef") }),
                new MethodBodyDefinition(new BlockExpressionDefinition(new MultilineExpressionDefinition(lines))));
            var effectPassCtor = effectPassType.Methods.OfType<ConstructorDefinition>().First();
            if (effectPassCtor.MethodBody.Body is BlockExpressionDefinition { ScopeContent: MultilineExpressionDefinition mle })
            {
                mle.Lines.Add($"{updateActionField.Name} = {updateMethod.Signature.Name};");
            }
            
            effectPassType.Methods.Add(updateMethod);

            var fieldProp = new FieldDefinition(GenericModifiers.Private, matRef, $"_{material.Name}");
            var matProp = new PropertyDefinition(MethodModifiers.Public, matRef, material.Name,
                new ImplementedPropertyMethodDefinition(new MethodBodyDefinition($"{fieldProp.Name}"), false)
                , new ImplementedPropertyMethodDefinition(new MethodBodyDefinition(new BlockExpressionDefinition(
                    new MultilineExpressionDefinition(
                        new CodeExpressionDefinition[]
                        {
                            $"var mat = {fieldProp.Name};",
                            "mat?.Dispose();",
                            $"{fieldProp.Name} = value;",
                            $"value.Update = {updateActionField.Name};"
                        }))), true), Comment: "/// <summary>Gets or sets a reference to the material.</summary>");
            
            effectPassType.Fields.Add(fieldProp);
            effectPassType.Properties.Add(matProp);
        }
        private static Type GetType(EffectParameterType type)
        {
            Type t;
            switch (type)
            {
                case EffectParameterType.Bool:
                    t = typeof(bool);
                    break;
                case EffectParameterType.Double:
                    t = typeof(double);
                    break;
                case EffectParameterType.Float:
                    t = typeof(float);
                    break;
                case EffectParameterType.FloatMat4:
                    t = typeof(Matrix);
                    break;
                case EffectParameterType.FloatVec2:
                    t = typeof(Vector2);
                    break;
                case EffectParameterType.FloatVec3:
                    t = typeof(Vector3);
                    break;
                case EffectParameterType.FloatVec4:
                    t = typeof(Vector4);
                    break;
                case EffectParameterType.Sampler2D:
                case EffectParameterType.Sampler2DShadow:
                    t = typeof(Texture2D);
                    break;
                case EffectParameterType.Sampler2DArray:
                case EffectParameterType.Sampler2DArrayShadow:
                    t = typeof(Texture2DArray);
                    break;
                case EffectParameterType.Int:
                    t = typeof(int);
                    break;
                case EffectParameterType.IntVec2:
                    t = typeof(Point);
                    break;
                case EffectParameterType.UnsignedInt:
                    t = typeof(uint);
                    break;
                default:
                    t = typeof(EffectPassParameter);
                    break;
            }

            return t;
        }

        /// <inheritdoc />
        public override EffectContent? Process(EffectContent input, string filename, ContentProcessorContext context)
        {
            var game = (IGame)context.Game;
            
            try
            {
                var success = true;
                input.CreateUserEffect = _settings.CreateUserEffect;
                //Passthrough and verification
                foreach (var technique in input.Techniques)
                {
                    foreach (var pass in technique.Passes)
                    {
                        Graphics.EffectPass compiledPass = new Graphics.EffectPass(game.GraphicsDevice, pass.Name);

                        foreach (var shader in pass.Shaders)
                        {
                            try
                            {
                                var tmp = new Shader(game.GraphicsDevice, shader.Key,
                                    File.ReadAllText(shader.Value));
                                tmp.Compile();
                                compiledPass.AttachShader(tmp);
                            }
                            catch (Exception ex)
                            {
                                success = false;
                                PreprocessMessage(context, shader.Value, ex.Message,
                                    BuildMessageEventArgs.BuildMessageType.Error);
                            }
                        }

                        compiledPass.Link();
                        compiledPass.Apply();
                        foreach (var attr in pass.Attributes)
                        {
                            compiledPass.BindAttribute(attr.Key, attr.Value);
                        }

                        compiledPass.CacheParameters();

                        Dictionary<string, ParameterInfo> subs = new();

                        foreach (var p in compiledPass.Parameters)
                        {
                            var structName = p.Name;
                            var dotInd = structName.LastIndexOf('.');
                            if (dotInd == -1)
                            {
                                if (CreateArray(ref structName, subs, p, pass))
                                    continue;
                            }
                            else
                            {
                                var subName = structName[..dotInd];

                                var prevIndex = structName.LastIndexOf('.', dotInd - 1);
                                if (prevIndex == -1)
                                {
                                    prevIndex = 0;
                                }

                                structName = p.Name[prevIndex..dotInd];

                                _ = CreateArray(ref structName, subs, p, pass);

                                {
                                    if (!subs.TryGetValue(structName, out var param))
                                    {
                                        param = new StructParameterInfo(p.Name, structName, new TypeReference(null, structName), 0);
                                        subs.Add(structName, param);
                                        pass.Parameters.Add(param);
                                    }

                                    ((StructParameterInfo)param).SubParameters.Add(
                                        new ParameterInfo(p.Name[(dotInd + 1)..], GetType(p.Type)));
                                }
                                continue;
                            }

                            pass.Parameters.Add(new ParameterInfo(p.Name, GetType(p.Type)));
                        }

                        int calcLayoutSize(TypeReference typeRef)
                        {
                            if (!subs.TryGetValue(typeRef.ToString(), out var parameterInfo)) 
                                return 1;
                            switch (parameterInfo)
                            {
                                case StructParameterInfo structParameterInfo:
                                {
                                    if (structParameterInfo.LayoutSize != 0)
                                        return structParameterInfo.LayoutSize;
                                    foreach (var s in structParameterInfo.SubParameters)
                                        structParameterInfo.LayoutSize += calcLayoutSize(s.Type);
                                    return structParameterInfo.LayoutSize;
                                }
                                case ArrayParameterInfo arrayParameterInfo:
                                    arrayParameterInfo.LayoutSize =
                                        calcLayoutSize(parameterInfo.Type) * arrayParameterInfo.Length;
                                    return arrayParameterInfo.LayoutSize;
                            }

                            return 1;
                        }

                        foreach (var (n, p) in subs)
                        {
                            switch (p)
                            {
                                case StructParameterInfo structParameterInfo:
                                    structParameterInfo.LayoutSize = calcLayoutSize(p.Type);
                                    break;
                                case ArrayParameterInfo arrayParameterInfo:
                                    arrayParameterInfo.LayoutSize = calcLayoutSize(p.Type) * arrayParameterInfo.Length;
                                    break;
                            }
                        }
                    }
                }

                if (!success)
                    return null;

                if (input.CreateUserEffect)
                {
                    string rel = context.GetRelativePathToWorkingDirectory(filename);
                    var namespce = Path.GetDirectoryName(rel);
                    namespce = namespce?.Replace(Path.DirectorySeparatorChar, '.')
                        .Replace(Path.AltDirectorySeparatorChar, '.') ?? string.Empty;
                    var nameWithoutExtension = Path.GetFileNameWithoutExtension(rel);
                    GenerateEffectSource(filename, input, namespce, nameWithoutExtension, context);
                }

                return input;
            }
            catch (FileNotFoundException ex)
            {
                PreprocessMessage(context, Path.GetFileName(filename), ex.Message,
                    BuildMessageEventArgs.BuildMessageType.Error);
            }

            return null;
        }

        private static bool CreateArray(ref string structName, Dictionary<string, ParameterInfo> subs, EffectPassParameter p, EffectPass pass)
        {
            if (!structName.EndsWith(']'))
                return false;

            var beg = structName.LastIndexOf('[');
            int arrayIndex = int.Parse(structName[(beg + 1)..^1]);
            var arrayName = structName[..(beg)];
            structName =
                arrayName.EndsWith("s") ? $"'{arrayName[..^1]}" : "'unnamedStructTODO"; // TODO:

            if (!subs.TryGetValue(arrayName, out var param))
            {
                var typeRef = p.Name.EndsWith("]")
                    ? GetType(p.Type).ToTypeReference()
                    : new TypeReference(null, $"{structName}Wrapper");
                param = new ArrayParameterInfo(p.Name, arrayName, typeRef, 0);
                subs.Add(arrayName, param);
                pass.Parameters.Add(param);
            }

            ((ArrayParameterInfo)param).Length = arrayIndex + 1;
            return true;

        }
    }

    /// <summary>
    ///     Settings class to influence <see cref="EffectProcessor"/>.
    /// </summary>
    [Serializable]
    public class EffectProcessorSettings : ProcessorSettings
    {
        /// <summary>
        ///     Gets or sets a value indicating whether to create source code for the effect.
        /// </summary>
        [DefaultValue(true)] public bool CreateUserEffect { get; set; } = true;
    }
}