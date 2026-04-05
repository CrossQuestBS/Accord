using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;

namespace CrossAccord.Builder.Trampoline;

public class TrampolineCilInfo(
    IReadOnlyList<CilInstruction> matched,
    IEnumerable<CilInstruction> modified,
    IMethodDefOrRef trampolineInstanceRef,
    TypeSignature trampolineInstanceType)
{
    public IReadOnlyList<CilInstruction> Matched { get; } = matched;
    public IEnumerable<CilInstruction> Modified { get; } = modified;
    public IMethodDefOrRef TrampolineInstanceRef { get; } = trampolineInstanceRef;
    public TypeSignature TrampolineInstanceType { get; } = trampolineInstanceType;
}

public static class CilExtensions
{
    
    public static void InsertBefore(this CilInstructionCollection self, CilInstruction target,
        CilInstruction instruction)
    {
        // Required for self.IndexOf to return correct
        self.CalculateOffsets();
        var index = self.IndexOf(target);
        self.Insert(index, instruction);
    }
    
    public static void InsertAfter(this CilInstructionCollection self, CilInstruction target,
        CilInstruction instruction)
    {
        self.CalculateOffsets();
        var index = self.IndexOf(target) + 1;
        self.Insert(index, instruction);
    }
}

public static class TrampolinePatcherV2
{
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