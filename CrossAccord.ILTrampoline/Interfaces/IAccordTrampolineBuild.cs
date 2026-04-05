using System;
using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;

namespace CrossAccord.ILTrampoline.Interfaces;




public enum CilMatch
{
    Start,
    End,
    Strict,
    Relaxed,
    None
}

public interface IAccordTrampolineBuild
{
    public IEnumerable<Func<CilInstruction, CilMatch>> MatchInstructions();
    public IEnumerable<CilInstruction> PatchTrampoline(IEnumerable<CilInstruction> instructions, TypeDefinition definition, CilLocalVariable instance);
}