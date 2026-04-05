using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;

namespace CrossAccord.Builder;

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