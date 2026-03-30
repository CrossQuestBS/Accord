using System.Reflection;
using CrossAccord.ILTrampoline.Interfaces;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CrossAccord.Builder.Trampoline;

public static class TrampolinePatcher
{
    public static void PatchAssembly(string assemblyPath, List<TrampolinePatchInfo> patches, string[] extraPaths,
        Dictionary<string, Assembly> assembliesContext)
    {
        var parentPath = Path.GetDirectoryName(assemblyPath);
        AssemblyHelper.InitializeResolver(parentPath, extraPaths);
        var assembly = AssemblyHelper.ReadAssemblyInMemory(assemblyPath);

        var patchesByMethodName = patches.GroupBy(it => it.MethodFullName);

        foreach (var group in patchesByMethodName)
        {
            var methodFullName = group.Key;
            var patchType = assembly.MainModule.Types.First(it => it.Methods.Any(it => it.FullName == methodFullName));
            var patchMethod = patchType.Methods.First(it => it.FullName == methodFullName);

            var ilProcessor = patchMethod.Body.GetILProcessor();

            Dictionary<string, Instruction> Labels = new();

            foreach (var patchInfo in group)
            {
                using var trampolineAssemblyDefinition =
                    AssemblyHelper.ReadAssemblyInMemory(patchInfo.PatchAssemblyPath, false);

                using var trampolineModDefinition =
                    AssemblyHelper.ReadAssemblyInMemory(patchInfo.ModAssemblyPath, false);

                if (!assembliesContext.TryGetValue(patchInfo.PatchAssemblyPath, out var trampolineAssembly))
                    continue;

                if (!GetTrampoline(trampolineAssembly, patchInfo.TrampolineTypeFullName, out var trampoline))
                    continue;

                var trampolineModType =
                    trampolineModDefinition.MainModule.Types.First(it => it.FullName == patchInfo.ModTypeFullName);

                var variableDefinition = createTrampolineModDefinition(patchType, trampolineModType);
                patchMethod.Body.Variables.Add(variableDefinition);

                var firstInstruction = patchMethod.Body.Instructions.ToList()[0];

                var getInstance = trampolineModType.Methods.First(it => it.Name.Contains("get_Instance"));

                var getInstanceRef = patchType.Module.ImportReference(getInstance);

                #region Get Trampoline Mod Instance

                var methodInstructions = new List<Instruction>
                {
                    ilProcessor.Create(OpCodes.Call, getInstanceRef),
                    ilProcessor.Create(OpCodes.Stloc, variableDefinition)
                };

                foreach (var instruction in methodInstructions)
                {
                    ilProcessor.InsertBefore(firstInstruction, instruction);
                }

                #endregion

                var instructions = patchMethod.Body.Instructions.ToArray();

                #region StartOffset

                var startOffset = instructions.First(trampoline.StartOffset);
                var startOffsetIdx = Array.IndexOf(instructions, startOffset);

                #endregion

                var instructionsFromOffset = instructions[startOffsetIdx..];

                #region EndOffset

                var endOffset = instructionsFromOffset.First(trampoline.EndOffset);
                var endOffsetIdx = Array.IndexOf(instructionsFromOffset, endOffset) + 1;

                #endregion

                var instructionsRange = instructionsFromOffset[..endOffsetIdx];

                #region Duplicate instructions

                List<Instruction> duplicateInstructions = new List<Instruction>();

                foreach (var instruction in instructionsRange)
                {
                    var duplicate = ilProcessor.Create(OpCodes.Nop);

                    duplicate.OpCode = instruction.OpCode;
                    duplicate.Operand = instruction.Operand;
                    duplicateInstructions.Add(duplicate);
                }

                #endregion

                Instruction endPatch = !Labels.TryGetValue("EndPatch", out var patch)
                    ? SetupEndPatch(ilProcessor, Labels, instructions[^1])
                    : patch;

                var trampolineInstructions =
                    trampoline.PatchTrampoline(duplicateInstructions, patchType, variableDefinition).ToArray();

                var goingBack = instructionsFromOffset[endOffsetIdx];
                var startPlace = instructionsFromOffset[0];
                var loadVariable = ilProcessor.Create(OpCodes.Ldloc, variableDefinition);
                var branchInstruction = ilProcessor.Create(OpCodes.Brfalse, startPlace);
                var branchBack = ilProcessor.Create(OpCodes.Br, goingBack);

                ilProcessor.InsertBefore(endPatch, loadVariable);
                ilProcessor.InsertBefore(endPatch, branchInstruction);

                foreach (var instruction in trampolineInstructions)
                {
                    ilProcessor.InsertBefore(endPatch, instruction);
                }

                ilProcessor.InsertAfter(trampolineInstructions[^1], branchBack);

                var updatedBranch = false;
                foreach (var instruction in instructions)
                {
                    if (instruction.Operand is not Instruction instructionOperand) continue;
                    if (instructionOperand.Offset == startOffset.Offset)
                    {
                        instruction.Operand = loadVariable;
                        updatedBranch = true;
                    }
                }

                if (!updatedBranch)
                {
                    ilProcessor.InsertBefore(instructionsFromOffset[0], ilProcessor.Create(OpCodes.Br, loadVariable));
                }
            }
        }

        assembly.Write(assemblyPath);
    }

    static Instruction SetupEndPatch(ILProcessor ilProcessor, Dictionary<string, Instruction> labels,
        Instruction lastInstruction)
    {
        var startNop = ilProcessor.Create(OpCodes.Nop);

        labels.Add("EndPatch", startNop);
        ilProcessor.InsertBefore(lastInstruction, startNop);
        ilProcessor.InsertBefore(startNop, ilProcessor.Create(OpCodes.Br, lastInstruction));
        return startNop;
    }

    static VariableDefinition createTrampolineModDefinition(TypeDefinition typeDefinition,
        TypeDefinition trampolineModInstance)
    {
        var trampoLineModRef = typeDefinition.Module.ImportReference(trampolineModInstance);
        return new VariableDefinition(trampoLineModRef);
    }

    private static TrampolineAttributeMono GetAttributeMono(CustomAttribute attribute)
    {
        var patchClassType = (TypeReference)attribute.ConstructorArguments[0].Value;
        var patchMethod = (string)attribute.ConstructorArguments[1].Value;
        var typeToUse = (TypeReference)attribute.ConstructorArguments[2].Value;

        return new TrampolineAttributeMono(patchClassType, patchMethod, typeToUse);
    }

    static List<TypeDefinition> GetTrampolineTypes(AssemblyDefinition assemblyDefinition, string attributeName)
    {
        var typesWithCustomAttribute = assemblyDefinition.MainModule.Types
            .Where(it => it.HasCustomAttributes &&
                         it.CustomAttributes.Any(attribute => attribute.AttributeType.Name == attributeName));
        return typesWithCustomAttribute.ToList();
    }


    public static Dictionary<string, List<TrampolinePatchInfo>> GetAllPatches(string[] buildAssemblies,
        string[] allFiles)
    {
        Dictionary<string, List<TrampolinePatchInfo>> output =
            new Dictionary<string, List<TrampolinePatchInfo>>();

        foreach (var buildAssemblyPath in buildAssemblies)
        {
            var parentPath = Path.GetDirectoryName(buildAssemblyPath);
            AssemblyHelper.InitializeResolver(parentPath, allFiles);

            var buildAssemblyDefinition = AssemblyHelper.ReadAssemblyInMemory(buildAssemblyPath, false);

            var trampolineTypes = GetTrampolineTypes(buildAssemblyDefinition, "AccordTrampolineBuildAttribute");

            if (trampolineTypes.Count == 0)
                continue;


            foreach (var trampolineType in trampolineTypes)
            {
                var attribute =
                    trampolineType.CustomAttributes.First(it =>
                        it.AttributeType.Name == "AccordTrampolineBuildAttribute");

                var trampolineAttribute = GetAttributeMono(attribute);

                var properType = trampolineAttribute.PatchedType.Resolve();
                var modType = trampolineAttribute.ModTrampoline.Resolve();

                var methodDefinition = properType.Methods.First(it =>
                    it.Name == trampolineAttribute.MethodName &&
                    it.DeclaringType.FullName == trampolineAttribute.PatchedType.FullName);

                var trampolinePatch = new TrampolinePatchInfo(buildAssemblyPath, trampolineType.FullName,
                    modType.Module.FileName, modType.FullName, methodDefinition.FullName);

                var patchAssemblyPath = properType.Module.FileName;

                if (output.TryGetValue(patchAssemblyPath, out var entry))
                {
                    entry.Add(trampolinePatch);
                    continue;
                }

                var trampolinePatchWithMethod = new List<TrampolinePatchInfo> { trampolinePatch };
                output.Add(patchAssemblyPath, trampolinePatchWithMethod);
            }
        }

        return output;
    }


    private static bool GetTrampoline(Assembly assembly, string trampolineTypeName,
        out IAccordTrampolineBuild trampoline)
    {
        var typeName = assembly.GetType(trampolineTypeName);

        if (typeName is null)
        {
            trampoline = null;
            return false;
        }

        trampoline = (IAccordTrampolineBuild)Activator.CreateInstance(typeName)!;
        return true;
    }
}