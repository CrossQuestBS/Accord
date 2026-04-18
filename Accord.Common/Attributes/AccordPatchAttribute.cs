using System;
using System.Linq;
using System.Reflection;

namespace Accord.Common.Attributes;

/// <summary>
/// Generate patches on nonstatic class <br/>
/// <b>NOTE:</b> This will by default generate to Postfix if not specified.
/// </summary>
/// <param name="declaringType"></param>
/// <param name="methodName"></param>
[AttributeUsage(AttributeTargets.Class)]
public class AccordPatchAttribute(Type declaringType, string methodName, Type[]? arguments) : Attribute
{
    public Type DeclaringType = declaringType;
    public Type[]? Arguments = arguments;
    public string MethodName = methodName;
}