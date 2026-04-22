using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;

namespace Accord.Builder.Transpiler;

public class TranspilerInfo(
    IReadOnlyList<CilInstruction> matched,
    IEnumerable<CilInstruction> modified,
    IMethodDefOrRef transpilerInstanceRef)
{
    public IReadOnlyList<CilInstruction> Matched { get; } = matched;
    public IEnumerable<CilInstruction> Modified { get; } = modified;
    public IMethodDefOrRef TranspilerInstanceRef { get; } = transpilerInstanceRef;
}
