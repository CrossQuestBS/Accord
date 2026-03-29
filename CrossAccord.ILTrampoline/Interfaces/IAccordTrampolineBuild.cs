using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CrossAccord.ILTrampoline.Interfaces;

public interface IAccordTrampolineBuild
{
    public int Matches { get; }
    public bool StartOffset(Instruction instructions);
    public bool EndOffset(Instruction instructions);
    public IEnumerable<Instruction> PatchTrampoline(IEnumerable<Instruction> instructions, TypeDefinition definition, VariableDefinition instance);
}