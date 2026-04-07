using System;

namespace Accord.ILTrampoline.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class AccordTrampolineBuildAttribute(Type declaringType, string methodName, Type[] arguments, Type trampolinePatch) : Attribute
{
    public Type DeclaringType = declaringType;
    public string MethodName = methodName;
    public Type[] Arguments = arguments;
    public Type TrampolineInstance = trampolinePatch;
}