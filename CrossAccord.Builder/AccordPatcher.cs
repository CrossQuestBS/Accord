using System.Reflection;
using AsmResolver.DotNet;
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

        var context = new RuntimeContext(targetRuntime: DotNetRuntimeInfo.NetStandard(2, 1));
        
        var generatedAssemblyPath = Path.Join(stagingPath, "CrossAccord.Generated.dll");
        
        var uniqueAssemblies = SharedState.PatcherInfos.Select(it => it.AssemblyName).ToHashSet().ToArray();

        context.LoadAssembly(generatedAssemblyPath);
        foreach (var assembly in uniqueAssemblies)
        {
            var path = files.FirstOrDefault(it => it.EndsWith(assembly + ".dll"));
            
            if (path is not null)
                context.LoadAssembly(path);
        }


        foreach (var patcherInfo in SharedState.PatcherInfos)
        {
            Console.WriteLine(patcherInfo);
        }
        
        AssemblyPatcherV2.PatchAll(SharedState.PatcherInfos, context, stagingPath);
    }
}

