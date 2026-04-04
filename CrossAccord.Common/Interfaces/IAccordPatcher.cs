using System;

namespace CrossAccord.Common.Interfaces;

public interface IAccordPatcher
{
    public Type MethodType { get; }
    public string MethodName { get; }
    public Type[] Arguments { get; }
    public void Patch(IAccordPatch patch);
    public void Unpatch(IAccordPatch patch);
}