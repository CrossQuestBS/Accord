using System;

namespace CrossAccord.ILTrampoline.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class AccordTrampolineBuildAttribute(Type declaringType, string methodName, Type trampolinePatch) : Attribute
{
    public Type DeclaringType = declaringType;
    public string MethodName = methodName;
    public Type TrampolineInstance = trampolinePatch;
}