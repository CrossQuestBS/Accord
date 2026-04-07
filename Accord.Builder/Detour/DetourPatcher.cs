using Accord.Builder.Extensions;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Cloning;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace Accord.Builder.Detour;



public class DetourPatcher
{


    public class CloneListener : MemberClonerListener
    {
        public override void OnClonedMethod(MethodDefinition original, MethodDefinition cloned)
        {
            cloned.Name = $"Orig_{original.Name}";
        }
    }

    public static MethodDefinition? FindOriginalMethod(DetourPatchInfo info, TypeDefinition? typeDefinition)
    {
        return typeDefinition?.Methods.FirstOrDefault(it => it.FullName == info.MethodFullName);
    }

    public static void PatchAssembly(DetourPatchInfo[] patcherInfos, ModuleDefinition moduleToPatch,
        ModuleDefinition patcherModule)
    {
        foreach (var patch in patcherInfos)
        {
            var patcherType = GetGeneratedPatcher(patch, patcherModule);

            if (patcherType is null)
                continue;

            var type = moduleToPatch?.GetAllTypes()
                .FirstOrDefault(type => type.FullName == patch.TypeFullName);

            if (type is null)
                continue;

            var originalMethod = FindOriginalMethod(patch, type);

            if (originalMethod is null)
                continue;

            AddPatcher(moduleToPatch, type, patcherType, originalMethod);
        }
    }

    public static void PatchAll(DetourPatchInfo[] patchers, RuntimeContext context, string outputPath,
        bool saveAssembly = true)
    {
        var patcherGroupedByAssemblyPath = patchers.GroupBy(it => it.AssemblyName);

        var assemblies = context.GetLoadedAssemblies();
        var generatedPatchAssembly = assemblies.FirstOrDefault(it => it.Name.ToString() == "Accord.Generated");

        if (generatedPatchAssembly is null)
            throw new Exception("Generated patch is null");

        foreach (var groupPatches in patcherGroupedByAssemblyPath)
        {
            var assembly = assemblies.FirstOrDefault(it => it.Name.ToString() == groupPatches.Key);

            if (assembly is null)
            {
                Console.WriteLine($"Skipping: {groupPatches.Key}");
                continue;
            }

            Console.WriteLine($"Trying to load assembly: {assembly.Name}");

            PatchAssembly(groupPatches.ToArray(), assembly.ManifestModule, generatedPatchAssembly.ManifestModule);

            if (!saveAssembly)
                continue;

            var path = Path.Join(outputPath, groupPatches.Key + ".dll");
            assembly.Write(path);
        }
    }


    public static void AddPatcher(ModuleDefinition moduleDefinition, TypeDefinition typeDefinition,
        TypeDefinition patchedType, MethodDefinition originalMethod)
    {

        MethodDefinition patcherInstance =
            patchedType.Methods.FirstOrDefault(it => it.Name.ToString().Contains("get_Instance"));
        MethodDefinition prefix = patchedType.Methods.FirstOrDefault(it => it.Name.ToString().Contains("Prefix"));
        MethodDefinition postfix = patchedType.Methods.FirstOrDefault(it => it.Name.ToString().Contains("Postfix"));
        ;

        var result = new MemberCloner(moduleDefinition)
            .Include(originalMethod)
            .AddListener(new CloneListener())
            .Clone();
        var clonedMethod = result.GetClonedMember(originalMethod);

        typeDefinition.Methods.Add(clonedMethod);

        var methodCILBody = originalMethod.CilMethodBody;

        if (methodCILBody is null)
            throw new ArgumentException(nameof(originalMethod));

        methodCILBody.Instructions.Clear();
        methodCILBody.ExceptionHandlers.Clear();

        CilLocalVariable? returnValue = null;

        if (originalMethod.Signature.ReturnType != moduleDefinition.CorLibTypeFactory.Void)
        {
            returnValue = new CilLocalVariable(originalMethod.Signature.ReturnType);
            methodCILBody.LocalVariables.Add(returnValue);
        }

        var isInstanceMethod = originalMethod.Signature.HasThis;

        var instanceMethod = moduleDefinition.DefaultImporter.ImportMethod(patcherInstance);

        var instanceValue = CreateInstance(patcherInstance, methodCILBody, instanceMethod);

        methodCILBody.Instructions.Add(CilOpCodes.Ldloc, instanceValue);

        PrepareArguments(originalMethod, methodCILBody, isInstanceMethod, true, returnValue);

        var prefixMethod = moduleDefinition.DefaultImporter.ImportMethod(prefix);

        methodCILBody.Instructions.Add(CilOpCodes.Callvirt, prefixMethod);


        var label = new CilInstructionLabel();

        methodCILBody.Instructions.Add(
            new CilInstruction(CilOpCodes.Brfalse, label)
        );

        if (isInstanceMethod)
            methodCILBody.Instructions.Add(CilOpCodes.Ldarg_0);

        foreach (var parameter in originalMethod.Parameters)
        {
            methodCILBody.Instructions.Add(CilOpCodes.Ldarg, parameter);
        }

        methodCILBody.Instructions.Add(CilOpCodes.Call, clonedMethod);

        if (returnValue != null)
            methodCILBody.Instructions.Add(CilOpCodes.Stloc, returnValue);

        methodCILBody.Instructions.Add(CilOpCodes.Ldloc, instanceValue);

        PrepareArguments(originalMethod, methodCILBody, isInstanceMethod, true, returnValue);

        var postfixMethod = moduleDefinition.DefaultImporter.ImportMethod(postfix);

        methodCILBody.Instructions.Add(CilOpCodes.Callvirt, postfixMethod);


        if (returnValue != null)
        {
            label.Instruction = methodCILBody.Instructions.Add(CilOpCodes.Ldloc, returnValue);
            methodCILBody.Instructions.Add(CilOpCodes.Ret);
            return;
        }

        label.Instruction = methodCILBody.Instructions.Add(CilOpCodes.Ret);
    }

    private static CilLocalVariable CreateInstance(MethodDefinition? patcherInstance, CilMethodBody methodCILBody,
        IMethodDefOrRef instanceMethod)
    {

        CilLocalVariable instanceValue = new CilLocalVariable(patcherInstance.Signature.ReturnType);
        methodCILBody.LocalVariables.Add(instanceValue);
        methodCILBody.Instructions.Add(CilOpCodes.Call, instanceMethod);
        methodCILBody.Instructions.Add(CilOpCodes.Stloc, instanceValue);

        return instanceValue;
    }

    private static void PrepareArguments(MethodDefinition originalMethod, CilMethodBody methodCILBody,
        bool isInstanceMethod, bool withReturnValueArg = false, CilLocalVariable? returnValue = null)
    {
        if (isInstanceMethod)
            methodCILBody.Instructions.Add(CilOpCodes.Ldarg_0);

        foreach (var parameter in originalMethod.Parameters)
        {
            var isRefType = parameter.ParameterType.ElementType == ElementType.ByRef || parameter.Definition.IsIn ||
                            parameter.Definition.IsOut;
            methodCILBody.Instructions.Add(isRefType ? CilOpCodes.Ldarg : CilOpCodes.Ldarga, parameter);
        }

        if (withReturnValueArg && returnValue != null)
            methodCILBody.Instructions.Add(CilOpCodes.Ldloca, returnValue);
    }


    public static TypeDefinition? GetGeneratedPatcher(DetourPatchInfo patch, ModuleDefinition? moduleDefinition)
    {
        return moduleDefinition?.GetAllTypes()
            .FirstOrDefault(it => it.FullName.EndsWith(patch.Guid.ToClassSafeString()));
    }
}