namespace CrossAccord.Builder.Trampoline;


public class TrampolinePatchInfo
{
    public string PatchAssemblyPath;
    public string TrampolineTypeFullName;
    public string ModAssemblyPath;
    public string ModTypeFullName;
    public string MethodFullName;

    public TrampolinePatchInfo(string patchAssemblyPath, 
        string trampolineTypeFullName, 
        string modAssemblyPath, 
        string modTypeFullName,
        string methodFullName)
    {
        PatchAssemblyPath = patchAssemblyPath;
        TrampolineTypeFullName = trampolineTypeFullName;
        ModAssemblyPath = modAssemblyPath;
        ModTypeFullName = modTypeFullName;
        MethodFullName = methodFullName;
    }
}