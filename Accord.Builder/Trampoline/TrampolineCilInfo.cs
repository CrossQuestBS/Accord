using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;

namespace Accord.Builder.Trampoline;

public class TrampolineCilInfo(
    IReadOnlyList<CilInstruction> matched,
    IEnumerable<CilInstruction> modified,
    IMethodDefOrRef trampolineInstanceRef,
    CilLocalVariable trampolineInstanceVariable)
{
    public IReadOnlyList<CilInstruction> Matched { get; } = matched;
    public IEnumerable<CilInstruction> Modified { get; } = modified;
    public IMethodDefOrRef TrampolineInstanceRef { get; } = trampolineInstanceRef;
    public CilLocalVariable TrampolineInstanceVariable { get; } = trampolineInstanceVariable;
}
