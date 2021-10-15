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
            var typeDefinition = new TypeDefinition(typeNamespace, TypeModifiers.Class | TypeModifiers.Public, name,
                new[] { baseType });

            createdTypeContainer.FileDefinition.Types.Add(typeDefinition.FullName, typeDefinition);
            var ctor = new ConstructorDefinition(typeDefinition, MethodModifiers.Public,
                new[] { new ParameterDefinition(graphicsDeviceType, "graphicsDevice") },
                MethodBodyDefinition.EmptyBody,
                new CodeExpressionDefinition[] { new SimpleExpressionDefinition("base(graphicsDevice)") });

            typeDefinition.Methods.Add(ctor);

            var initializeMethod = new ImplementedMethodDefinition(
                new SignatureDefinition(MethodModifiers.Protected | MethodModifiers.Override, TypeSystem.Void,
                    "Initialize",
                    Array.Empty<ParameterDefinition>()), null);

            using var expressionBuilder = new ExpressionBuilder();

            expressionBuilder.Append("base.Initialize();");
            expressionBuilder.Append("var techniques = Techniques;");
            foreach (var technique in input.Techniques)
            {
                var techniqueType = GenerateEffectTechniqueSource(typeDefinition, technique);
                var p = typeDefinition.AddAutoProperty(MethodModifiers.Public, techniqueType, technique.Name,
                    setterModifiers: MethodModifiers.Private);

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

        private TypeDefinition GenerateEffectTechniqueSource(TypeDefinition parent, EffectTechnique technique)
        {
            technique.UserTechniqueName = $"{technique.Name}Impl";

            var baseType = new TypeReference("engenious.Graphics", "EffectTechnique");
            var effectPassCollectionType = new TypeReference("engenious.Graphics", "EffectPassCollection");
            var graphicsDeviceType = new TypeReference("engenious.Graphics", "GraphicsDevice");
            var typeDefinition = new TypeDefinition(string.Empty, TypeModifiers.Class | TypeModifiers.Public,
                $"{technique.Name}Impl",
                new[] { baseType });

            parent.NestedTypes.Add(typeDefinition);
            var ctor = new ConstructorDefinition
            (
                typeDefinition, MethodModifiers.Public,
                new[] { new ParameterDefinition(TypeSystem.String, "name") }, MethodBodyDefinition.EmptyBody,
                new CodeExpressionDefinition[] { new SimpleExpressionDefinition("base(name)") }
            );
            typeDefinition.Methods.Add(ctor);

            var initializeMethod = new ImplementedMethodDefinition(
                new SignatureDefinition(MethodModifiers.Override | MethodModifiers.Protected, TypeSystem.Void,
                    "Initialize", Array.Empty<ParameterDefinition>()), null);

            using var initializeMethodBuilder = new ExpressionBuilder();
            initializeMethodBuilder.Append("base.Initialize();");
            initializeMethodBuilder.Append("var passes = Passes;");

            var passTypeDefinitions = new Dictionary<string, (TypeDefinition, PropertyDefinition)>();

            foreach (var pass in technique.Passes)
            {
                var passType = GenerateEffectPassSource(typeDefinition, pass);
                if (passType == null)
                {
                    throw new Exception($"Unable to create IL code for {pass.Name} EffectPass");
                }

                var p = typeDefinition.AddAutoProperty(MethodModifiers.Public, passType, pass.Name,
                    setterModifiers: MethodModifiers.Private);

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

                bool isPrimitive = !string.IsNullOrEmpty(propertyType.Namespace);

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

                var setterWriter = new ExpressionBuilder();
                foreach (var parameterReference in parameterReferences)
                {
                    var (passType, passProperty) = passTypeDefinitions[parameterReference.Pass.Name];

                    if (isPrimitive)
                        setterWriter.Append($"{passProperty.Name}.{name} = value");
                    else
                        setterWriter.Append($"{passProperty.Name}.{name}");
                }

                var m = new ImplementedPropertyMethodDefinition(new MethodBodyDefinition(setterWriter.ToExpression()),
                    isPrimitive);

                var prop = new PropertyDefinition(MethodModifiers.Public, propertyType, name, isPrimitive ? null : m, isPrimitive ? m : null);

                typeDefinition.Properties.Add(prop);
            }

            return typeDefinition;
        }

        private bool TryGenerateArray(TypeDefinition parent, ParameterInfo parameterInfo, ExpressionBuilder cacheParametersWriter, [MaybeNullWhen(false)] out ArrayParameterInfo arrayParameterInfo)
        {
            arrayParameterInfo = parameterInfo as ArrayParameterInfo;
            if (arrayParameterInfo == null)
                return false;

            var typeDef = new TypeDefinition(string.Empty, TypeModifiers.Class | TypeModifiers.Public,
                $"{arrayParameterInfo.Name}Array", null);
            typeDef.Methods.Add(new ConstructorDefinition(typeDef, MethodModifiers.Public,
                new[]
                {
                    new ParameterDefinition(new TypeReference("engenious.Graphics", "EffectPass"), "pass"),
                    new ParameterDefinition(TypeSystem.Int32, "offset")
                },
                new MethodBodyDefinition(
                    new BlockExpressionDefinition(
                        new MultilineExpressionDefinition(new CodeExpressionDefinition[]
                                                          { "Pass = pass;", "Offset = offset;", "_valueAccessor = new(pass, 0);" })))));

            var builder = new ExpressionBuilder();

            bool isPrimitive = !string.IsNullOrEmpty(arrayParameterInfo.Type.Namespace);

            var propTypeRef = new TypeReference(parameterInfo.Type.Namespace,
                parameterInfo.Type.Name.TrimStart('\'') + "Wrapper");
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
                new TypeReference("engenious.Graphics", "EffectPass"), "Pass", new SimplePropertyGetter(), null));

            typeDef.Properties.Add(new PropertyDefinition(MethodModifiers.Public, TypeSystem.Int32, "Offset",
                new SimplePropertyGetter(), new SimplePropertySetter()));

            var m = new ImplementedPropertyMethodDefinition(
                new MethodBodyDefinition(builder.ToExpression()), isPrimitive);
            
            typeDef.Properties.Add(new PropertyDefinition(MethodModifiers.Public, propTypeRef, "this",
                isPrimitive ? null : m, isPrimitive ? m : null,
                IndexerType: TypeSystem.Int32, IndexerName: "index"));
            

            parent.NestedTypes.Add(typeDef);

            return true;
        }

        private bool TryGenerateStruct(TypeDefinition parent, ParameterInfo parameterInfo, [MaybeNullWhen(false)] out StructParameterInfo structParameterInfo)
        {
            structParameterInfo = parameterInfo as StructParameterInfo;
            if (structParameterInfo == null)
                return false;
            var typeDef = new TypeDefinition(string.Empty, TypeModifiers.Class | TypeModifiers.Public,
                structParameterInfo.Name.TrimStart('\'') + "Wrapper", null);



            typeDef.Properties.Add(new PropertyDefinition(MethodModifiers.Public,
                new TypeReference("engenious.Graphics", "EffectPass"), "Pass", new SimplePropertyGetter(), null));

            typeDef.Properties.Add(new PropertyDefinition(MethodModifiers.Public, TypeSystem.Int32, "Offset",
                new SimplePropertyGetter(), new SimplePropertySetter()));

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
                            SetMethod = new ImplementedPropertyMethodDefinition(new MethodBodyDefinition(builder.ToExpression()), true)
                       };
                typeDef.Properties.Add(prop);
                typeDef.Fields.Add(field);
            }

            typeDef.Methods.Add(new ConstructorDefinition(typeDef, MethodModifiers.Public,
                new[]
                {
                    new ParameterDefinition(new TypeReference("engenious.Graphics", "EffectPass"), "pass"),
                    new ParameterDefinition(TypeSystem.Int32, "offset")
                },
                new MethodBodyDefinition(ctorBuilder.ToExpression())));
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

        private TypeDefinition? GenerateEffectPassSource(TypeDefinition parent, EffectPass pass)
        {
            var baseType = new TypeReference("engenious.Graphics", "EffectPass");
            var effectPassParameterCollectionType =
                new TypeReference("engenious.Graphics", "EffectPassParameterCollection");
            var effectPassParameterType = new TypeReference("engenious.Graphics", "EffectPassParameter");
            var graphicsDeviceType = new TypeReference("engenious.Graphics", "GraphicsDevice");
            var typeDefinition =
                new TypeDefinition(string.Empty, TypeModifiers.Class | TypeModifiers.Public, $"{pass.Name}Impl",
                    new[] { baseType });

            parent.NestedTypes.Add(typeDefinition);

            var ctor = new ConstructorDefinition(typeDefinition,
                MethodModifiers.Public,
                new[]
                {
                    new ParameterDefinition(graphicsDeviceType, "graphicsDevice"),
                    new ParameterDefinition(TypeSystem.String, "name")
                },
                MethodBodyDefinition.EmptyBody,
                new CodeExpressionDefinition[] { new SimpleExpressionDefinition("base(graphicsDevice, name)") });

            typeDefinition.Methods.Add(ctor);

            var cacheParametersMethod = new ImplementedMethodDefinition(
                new SignatureDefinition(MethodModifiers.Override | MethodModifiers.Protected, TypeSystem.Void,
                    "CacheParameters", Array.Empty<ParameterDefinition>()),
                null);

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
                        new ImplementedPropertyMethodDefinition(new MethodBodyDefinition(f.Name), false), null);
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
                        new ImplementedPropertyMethodDefinition(new MethodBodyDefinition(f.Name), false), null);
                    cacheParametersWriter.Append($"{f.Name} = new (this, parameters[\"{arrayParameterInfo.AttributeName}\"].Location);");

                    typeDefinition.Fields.Add(f);
                    typeDefinition.Properties.Add(prop);
                    continue;
                }
                var paramType = new TypeReference(p.Type.Namespace, p.Type.Name);
                if (p.Type.Namespace == typeof(EffectPassParameter).Namespace && p.Type.Name == nameof(EffectPassParameter))
                {
                    var paramProp = typeDefinition.AddAutoProperty(MethodModifiers.Public, paramType, p.Name,
                        setterModifiers: MethodModifiers.Private);

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
                                        new MethodBodyDefinition(setWriter.ToExpression()), true)
                                };

                    typeDefinition.Properties.Add(paramProp);
                }
            }

            cacheParametersMethod = cacheParametersMethod with
                                    {
                                        MethodBody = new MethodBodyDefinition(cacheParametersWriter.ToExpression())
                                    };
            typeDefinition.Methods.Add(cacheParametersMethod);

            return typeDefinition;
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
                            var dotInd = p.Name.LastIndexOf('.');
                            if (dotInd != -1)
                            {
                                var subName = p.Name[..dotInd];

                                var prevIndex = p.Name.LastIndexOf('.', dotInd - 1);
                                if (prevIndex == -1)
                                {
                                    prevIndex = 0;
                                }

                                var structName = p.Name[prevIndex..dotInd];

                                if (structName.EndsWith(']'))
                                {
                                    var beg = structName.LastIndexOf('[');
                                    int arrayIndex = int.Parse(structName[(beg + 1)..^1]);
                                    var arrayName = structName[..(beg)];
                                    structName =
                                        arrayName.EndsWith("s") ? $"'{arrayName[..^1]}" : "'unnamedStructTODO"; // TODO:

                                    if (!subs.TryGetValue(arrayName, out var param))
                                    {
                                        var typeRef = p.Name.EndsWith("]") ? GetType(p.Type).ToTypeReference() : new TypeReference(null, structName);
                                        param = new ArrayParameterInfo(p.Name, arrayName, typeRef, 0);
                                        subs.Add(arrayName, param);
                                        pass.Parameters.Add(param);
                                    }

                                    ((ArrayParameterInfo)param).Length = arrayIndex + 1;
                                }

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