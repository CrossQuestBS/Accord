using System.Reflection;

namespace CrossAccord.Builder;

public static class SharedState
{
    public static PatcherInfo[] PatcherInfos { get; set; } = {};
}