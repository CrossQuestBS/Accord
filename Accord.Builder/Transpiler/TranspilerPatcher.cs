using System.Reflection;
using Accord.Builder.Detour;
using Accord.Builder.Extensions;
using Accord.Transpiler.Attributes;
using Accord.Transpiler.Interfaces;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;

namespace Accord.Builder.Transpiler;

public static class TranspilerPatcher
{
    public static (CilInstruction[]?, int) GetMatchedInstructions(CilInstruction[] instructions,
        IAccordTranspiler transpilerInstance)
    {
        using var matchStart = transpilerInstance.Match().GetEnumerator();

        List<List<CilMatchResult>> matches = new();
        

        while (matchStart.MoveNext())
        {
            List<CilMatchResult> currentMatches = new();

            if (matchStart.Current is null)
                break;

            for (var i = 0; i < instructions.Length; i++)
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

        return foundMatch ? (instructions[startIndex..(endIndex + 1)], endIndex+1) : (null, -1);
    }

    private static CustomAttribute? GetPatchAttribute(TypeDefinition typeDefinition)
    {
        if (!typeDefinition.HasCustomAttributes)
            return null;
        
        return typeDefinition.CustomAttributes.FirstOrDefault(it => it.Type?.Name == nameof(AccordTranspilerAttribute));
    }

    public class PatchInfo(MethodDefinition patchMethodDefinition, TypeDefinition transpilerBuildType, TypeDefinition transpilerInstanceType)
    {
        public MethodDefinition PatchMethodDefinition = patchMethodDefinition;
        public TypeDefinition TranspilerBuildType = transpilerBuildType;
        public TypeDefinition TranspilerInstanceType = transpilerInstanceType;
    }
    
    public static void Patch(RuntimeContext context, Dictionary<string, Assembly> reflectionAssemblies, string fileSuffix = "_modified_transpiler")
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
            Console.WriteLine($"Running patches: {patches.Count}");
            foreach (var patch in patches)
            {
                var fullPath = Path.GetFullPath(patch.TranspilerBuildType.DeclaringModule.FilePath);
                
                var assemblyFile = Path.GetFileName(fullPath);

                if (!reflectionAssemblies.TryGetValue(assemblyFile, out var reflectionAssembly))
                {
                    Console.WriteLine(reflectionAssemblies.Keys);
                    Console.WriteLine(fullPath);
                    throw new Exception("Failed to get reflection assembly!");
                }
                
                
                Console.WriteLine($"Running patcher on: {patch.PatchMethodDefinition.FullName}");
                var defaultImporter = patch.PatchMethodDefinition.DeclaringModule.DefaultImporter;

                var getInstance =
                    defaultImporter.ImportMethod(
                        patch.TranspilerInstanceType.Methods.FirstOrDefault(it => it.Name == "get_Instance"));

                var patchedCil = RunPatch(reflectionAssembly, defaultImporter, patch.PatchMethodDefinition.CilMethodBody,
                    patch.TranspilerBuildType, patch.TranspilerInstanceType, getInstance);

                foreach (var transpilerInfo in patchedCil)
                {
                    AddTranspiler(patch.PatchMethodDefinition.CilMethodBody, transpilerInfo);
                }
                
                patch.PatchMethodDefinition.CilMethodBody.Instructions.OptimizeMacros();
            }

            var output = Path.Join(Path.GetDirectoryName(assemblyDefinition.ManifestModule.FilePath),
                Path.GetFileNameWithoutExtension(assemblyDefinition.ManifestModule.FilePath) + $"{fileSuffix}.dll");
            
            assemblyDefinition.Write(output);
        }
    }
    
    public static List<TranspilerInfo>? RunPatch(Assembly assembly, ReferenceImporter importer, CilMethodBody? methodBody, TypeDefinition buildType, TypeDefinition modType, IMethodDefOrRef methodDefOrRef)
    {
        var  types = assembly.GetTypes();
        var type = types.FirstOrDefault(it => it.Name == buildType.Name.Value && it.Namespace == buildType.Namespace.Value);
        var instance = (IAccordTranspilerInstance)Activator.CreateInstance(type);

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

        List<TranspilerInfo> transpilerInfos = new List<TranspilerInfo>();
        
        int offset = 0;
        var allInstructions = methodBody.Instructions.ToArray();
        foreach (var transpiler in instance.TranspilerList)
        {
            var instructions = allInstructions[offset..];
            
            Console.WriteLine($"Instructions running: {instructions.Length}");
            
            var (matchedInstructions, endOffset) = GetMatchedInstructions(instructions, transpiler);

            if (matchedInstructions is null)
            {
                Console.WriteLine($"Failed to get matchedInstructions of type {type}");
                return null;
            }

            if (endOffset != -1)
                offset = endOffset;

            var modified = transpiler.Modify(matchedInstructions, modType, methodDefOrRef, importer);

            transpilerInfos.Add(new TranspilerInfo(matchedInstructions, modified, methodDefOrRef));
        }

        return transpilerInfos;
    }
    
    public static void AddTranspiler(CilMethodBody methodBody, TranspilerInfo transpilerInfo)
    {
        var instructions = methodBody.Instructions;
        var matchedInstruction = transpilerInfo.Matched;
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

        AddTranspilerCil(methodBody, transpilerInfo, matchedStartLabel, instructions, trampolineEnd);
    }

    private static void AddTranspilerCil(CilMethodBody methodBody, TranspilerInfo transpilerInfo,
        ICilLabel matchedStartLabel, CilInstructionCollection instructions, CilInstruction trampolineEnd)
    {
        List<CilInstruction> transpilerSetup =
        [
            new(CilOpCodes.Call, transpilerInfo.TranspilerInstanceRef),
            new(CilOpCodes.Brfalse, matchedStartLabel)
        ];

        transpilerSetup.AddRange(transpilerInfo.Modified);

        foreach (var cilInstruction in transpilerSetup)
        {
            instructions.InsertBefore(trampolineEnd, cilInstruction);
        }
    }
}