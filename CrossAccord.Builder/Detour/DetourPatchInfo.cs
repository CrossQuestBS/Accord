using Microsoft.CodeAnalysis;

namespace CrossAccord.Builder;

public class DetourPatchInfo
{
    public DetourPatchInfo(string assemblyName, string methodFullName, string typeFullName ,SyntaxTree generatedCode, Guid guid)
    {
        AssemblyName = assemblyName;
        TypeFullName = typeFullName;
        MethodFullName = methodFullName;
        GeneratedCode = generatedCode;
        Guid = guid;
    }

    public override string ToString()
    {
        return $"Assembly: {AssemblyName}\n Method: {MethodFullName}\n Type: {TypeFullName}";
    }

    public string AssemblyName { get; }
    public string TypeFullName { get; }
    public string MethodFullName { get; }
    
    public SyntaxTree GeneratedCode { get; }
    
    public Guid Guid { get; }
}