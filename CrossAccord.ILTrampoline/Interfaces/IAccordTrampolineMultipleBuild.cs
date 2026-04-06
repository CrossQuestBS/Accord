using System;
using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;

namespace CrossAccord.ILTrampoline.Interfaces;

// TODO: Support this type
public interface IAccordTrampolineMultipleBuild
{
    public List<IEnumerable<Func<CilInstruction, CilMatch>>> MatchInstructions();
    public List<IEnumerable<CilInstruction>> PatchTrampoline(List<IEnumerable<CilInstruction>> instructions, TypeDefinition definition, CilLocalVariable instance);
}