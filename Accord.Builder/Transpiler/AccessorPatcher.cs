using Accord.Common.Attributes;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;

namespace Accord.Builder.Transpiler;

public static class AccessorPatcher
{ 
    private static CustomAttribute? GetAccessorAttribute(TypeDefinition typeDefinition)
    {
        if (!typeDefinition.HasCustomAttributes)
            return null;
        
        return typeDefinition.CustomAttributes.FirstOrDefault(it => it.Type?.Name == nameof(AccessorAttribute));
    }
    
    public static void PatchFieldAccessor(RuntimeContext context, AssemblyDefinition assemblyDefinition, TypeDefinition typeDefinition)
    {
        foreach (var method in typeDefinition.Methods)
        {
            if (!method.HasCustomAttributes)
                continue;
            
            var fieldAccessorAttribute = method.CustomAttributes.FirstOrDefault(it => it.Type?.Name == nameof(FieldAccessorAttribute));

            if (fieldAccessorAttribute is null)
                continue;

            var fieldName = (Utf8String)fieldAccessorAttribute.Signature.FixedArguments[0].Element;

            var targetType = method.Parameters.FirstOrDefault().ParameterType;
            
            if (!targetType.TryResolve(context, out TypeDefinition definition))
                continue;

            var field = definition.Fields.FirstOrDefault(it => it.Name.Value == fieldName);
            
            if (field is null)
                continue;

            var b = "";

            var instructions = method.CilMethodBody.Instructions;
            
            instructions.Clear();

            instructions.Add(CilOpCodes.Ldarg_0);
            if (!targetType.IsValueType)
                instructions.Add(CilOpCodes.Ldind_Ref);
            instructions.Add(CilOpCodes.Ldflda, assemblyDefinition.ManifestModule.DefaultImporter.ImportField(field));
            instructions.Add(CilOpCodes.Ret);
        }
    }
    
    public static void Patch(RuntimeContext context, string fileSuffix = "_modified_accessor")
    {
        foreach (var assembly in context.GetLoadedAssemblies())
        {
            if (assembly.ManifestModule is null)
                continue;
            
            var shouldWrite = false;
            
            foreach (var patchType in assembly.ManifestModule.GetAllTypes())
            {

                var attribute = GetAccessorAttribute(patchType);
                
                // TODO: Check for others as well before continue
                if (attribute is null)
                    continue;

                shouldWrite = true;
                
                PatchFieldAccessor(context, assembly, patchType);
            }
            
            if (!shouldWrite)
                continue;
            
            var output = Path.Join(Path.GetDirectoryName(assembly.ManifestModule.FilePath),
                Path.GetFileNameWithoutExtension(assembly.ManifestModule.FilePath) + $"{fileSuffix}.dll");
            assembly.Write(output);
        }
    }
    
}