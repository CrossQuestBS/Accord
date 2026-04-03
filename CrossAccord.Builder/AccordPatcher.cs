using System.Reflection;
using IPA.BuildProcess.Interfaces;

namespace CrossAccord.Builder;

public class AccordPatcher : IPostStagingBuild
{
    public int executeOrder => 2;
    public void Execute(List<string> files, Dictionary<string, Assembly> _)
    {
        var assemblies = files.Where(it => it.EndsWith(".dll")).ToArray();

        var assemblyPath = assemblies.First();

        var stagingPath = Path.GetDirectoryName(assemblyPath);

        if (stagingPath is null)
            throw new DirectoryNotFoundException($"Could not find staging directory from {assemblyPath}");

        var generatedAssemblyPath = Path.Join(stagingPath, "CrossAccord.Generated.dll");
        
        AssemblyPatcher.PatchAll(SharedState.PatcherInfos, generatedAssemblyPath, stagingPath);
    }
}