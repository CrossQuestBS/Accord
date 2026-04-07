using System.Reflection;
using AsmResolver.DotNet;
using CrossAccord.Builder.Detour;
using IPA.BuildProcess.Interfaces;

namespace CrossAccord.Builder.Builders;

public class DetourPatcherBuild : IPostStagingBuild
{
    public int executeOrder => 2;
    public void Execute(List<string> files, Dictionary<string, Assembly> _)
    {
        var assemblies = files.Where(it => it.EndsWith(".dll")).ToArray();
        
        var assemblyPath = assemblies.First();

        var stagingPath = Path.GetDirectoryName(assemblyPath);

        if (stagingPath is null)
            throw new DirectoryNotFoundException($"Could not find staging directory from {assemblyPath}");

        String[] directories = {stagingPath};
        var context = new RuntimeContext(targetRuntime: DotNetRuntimeInfo.NetFramework(4, 0),
            searchDirectories: directories);
        
        var generatedAssemblyPath = Path.Join(stagingPath, "CrossAccord.Generated.dll");
        
        var uniqueAssemblies = SharedState.PatchInfos.Select(it => it.AssemblyName).ToHashSet().ToArray();

        context.LoadAssembly(generatedAssemblyPath);
        foreach (var assembly in uniqueAssemblies)
        {
            var path = files.FirstOrDefault(it => Path.GetFileName(it) == assembly + ".dll");
            
            Console.WriteLine($"Path: {path}");
            if (path is not null)
                context.LoadAssembly(path);
        }
        
        DetourPatcher.PatchAll(SharedState.PatchInfos, context, stagingPath);
    }
}

