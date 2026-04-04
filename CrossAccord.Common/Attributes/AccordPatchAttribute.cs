using System;

namespace CrossAccord.Common.Attributes;

/// <summary>
/// Generate patches on nonstatic class <br/>
/// <b>NOTE:</b> This will by default generate to Postfix if not specified.
/// </summary>
/// <param name="declaringType"></param>
/// <param name="methodName"></param>
[AttributeUsage(AttributeTargets.Class)]
public class AccordPatchAttribute : Attribute
{
    public AccordPatchAttribute(Type declaringType, string methodName)
    {
        DeclaringType = declaringType;
        MethodName = methodName;
        Arguments = null;
    }
    
    public AccordPatchAttribute(Type declaringType, string methodName, Type[] arguments)
    {
        DeclaringType = declaringType;
        MethodName = methodName;
        Arguments = arguments;
    }

    public Type DeclaringType;
    public Type[]? Arguments;
    public string MethodName;
}