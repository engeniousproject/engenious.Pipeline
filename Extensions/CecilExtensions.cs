using Mono.Cecil;
using Mono.Cecil.Cil;

namespace engenious.Pipeline.Extensions
{
    public static class CecilExtension
    {
        public static (PropertyDefinition, FieldDefinition) AddEmptyProperty(this TypeDefinition parent, TypeReference propertyType, string name,
            MethodAttributes? getterAttr = null, MethodAttributes? setterAttr = null, string? fieldName = null)
        {
            var p = new PropertyDefinition(name, PropertyAttributes.None, propertyType);
            var f = new FieldDefinition(fieldName ?? $"'<{name}>k__BackingField'", FieldAttributes.Private | FieldAttributes.SpecialName, propertyType);

            if (getterAttr != null)
            {
                var getMethod = new MethodDefinition($"get_{name}", MethodAttributes.HideBySig | MethodAttributes.SpecialName | getterAttr.Value, propertyType);

                p.GetMethod = getMethod;
                parent.Methods.Add(getMethod);
            }
            
            if (setterAttr != null)
            {
                var setMethod = new MethodDefinition($"set_{name}", MethodAttributes.HideBySig | MethodAttributes.SpecialName | setterAttr.Value, parent.Module.TypeSystem.Void);
                setMethod.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, propertyType));
                p.SetMethod = setMethod;
                parent.Methods.Add(setMethod);
            }
            parent.Fields.Add(f);
            parent.Properties.Add(p);
            return (p, f);
        }
        public static (PropertyDefinition, FieldDefinition) AddAutoProperty(this TypeDefinition parent, TypeReference propertyType, string name,
            MethodAttributes? getterAttr = null, MethodAttributes? setterAttr = null)
        {
            var (p, f) = parent.AddEmptyProperty(propertyType, name, getterAttr, setterAttr);
            if (getterAttr != null)
            {
                var methodWriter = p.GetMethod.Body.GetILProcessor();
                methodWriter.Emit(OpCodes.Ldarg_0);
                methodWriter.Emit(OpCodes.Ldfld, f);
                methodWriter.Emit(OpCodes.Ret);
            }
            
            if (setterAttr != null)
            {
                var methodWriter = p.SetMethod.Body.GetILProcessor();
                methodWriter.Emit(OpCodes.Ldarg_0);
                methodWriter.Emit(OpCodes.Ldarg_1);
                methodWriter.Emit(OpCodes.Stfld, f);
                methodWriter.Emit(OpCodes.Ret);
            }
            return (p, f);
        }
    }
}