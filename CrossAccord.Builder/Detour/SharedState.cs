using System.Reflection;

namespace CrossAccord.Builder;

public static class SharedState
{
    public static DetourPatchInfo[] PatchInfos { get; set; } = {};
}