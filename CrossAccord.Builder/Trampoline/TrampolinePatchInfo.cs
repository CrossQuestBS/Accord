namespace CrossAccord.Builder.Trampoline;


public class TrampolinePatchInfo(
    string assembly,
    string trampolineTypeFullName,
    string modAssemblyPath,
    string modTypeFullName,
    string methodFullName)
{
    public string Assembly = assembly;
    public string TrampolineTypeFullName = trampolineTypeFullName;
    public string ModAssemblyPath = modAssemblyPath;
    public string ModTypeFullName = modTypeFullName;
    public string MethodFullName = methodFullName;
}