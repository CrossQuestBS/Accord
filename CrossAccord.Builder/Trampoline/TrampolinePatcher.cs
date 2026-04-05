/*using System.Reflection;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Serialized;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using CrossAccord.ILTrampoline.Interfaces;
using Mono.Cecil.Cil;
using AssemblyDefinition = Mono.Cecil.AssemblyDefinition;
using AssemblyDefinition2 = AsmResolver.DotNet.AssemblyDefinition;

using CustomAttribute = Mono.Cecil.CustomAttribute;
using TypeDefinition = Mono.Cecil.TypeDefinition;
using TypeReference = Mono.Cecil.TypeReference;

namespace CrossAccord.Builder.Trampoline;


public static class TrampolinePatcher
{

    public static void PatchAssembly(AssemblyDefinition2 assemblyDefinition, RuntimeContext context, List<TrampolinePatchInfo> patches, Dictionary<string, Assembly> assembliesContext)
    {
        var trampolineGroups = patches.GroupBy(it => it.MethodFullName);

        foreach (var trampolineGroup in trampolineGroups)
        {
            var methodFullName = trampolineGroup.Key;

            // TODO: Fix!
            MethodDefinition methodFound = null;
            var cilMethodBody = methodFound.CilMethodBody;

            foreach (var patchInfo in trampolineGroup)
            {
                
                // Todo fix
                CilInstruction origStartFound = null;
                
                // Todo fix2
                CilInstruction origStartEnd = null;
                
                // Todo fix3
                TypeSignature patcherModType = null;

                // Todo fix4
                IEnumerable<CilInstruction> instructionsModified = new []{ new CilInstruction(CilOpCodes.Nop)};

                // Todo fix5
                IMethodDefOrRef getInstanceDefinition = null;
                
                
                var _origStartLabel = origStartFound.CreateLabel();

                CilInstructionLabel _startPatchLabel = new CilInstructionLabel();
                CilInstructionLabel _endPatchLabel = new CilInstructionLabel();

                var branchBack = new CilInstruction(CilOpCodes.Br, _endPatchLabel);
                
                cilMethodBody.Instructions.InsertBefore(origStartFound,  new CilInstruction(CilOpCodes.Br, _startPatchLabel));
                cilMethodBody.Instructions.InsertAfter(origStartEnd, branchBack);

                //Setup before and after patched instructions
                CilInstruction startPatchInstruction = new CilInstruction(CilOpCodes.Nop);
                _startPatchLabel.Instruction = startPatchInstruction;
                cilMethodBody.Instructions.InsertAfter(branchBack, startPatchInstruction);

                CilInstruction endPatchInstruction = new CilInstruction(CilOpCodes.Nop);
                _endPatchLabel.Instruction = endPatchInstruction;
                cilMethodBody.Instructions.InsertAfter(startPatchInstruction, endPatchInstruction);

                cilMethodBody.Instructions.InsertBefore(endPatchInstruction, new CilInstruction(CilOpCodes.Call, getInstanceDefinition));
                cilMethodBody.Instructions.InsertBefore(endPatchInstruction, new CilInstruction(CilOpCodes.Brfalse, _origStartLabel));

                foreach (var cilInstruction in instructionsModified)
                {
                    cilMethodBody.Instructions.InsertBefore(endPatchInstruction, cilInstruction);
                }

            }

        }
        
    }
    
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
                var endOffsetIdx = Array.IndexOf(instructionsFromOffset, endOffset);

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


                Instruction _startNop = Instruction.Create(OpCodes.Nop);
                Instruction _endNop = Instruction.Create(OpCodes.Nop);

                
                var origEnd = instructionsFromOffset[endOffsetIdx];

                ilProcessor.InsertAfter(origEnd, _startNop);
                ilProcessor.InsertAfter(_startNop, _endNop);
                ilProcessor.InsertBefore(_startNop, Instruction.Create(OpCodes.Br, _endNop));

                
                var startPlace = instructionsFromOffset[0];
                var loadVariable = ilProcessor.Create(OpCodes.Ldloc, variableDefinition);
                var branchInstruction = ilProcessor.Create(OpCodes.Brfalse, startPlace);

                ilProcessor.InsertBefore(_endNop, loadVariable);
                ilProcessor.InsertBefore(_endNop, branchInstruction);
                
                var trampolineInstructions =
                    trampoline.PatchTrampoline(duplicateInstructions, patchType, variableDefinition).ToArray();
               

                foreach (var instruction in trampolineInstructions)
                {
                    ilProcessor.InsertBefore(_endNop, instruction);
                }

                var updatedBranch = false;
                foreach (var instruction in instructions)
                {
                    if (instruction.Operand is not Instruction instructionOperand) continue;
                    if (instructionOperand.Offset == startOffset.Offset)
                    {
                        instruction.Operand = _startNop;
                        updatedBranch = true;
                    }
                }

                if (!updatedBranch)
                {
                    ilProcessor.InsertBefore(instructionsFromOffset[0], ilProcessor.Create(OpCodes.Br, _startNop));
                }
            }
        }

        assembly.Write(assemblyPath);
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
}*/