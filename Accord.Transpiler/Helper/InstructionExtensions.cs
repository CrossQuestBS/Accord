using Accord.Transpiler.Interfaces;
using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;

namespace Accord.Transpiler.Helper;

public static class InstructionExtensions
{
    private static CilMatch Matching(this CilInstruction instruction, CilMatch matchType, CilOpCode opCode,
        object? operand = null, bool forceOperandCheck = false)
    {
        bool hasEqualOpCode = instruction.OpCode == opCode;
        bool hasEqualOperand = instruction.Operand == operand;
        if (operand == null && !forceOperandCheck)
            return hasEqualOpCode ? matchType : CilMatch.None;

        return hasEqualOpCode && hasEqualOperand ? matchType : CilMatch.None;
    }

    private static bool IsCall(this CilInstruction ins)
    {
        CilOpCode opCode = ins.OpCode;

        return opCode == CilOpCodes.Call || opCode == CilOpCodes.Callvirt || opCode == CilOpCodes.Calli;
    }

    public static CilMatch Calls(this CilInstruction ins, CilMatch matchType, string Name)
    {
        if (!ins.IsCall() ||
            ins.Operand is not IMethodDefOrRef method ||
            method.Name != Name)
            return CilMatch.None;

        return matchType;
    }
    
    public static CilMatch LoadsField(this CilInstruction ins, CilMatch matchType, string Name)
    {
        if ((ins.OpCode != CilOpCodes.Ldfld || ins.OpCode != CilOpCodes.Ldflda ) ||
            ins.Operand is not IFieldDescriptor method ||
            method.Name != Name)
            return CilMatch.None;

        return matchType;
    }
    
    
    public static CilMatch Match(this CilInstruction instruction, CilOpCode opCode, object? operand = null, bool forceOperandCheck = false)
    {
        return instruction.Matching(CilMatch.Strict, opCode, operand, forceOperandCheck);
    }
    
    public static CilMatch MatchStart(this CilInstruction instruction, CilOpCode opCode, object? operand = null, bool forceOperandCheck = false)
    {
        return instruction.Matching(CilMatch.Start, opCode, operand, forceOperandCheck);
    }
    
    public static CilMatch MatchEnd(this CilInstruction instruction, CilOpCode opCode, object? operand = null, bool forceOperandCheck = false)
    {
        return instruction.Matching(CilMatch.End, opCode, operand, forceOperandCheck);
    }
}