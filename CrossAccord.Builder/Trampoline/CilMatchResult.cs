using CrossAccord.ILTrampoline.Interfaces;

namespace CrossAccord.Builder.Trampoline;

public class CilMatchResult(CilMatch match, int index)
{
    public CilMatch Match => match;
    public int Index = index;
}