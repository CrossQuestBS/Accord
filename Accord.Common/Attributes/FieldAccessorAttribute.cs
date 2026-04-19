using System;
using System.Linq;
using System.Reflection;

namespace Accord.Common.Attributes;

/// <summary>
/// GenerateFieldAccessor <br/>
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class FieldAccessorAttribute(string fieldName) : Attribute
{
    public string FieldName = fieldName;
}