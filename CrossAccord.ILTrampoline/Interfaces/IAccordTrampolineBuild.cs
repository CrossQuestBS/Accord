using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;

namespace CrossAccord.ILTrampoline.Interfaces;

public class CilInstructionPosition
{
    public CilInstruction Current { get; }
    public CilInstruction? Next { get;  }
    public CilInstruction? Previous { get; }
}

public interface IAccordTrampolineBuild
{
    public bool MatchStart(CilInstructionPosition instructions);
    public bool MatchEnd(CilInstructionPosition instructions);
    public IEnumerable<CilInstruction> PatchTrampoline(IEnumerable<CilInstruction> instructions, TypeDefinition definition, CilLocalVariable instance);
}