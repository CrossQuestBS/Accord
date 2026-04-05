using AsmResolver.DotNet;
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
