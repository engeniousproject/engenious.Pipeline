using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using engenious.Graphics;
using engenious.Pipeline;
using engenious.Pipeline.Extensions;
using engenious.Pipeline.Helper;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using OpenTK.Graphics.OpenGL;

namespace engenious.Content.Pipeline
{
    [ContentProcessor(DisplayName = "Effect Processor")]
    public class EffectProcessor : ContentProcessor<EffectContent, EffectContent, EffectProcessorSettings>
    {
        private static BuildMessageEventArgs.BuildMessageType GetMessageType(string line)
        {
            var splt = line.Split(new[] {':'}, StringSplitOptions.None);
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
            string[] lines = msg.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
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

        private void GenerateEffectSource(string filename, EffectContent input, string @namespace, string name, ContentProcessorContext context)
        {
            var mainModule = context.CreatedContent.AssemblyDefinition.MainModule;
            var createdTypeContainer = context.CreatedContent.AddOrUpdateTypeContainer(context.GetRelativePathToContentDirectory(filename), context.BuildId);
            AssemblyDefinition engeniousAssembly = AssemblyDefinition.ReadAssembly(typeof(Game).Assembly.Location);
            string typeNamespace = "engenious.UserDefined" +
                                     (string.IsNullOrEmpty(@namespace) ? string.Empty : "." + @namespace);

            input.UserEffectName = $"{typeNamespace}.{name}";
            var baseType = engeniousAssembly.MainModule.GetType("engenious.Graphics.Effect");
            var effectTechniqueCollectionType =
                engeniousAssembly.MainModule.GetType("engenious.Graphics.EffectTechniqueCollection");
            var graphicsDeviceType = mainModule.ImportReference(engeniousAssembly.MainModule.GetType("engenious.Graphics.GraphicsDevice"));
            TypeDefinition typeDefinition =
                new TypeDefinition(typeNamespace, name, TypeAttributes.Public | TypeAttributes.Class, mainModule.ImportReference(baseType));
            
            createdTypeContainer.Types.Add(typeDefinition);
            var ctor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig, mainModule.TypeSystem.Void);
            ctor.Parameters.Add(new ParameterDefinition(graphicsDeviceType));
            var ctorWriter = ctor.Body.GetILProcessor();
            ctorWriter.Emit(OpCodes.Ldarg_0);
            ctorWriter.Emit(OpCodes.Ldarg_1);
            var baseCtor = baseType.Methods.FirstOrDefault(x =>
                x.Name == ".ctor" && x.Parameters.Count == 1 &&
                x.Parameters[0].ParameterType.FullName == graphicsDeviceType.FullName);
            ctorWriter.Emit(OpCodes.Call, mainModule.ImportReference(baseCtor));
            ctorWriter.Emit(OpCodes.Ret);
            
            typeDefinition.Methods.Add(ctor);
            
            var initializeMethod = new MethodDefinition("Initialize", MethodAttributes.Family | MethodAttributes.Virtual, mainModule.TypeSystem.Void);
            typeDefinition.Methods.Add(initializeMethod);
            initializeMethod.Body.Variables.Add(new VariableDefinition(mainModule.ImportReference(effectTechniqueCollectionType)));
            var initializeWriter = initializeMethod.Body.GetILProcessor();
            initializeWriter.Emit(OpCodes.Ldarg_0);
            var baseInitialize = baseType.Methods.First(x => x.Parameters.Count == 0 && x.Name == "Initialize");
            initializeWriter.Emit(OpCodes.Call, mainModule.ImportReference(baseInitialize));
            var techniquesProperty = baseType.Properties.First(x => x.Name == "Techniques");
            
            initializeWriter.Emit(OpCodes.Ldarg_0);
            initializeWriter.Emit(OpCodes.Call, mainModule.ImportReference(techniquesProperty.GetMethod));
            initializeWriter.Emit(OpCodes.Stloc_0);
            var techniquesGetItem = techniquesProperty.PropertyType.Resolve().Methods.First(x =>
            {
                return x.Parameters.Count == 1 && x.Name == "get_Item" && x.Parameters[0].ParameterType.FullName == mainModule.TypeSystem.String.FullName;
            });
            foreach (var technique in input.Techniques)
            {
                var techniqueType = GenerateEffectTechniqueSource(typeDefinition, technique, engeniousAssembly);
                var (p, _) = typeDefinition.AddAutoProperty(techniqueType, technique.Name, MethodAttributes.Public,
                    MethodAttributes.Private);
                
                initializeWriter.Emit(OpCodes.Ldarg_0);
                initializeWriter.Emit(OpCodes.Ldloc_0);
                initializeWriter.Emit(OpCodes.Ldstr, technique.Name);
                initializeWriter.Emit(OpCodes.Callvirt, mainModule.ImportReference(techniquesGetItem));
                
                initializeWriter.Emit(OpCodes.Isinst, techniqueType);
                initializeWriter.Emit(OpCodes.Call, p.SetMethod);
            }
            
            initializeWriter.Emit(OpCodes.Ret);

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

        private TypeDefinition GenerateEffectTechniqueSource(TypeDefinition parent, EffectTechnique technique, AssemblyDefinition engeniousAssembly)
        {
            var mainModule = parent.Module;
            technique.UserTechniqueName = $"{technique.Name}Impl";
            
            var baseType = engeniousAssembly.MainModule.GetType("engenious.Graphics.EffectTechnique");
            var effectPassCollectionType =
                engeniousAssembly.MainModule.GetType("engenious.Graphics.EffectPassCollection");
            var graphicsDeviceType = engeniousAssembly.MainModule.GetType("engenious.Graphics.GraphicsDevice");
            TypeDefinition typeDefinition =
                new TypeDefinition(string.Empty,$"{technique.Name}Impl", TypeAttributes.Class | TypeAttributes.NestedPublic, mainModule.ImportReference(baseType));

            parent.NestedTypes.Add(typeDefinition);
            var ctor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig, mainModule.TypeSystem.Void);
            ctor.Parameters.Add(new ParameterDefinition(mainModule.TypeSystem.String));
            var ctorWriter = ctor.Body.GetILProcessor();
            ctorWriter.Emit(OpCodes.Ldarg_0);
            ctorWriter.Emit(OpCodes.Ldarg_1);
            var baseCtor = baseType.Methods.FirstOrDefault(x =>
                x.Name == ".ctor" && x.Parameters.Count == 1 &&
                x.Parameters[0].ParameterType.FullName == mainModule.TypeSystem.String.FullName);

            ctorWriter.Emit(OpCodes.Call, mainModule.ImportReference(baseCtor));
            ctorWriter.Emit(OpCodes.Ret);
            
            typeDefinition.Methods.Add(ctor);
            
            var initializeMethod = new MethodDefinition("Initialize", MethodAttributes.Family | MethodAttributes.Virtual, mainModule.TypeSystem.Void);
            typeDefinition.Methods.Add(initializeMethod);
            initializeMethod.Body.Variables.Add(new VariableDefinition(mainModule.ImportReference(effectPassCollectionType)));
            var initializeWriter = initializeMethod.Body.GetILProcessor();
            initializeWriter.Emit(OpCodes.Ldarg_0);
            var baseInitialize = baseType.Methods.First(x => x.Parameters.Count == 0 && x.Name == "Initialize");
            initializeWriter.Emit(OpCodes.Call, mainModule.ImportReference(baseInitialize));
            
            var passesProperty = baseType.Properties.First(x => x.Name == "Passes");
            
            initializeWriter.Emit(OpCodes.Ldarg_0);
            initializeWriter.Emit(OpCodes.Call, mainModule.ImportReference(passesProperty.GetMethod));
            initializeWriter.Emit(OpCodes.Stloc_0);
            var passesGetItem = passesProperty.PropertyType.Resolve().Methods.First(x => x.Parameters.Count == 1 && x.Name == "get_Item" && x.Parameters[0].ParameterType.FullName == mainModule.TypeSystem.String.FullName);

            var passTypeDefinitions = new Dictionary<string, (TypeDefinition,PropertyDefinition)>();
            
            foreach (var pass in technique.Passes)
            {
                var passType = GenerateEffectPassSource(typeDefinition, pass, engeniousAssembly);
                if (passType == null)
                {
                    throw new Exception($"Unable to create IL code for {pass.Name} EffectPass");
                }
                var (p, _) = typeDefinition.AddAutoProperty(passType, pass.Name, MethodAttributes.Public,
                    MethodAttributes.Private);
                
                initializeWriter.Emit(OpCodes.Ldarg_0);
                initializeWriter.Emit(OpCodes.Ldloc_0);
                initializeWriter.Emit(OpCodes.Ldstr, pass.Name);
                initializeWriter.Emit(OpCodes.Callvirt, mainModule.ImportReference(passesGetItem));
                
                initializeWriter.Emit(OpCodes.Isinst, passType);
                initializeWriter.Emit(OpCodes.Call, p.SetMethod);
                passTypeDefinitions.Add(pass.Name, (passType, p));
            }
            
            initializeWriter.Emit(OpCodes.Ret);

            var parameters = new Dictionary<string, List<ParameterReference>?>();
            foreach (var pass in technique.Passes)
            {
                foreach (var param in pass.Parameters)
                {
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


            foreach (var (name,parameterReferences) in parameters)
            {
                if (parameterReferences == null || parameterReferences.Count == 0)
                    continue;
                var propertyType = mainModule.ImportReference(parameterReferences[0].ParameterInfo.Type);
                var prop = new PropertyDefinition(name, PropertyAttributes.None, propertyType);
                var setter =
                    new MethodDefinition($"set_{name}", MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.Public,
                        parent.Module.TypeSystem.Void);
                setter.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, propertyType));
                typeDefinition.Methods.Add(setter);
                prop.SetMethod = setter;
                typeDefinition.Properties.Add(prop);

                var setterWriter = setter.Body.GetILProcessor();
                
                foreach (var parameterReference in parameterReferences)
                {
                    var (passType, passProperty) = passTypeDefinitions[parameterReference.Pass.Name];
                    var basePassProperty = passType.Properties.First(x => x.Name == name);
                    
                    setterWriter.Emit(OpCodes.Ldarg_0);
                    setterWriter.Emit(OpCodes.Call, passProperty.GetMethod);
                    setterWriter.Emit(OpCodes.Ldarg_1);
                    setterWriter.Emit(OpCodes.Call, basePassProperty.SetMethod);

                    setterWriter.Emit(OpCodes.Ret);
                }
            }

            return typeDefinition;
        }
        
        private TypeDefinition? GenerateEffectPassSource(TypeDefinition parent, EffectPass pass, AssemblyDefinition engeniousAssembly)
        {
            var mainModule = parent.Module;
            
            var baseType = engeniousAssembly.MainModule.GetType("engenious.Graphics.EffectPass");
            var effectPassParameterCollectionType =
                engeniousAssembly.MainModule.GetType("engenious.Graphics.EffectPassParameterCollection");
            var effectPassParameterType =
                engeniousAssembly.MainModule.GetType("engenious.Graphics.EffectPassParameter");
            var graphicsDeviceType =
                engeniousAssembly.MainModule.GetType("engenious.Graphics.GraphicsDevice");
            TypeDefinition typeDefinition =
                new TypeDefinition(string.Empty,$"{pass.Name}Impl", TypeAttributes.Class | TypeAttributes.NestedPublic, mainModule.ImportReference(baseType));

            parent.NestedTypes.Add(typeDefinition);
            
            var ctor = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.HideBySig, mainModule.TypeSystem.Void);
            ctor.Parameters.Add(new ParameterDefinition(mainModule.ImportReference(graphicsDeviceType)));
            ctor.Parameters.Add(new ParameterDefinition(mainModule.TypeSystem.String));
            var ctorWriter = ctor.Body.GetILProcessor();
            ctorWriter.Emit(OpCodes.Ldarg_0);
            ctorWriter.Emit(OpCodes.Ldarg_1);
            ctorWriter.Emit(OpCodes.Ldarg_2);
            var baseCtor = baseType.Methods.FirstOrDefault(x =>
                x.Name == ".ctor" && x.Parameters.Count == 2 &&
                x.Parameters[0].ParameterType.FullName == typeof(GraphicsDevice).FullName &&
                x.Parameters[1].ParameterType.FullName == mainModule.TypeSystem.String.FullName);
            ctorWriter.Emit(OpCodes.Call, mainModule.ImportReference(baseCtor));
            ctorWriter.Emit(OpCodes.Ret);

            typeDefinition.Methods.Add(ctor);
            
            var cacheParametersMethod = new MethodDefinition("CacheParameters", MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.Family, mainModule.TypeSystem.Void);

            typeDefinition.Methods.Add(cacheParametersMethod);
            
            cacheParametersMethod.Body.Variables.Add(new VariableDefinition(mainModule.ImportReference(effectPassParameterCollectionType)));
            var cacheParametersWriter = cacheParametersMethod.Body.GetILProcessor();
            cacheParametersWriter.Emit(OpCodes.Ldarg_0);
            var baseCacheParameters = baseType.Methods.First(x => x.Parameters.Count == 0 && x.Name == "CacheParameters");
            cacheParametersWriter.Emit(OpCodes.Call, mainModule.ImportReference(baseCacheParameters));
            
            var paramsProperty = baseType.Properties.First(x => x.Name == "Parameters");

            cacheParametersWriter.Emit(OpCodes.Ldarg_0);
            cacheParametersWriter.Emit(OpCodes.Call, mainModule.ImportReference(paramsProperty.GetMethod));
            cacheParametersWriter.Emit(OpCodes.Stloc_0);
            var paramsGetItem = paramsProperty.PropertyType.Resolve().Methods.First(x => x.Parameters.Count == 1 && x.Name == "get_Item" && x.Parameters[0].ParameterType.FullName == mainModule.TypeSystem.String.FullName);
            foreach (var p in pass.Parameters)
            {
                var paramType = mainModule.ImportReference(p.Type.ToCecilTypeRef().Resolve());

                var isStandardType = p.Type == typeof(EffectPassParameter);
                
                if (isStandardType)
                    cacheParametersWriter.Emit(OpCodes.Ldarg_0);
                cacheParametersWriter.Emit(OpCodes.Ldarg_0);
                cacheParametersWriter.Emit(OpCodes.Ldloc_0);
                    
                cacheParametersWriter.Emit(OpCodes.Ldstr, p.Name);
                cacheParametersWriter.Emit(OpCodes.Callvirt, mainModule.ImportReference(paramsGetItem));
                if (isStandardType)
                {
                    var (paramProp, _) = typeDefinition.AddAutoProperty(paramType, p.Name,
                        MethodAttributes.Public, MethodAttributes.Private);
                    
                    cacheParametersWriter.Emit(OpCodes.Call, paramProp.SetMethod);
                }
                else
                {
                    var effectPassParamField = new FieldDefinition($"_{p.Name}PassParameter", FieldAttributes.Private, mainModule.ImportReference(effectPassParameterType));

                    typeDefinition.Fields.Add(effectPassParamField);
                    cacheParametersWriter.Emit(OpCodes.Stfld, effectPassParamField);
                    
                    var (paramProp, paramField) = typeDefinition.AddEmptyProperty(paramType, p.Name,
                        MethodAttributes.Public, MethodAttributes.Public, $"_{p.Name}");
                    
                    var getWriter = paramProp.GetMethod.Body.GetILProcessor();
                    getWriter.Emit(OpCodes.Ldarg_0);
                    getWriter.Emit(OpCodes.Ldfld, paramField);
                    getWriter.Emit(OpCodes.Ret);

                    var setWriter = paramProp.SetMethod.Body.GetILProcessor();

                    var retOp = setWriter.Create(OpCodes.Ret);
                    setWriter.Emit(OpCodes.Ldarg_0);
                    setWriter.Emit(OpCodes.Ldfld, paramField);
                    

                    var insertPos = setWriter.Create(OpCodes.Ldarg_1);
                    setWriter.Append(insertPos);
                    
                    setWriter.Emit(OpCodes.Ret);

                    var branch = setWriter.Create(OpCodes.Ldarg_0);
                    setWriter.Append(branch);
                    setWriter.Emit(OpCodes.Ldarg_1);
                    setWriter.Emit(OpCodes.Stfld, paramField);

                    setWriter.Emit(OpCodes.Ldarg_0);
                    
                    setWriter.Emit(OpCodes.Ldfld, effectPassParamField);
                    setWriter.Emit(OpCodes.Ldarg_1);
                    
                    var resolvedParamType = paramType.Resolve();
                    var setValue = effectPassParameterType.Methods.First(x =>
                    {
                        return  x.Name == "SetValue" &&
                            (x.Parameters[0].ParameterType.FullName == resolvedParamType.FullName ||
                             x.Parameters[0].ParameterType.Resolve().IsAssignableFrom(resolvedParamType));
                    });
                    setWriter.Emit(OpCodes.Callvirt, mainModule.ImportReference(setValue));
                    
                    setWriter.Append(retOp);


                    static MethodDefinition? FindMatchingMethod(TypeDefinition resolvedParamType, Func<MethodDefinition, bool> methodConstrainer)
                    {
                        var res = resolvedParamType.Methods.FirstOrDefault(methodConstrainer);
                        if (res != null)
                            return res;
                        var curParamType = resolvedParamType;
                        while (res == null)
                        {
                            curParamType = curParamType.BaseType?.Resolve();
                            if (curParamType == null)
                                return null;
                            res = curParamType.Methods.FirstOrDefault(methodConstrainer);
                        }

                        return res;
                    }

                    MethodDefinition? opEquality = null;
                    if (!resolvedParamType.IsPrimitive)
                        opEquality = FindMatchingMethod(resolvedParamType,
                                         x => x.Name == "op_Equality" && !x.HasThis && x.Parameters.Count == 2 && x.Parameters[0].ParameterType == x.Parameters[1].ParameterType && x.Parameters[0].ParameterType.Resolve().IsAssignableFrom(resolvedParamType))
                                     ??  FindMatchingMethod(resolvedParamType, x => x.Name == "Equals" && x.HasThis && x.Parameters.Count == 1
                                         && x.Parameters[0].ParameterType.Resolve().IsAssignableFrom(resolvedParamType));

                    if (opEquality != null)
                    {
                        if (opEquality.Name == "Equals")
                        {
                            var valNullCheck = setWriter.Create(OpCodes.Ldarg_1);
                            var earlyExit = setWriter.Create(OpCodes.Ret);
                            var jump = setWriter.Create(OpCodes.Bne_Un, valNullCheck);
                            setWriter.InsertAfter(insertPos, jump);
                            setWriter.InsertAfter(jump, earlyExit);
                            setWriter.InsertAfter(earlyExit, valNullCheck);
                            insertPos = setWriter.Create(OpCodes.Brfalse_S, branch);
                            setWriter.InsertAfter(valNullCheck, insertPos);

                            var ldArg1 = setWriter.Create(OpCodes.Ldarg_1);
                            setWriter.InsertAfter(insertPos, ldArg1);
                            insertPos = Instruction.Create(OpCodes.Ldarg_0);
                            setWriter.InsertAfter(ldArg1, insertPos);

                            var ldFld = setWriter.Create(OpCodes.Ldfld, effectPassParamField);
                            setWriter.InsertAfter(insertPos, ldFld);
                            insertPos = ldFld;
                        }
                        var eqCheck = setWriter.Create(OpCodes.Call, mainModule.ImportReference(opEquality));
                        setWriter.InsertAfter(insertPos, eqCheck);
                    
                        setWriter.InsertAfter(eqCheck, setWriter.Create(OpCodes.Brfalse, branch));
                    }
                    else
                    {
                        setWriter.InsertAfter(insertPos, setWriter.Create(OpCodes.Bne_Un, branch));
                    }
                }

            }
            cacheParametersWriter.Emit(OpCodes.Ret);

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

        public override EffectContent? Process(EffectContent input, string filename, ContentProcessorContext context)
        {
            try
            {
                var success = true;
                input.CreateUserEffect = settings.CreateUserEffect;
                //Passthrough and verification
                foreach (var technique in input.Techniques)
                {
                    foreach (var pass in technique.Passes)
                    {
                        Graphics.EffectPass compiledPass = new Graphics.EffectPass(context.GraphicsDevice, pass.Name);

                        foreach (var shader in pass.Shaders)
                        {
                            try
                            {
                                var tmp = new Shader(context.GraphicsDevice, shader.Key,
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

                        foreach (var p in compiledPass.Parameters)
                        {
                            pass.Parameters.Add(new ParameterInfo(p.Name, GetType(p.Type)));
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

    [Serializable]
    public class EffectProcessorSettings : ProcessorSettings
    {
        [DefaultValue(true)] public bool CreateUserEffect { get; set; } = true;
    }
}