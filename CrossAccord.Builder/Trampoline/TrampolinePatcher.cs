using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;
using CrossAccord.ILTrampoline.Interfaces;

namespace CrossAccord.Builder.Trampoline;


public static class TrampolinePatcher
{
    public static CilInstruction[]? GetMatchedInstructions(CilInstructionCollection instructions, IAccordTrampolineBuild trampoline)
    {
        var matchStart = trampoline.MatchInstructions().GetEnumerator();
        
        List<List<CilMatchResult>> matches = new();

        var matchIndex = 0;
        while (matchStart.MoveNext())
        {
            List<CilMatchResult> currentMatches = new ();
            
            if (matchStart.Current is null)
                break;
            
            for (int i = 0; i < instructions.Count; i++)
            {
                var matchResult = matchStart.Current(instructions[i]);
                if (matchResult != CilMatch.None)
                    currentMatches.Add(new CilMatchResult(matchResult, i));
            }
            
            matches.Add(currentMatches);
            matchIndex++;
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

                var hasNextMatch = currentMatch.FirstOrDefault(it => it.Index == firstMatch.Index + i);
                if (hasNextMatch is null)
                    continue;

                switch (hasNextMatch.Match)
                {
                    case CilMatch.Start:
                        startIndex = hasNextMatch.Index;
                        break;
                    case CilMatch.End:
                        endIndex = hasNextMatch.Index;
                        break;
                }

                count++;
            }

            if (count == matches.Count)
            {
                foundMatch = true;
                break;
            }
        }

        if (foundMatch)
            return instructions.ToArray()[startIndex..(endIndex+1)];


        return null;
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

        AddTrampolineCil(methodBody, trampolineCilInfo, matchedStartLabel, instructions, trampolineEnd);
    }
    

    private static void AddTrampolineCil(CilMethodBody methodBody, TrampolineCilInfo trampolineCilInfo,
        ICilLabel matchedStartLabel, CilInstructionCollection instructions, CilInstruction trampolineEnd)
    {
        var trampolineInstance = new CilLocalVariable(trampolineCilInfo.TrampolineInstanceType);
        methodBody.LocalVariables.Add(trampolineInstance);

        List<CilInstruction> trampolineSetupInstructions =
        [
            new (CilOpCodes.Call, trampolineCilInfo.TrampolineInstanceRef),
            new (CilOpCodes.Stloc, trampolineInstance),
            new (CilOpCodes.Ldloc, trampolineInstance),
            new (CilOpCodes.Brfalse, matchedStartLabel)
        ];
        
        trampolineSetupInstructions.AddRange(trampolineCilInfo.Modified);

        foreach (var cilInstruction in trampolineSetupInstructions)
        {
            instructions.InsertBefore(trampolineEnd, cilInstruction);
        }
    }
}