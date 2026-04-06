using System.Reflection;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using CrossAccord.ILTrampoline.Interfaces;

namespace CrossAccord.Builder.Trampoline;

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
                    continue;

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
            }

            if (count != matches.Count) continue;
            foundMatch = true;
            break;
        }

        return foundMatch ? instructions.ToArray()[startIndex..(endIndex + 1)] : null;
    }

    public static IAccordTrampolineBuild? GetInstance(Assembly assembly, string typeName)
    {
        var type = assembly.GetType(typeName);
        
        if (type is null)
        {
            return null;
        }

        return (IAccordTrampolineBuild)Activator.CreateInstance(type);
    }
    
    

    public static TrampolineCilInfo? GetInfo(Assembly assembly, CilMethodBody methodBody, TypeDefinition typeDefinition, IMethodDefOrRef methodDefOrRef)
    {
        var instance = GetInstance(assembly, "Todo");

        if (instance is null)
            return null;
        
        var matchedInstructions = GetMatchedInstructions(methodBody.Instructions, instance);

        if (matchedInstructions is null)
            return null;

        var localVariable = new CilLocalVariable(typeDefinition.ToTypeSignature());

        var modified = instance.PatchTrampoline(matchedInstructions,typeDefinition,localVariable);

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