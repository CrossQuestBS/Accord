using System.Reflection;
using Accord.Builder.Detour;
using Accord.Builder.Extensions;
using Accord.ILTrampoline.Interfaces;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;

namespace Accord.Builder.Trampoline;

public static class TrampolinePatcher
{
    public static CilInstruction[]? GetMatchedInstructions(CilInstructionCollection instructions,
        IAccordTrampolineBuild trampoline)
    {
        using var matchStart = trampoline.MatchInstructions().GetEnumerator();

        List<List<CilMatchResult>> matches = new();
        

        while (matchStart.MoveNext())
        {
            List<CilMatchResult> currentMatches = new();

            if (matchStart.Current is null)
                break;

            for (var i = 0; i < instructions.Count; i++)
            {
                var matchResult = matchStart.Current(instructions[i]);
                if (matchResult != CilMatch.None)
                    currentMatches.Add(new CilMatchResult(matchResult, i));
            }

            matches.Add(currentMatches);
        }

        var firstMatchList = matches[0];

        int startIndex = -1;
        int endIndex = -1;

        bool foundMatch = false;

        foreach (var firstMatch in firstMatchList)
        {
            if (firstMatch.Match == CilMatch.Start)
                startIndex = firstMatch.Index;

            int count = 1;

            for (int i = 1; i < matches.Count; i++)
            {
                var currentMatch = matches[i];
                
                var nextMatch = currentMatch.FirstOrDefault(it => it.Index == firstMatch.Index + i);

                if (nextMatch is null)
                    break;

                switch (nextMatch.Match)
                {
                    case CilMatch.Start:
                        startIndex = nextMatch.Index;
                        break;
                    case CilMatch.End:
                        endIndex = nextMatch.Index;
                        break;
                }

                count++;
                if (count == matches.Count)
                    break;
            }

            if (count != matches.Count) continue;
            foundMatch = true;
            break;
        }

        return foundMatch ? instructions.ToArray()[startIndex..(endIndex + 1)] : null;
    }

   
    private static CustomAttribute? GetPatchAttribute(TypeDefinition typeDefinition)
    {
        if (!typeDefinition.HasCustomAttributes)
            return null;
        
        return typeDefinition.CustomAttributes.FirstOrDefault(it => it.Type?.Name == "AccordTrampolineBuildAttribute");
    }


    public class PatchInfo(MethodDefinition patchMethodDefinition, TypeDefinition trampolineBuildType, TypeDefinition trampolineInstanceType)
    {
        public MethodDefinition PatchMethodDefinition = patchMethodDefinition;
        public TypeDefinition TrampolineBuildType = trampolineBuildType;
        public TypeDefinition TrampolineInstanceType = trampolineInstanceType;
    }
    
    
    public static void Patch(RuntimeContext context, Dictionary<string, Assembly> reflectionAssemblies, string fileSuffix = "_modified")
    {
        Dictionary<AssemblyDefinition, List<PatchInfo>> allPatches = new ();
        foreach (var assembly in context.GetLoadedAssemblies())
        {
            if (assembly.ManifestModule is null)
                continue;
            
            foreach (var patchType in assembly.ManifestModule.GetAllTypes())
            {
                var attribute = GetPatchAttribute(patchType);
                
                if (attribute is null)
                    continue;
                
                if (attribute.Signature is null)
                    continue;


                var type = (TypeDefOrRefSignature)attribute.Signature.FixedArguments[0].Element!;
                var methodName = (Utf8String)attribute.Signature.FixedArguments[1].Element!;
                var arguments = (List<object>)attribute.Signature.FixedArguments[2].Elements;
                var typeMod = (TypeDefOrRefSignature)attribute.Signature.FixedArguments[3].Element!;


                if (!type.TryResolve(context, out var toPatchType))
                {
                    Console.WriteLine("Failed to get toPatchType");
                    continue;
                }

                if (!typeMod.TryResolve(context, out var trampolineModType))
                {
                    Console.WriteLine("Failed to get trampolineModType");
                    continue;
                }

                var method = DetourGenerator.GetMethodFromNameAndArguments(toPatchType.Methods.ToList(), methodName, arguments);

                if (method is null)
                {
                    Console.WriteLine($"Failed to find method {methodName.Value}");
                    continue;
                }
                
                
                List<PatchInfo> currentPatches = null;
                
                var patchInfo = new PatchInfo(method, patchType, trampolineModType);
                
                if (!allPatches.TryGetValue(toPatchType.DeclaringModule.Assembly, out currentPatches))
                {
                    allPatches.Add(toPatchType.DeclaringModule.Assembly, [patchInfo]);
                }
                else
                    currentPatches.Add(patchInfo);
            }
        }


        foreach (var (assemblyDefinition, patches) in allPatches)
        {
            
            foreach (var patch in patches)
            {
                var fullPath = Path.GetFullPath(patch.TrampolineBuildType.DeclaringModule.FilePath);
                
                var assemblyFile = Path.GetFileName(fullPath);

                if (!reflectionAssemblies.TryGetValue(assemblyFile, out var reflectionAssembly))
                {
                    Console.WriteLine(reflectionAssemblies.Keys);
                    Console.WriteLine(fullPath);
                    throw new Exception("Failed to get reflection assembly!");
                }
                
                
                Console.WriteLine($"Running patch on: {patch.PatchMethodDefinition.FullName}");
                var defaultImporter = patch.PatchMethodDefinition.DeclaringModule.DefaultImporter;

                var getInstance =
                    defaultImporter.ImportMethod(
                        patch.TrampolineInstanceType.Methods.FirstOrDefault(it => it.Name == "get_Instance"));

                var patchedCil = RunPatch(reflectionAssembly, defaultImporter, patch.PatchMethodDefinition.CilMethodBody,
                    patch.TrampolineBuildType, patch.TrampolineInstanceType, getInstance);
                AddTrampoline(patch.PatchMethodDefinition.CilMethodBody, patchedCil);
                
                patch.PatchMethodDefinition.CilMethodBody.Instructions.OptimizeMacros();
            }

            var output = Path.Join(Path.GetDirectoryName(assemblyDefinition.ManifestModule.FilePath),
                Path.GetFileNameWithoutExtension(assemblyDefinition.ManifestModule.FilePath) + $"{fileSuffix}.dll");
            
            assemblyDefinition.Write(output);
        }
    }
    
    public static TrampolineCilInfo? RunPatch(Assembly assembly, ReferenceImporter importer, CilMethodBody? methodBody, TypeDefinition buildType, TypeDefinition modType, IMethodDefOrRef methodDefOrRef)
    {
        var  types = assembly.GetTypes();
        var type = types.FirstOrDefault(it => it.Name == buildType.Name.Value && it.Namespace == buildType.Namespace.Value);
        var instance = (IAccordTrampolineBuild)Activator.CreateInstance(type);

        if (instance is null)
        {
            Console.WriteLine($"Failed to get Instance of type {type}");
            return null;
        }

        if (methodBody is null)
        {
            Console.WriteLine($"Methodbody is null!");
            return null;
        }
        
        var matchedInstructions = GetMatchedInstructions(methodBody.Instructions, instance);

        if (matchedInstructions is null)
        {
            Console.WriteLine($"Failed to get matchedInstructions of type {type}");
            return null;
        }

        var localVariable = new CilLocalVariable(modType.ToTypeSignature());

        var modified = instance.PatchTrampoline(matchedInstructions,modType,localVariable, importer);

        return new TrampolineCilInfo(matchedInstructions, modified, methodDefOrRef, localVariable);
    }
    
    public static void AddTrampoline(CilMethodBody methodBody, TrampolineCilInfo trampolineCilInfo)
    {
        var instructions = methodBody.Instructions;
        var matchedInstruction = trampolineCilInfo.Matched;
        var matchedStart = matchedInstruction[0];
        var matchedStartLabel = matchedStart.CreateLabel();
        var matchedEnd = matchedInstruction[^1];

        CilInstruction trampolineStart = new CilInstruction(CilOpCodes.Nop);
        CilInstruction trampolineEnd = new CilInstruction(CilOpCodes.Nop);

        instructions.InsertAfter(matchedEnd, trampolineStart);
        instructions.InsertAfter(trampolineStart, trampolineEnd);

        var trampolineStartLabel = trampolineStart.CreateLabel();
        var trampolineEndLabel = trampolineEnd.CreateLabel();

        CilInstruction jumpToTrampoline = new CilInstruction(CilOpCodes.Br, trampolineStartLabel);
        CilInstruction jumpBackToFlow = new CilInstruction(CilOpCodes.Br, trampolineEndLabel);

        instructions.InsertBefore(matchedStart, jumpToTrampoline);
        instructions.InsertBefore(trampolineStart, jumpBackToFlow);

        AddTrampolineCil(methodBody, trampolineCilInfo, matchedStartLabel, instructions, trampolineEnd,
            trampolineCilInfo.TrampolineInstanceVariable);
    }


    private static void AddTrampolineCil(CilMethodBody methodBody, TrampolineCilInfo trampolineCilInfo,
        ICilLabel matchedStartLabel, CilInstructionCollection instructions, CilInstruction trampolineEnd,
        CilLocalVariable trampolineInstance)
    {
        methodBody.LocalVariables.Add(trampolineInstance);

        List<CilInstruction> trampolineSetupInstructions =
        [
            new(CilOpCodes.Call, trampolineCilInfo.TrampolineInstanceRef),
            new(CilOpCodes.Stloc, trampolineInstance),
            new(CilOpCodes.Ldloc, trampolineInstance),
            new(CilOpCodes.Brfalse, matchedStartLabel)
        ];

        trampolineSetupInstructions.AddRange(trampolineCilInfo.Modified);

        foreach (var cilInstruction in trampolineSetupInstructions)
        {
            instructions.InsertBefore(trampolineEnd, cilInstruction);
        }
    }
}