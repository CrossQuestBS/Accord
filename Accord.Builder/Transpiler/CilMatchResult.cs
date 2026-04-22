using Accord.Transpiler.Interfaces;

namespace Accord.Builder.Transpiler;

public class CilMatchResult(CilMatch match, int index)
{
    public CilMatch Match => match;
    public int Index = index;
}