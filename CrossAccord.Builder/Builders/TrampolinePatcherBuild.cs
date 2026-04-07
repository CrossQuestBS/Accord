using System.Reflection;
using AsmResolver.DotNet;
using CrossAccord.Builder.Trampoline;
using IPA.BuildProcess.Interfaces;

namespace CrossAccord.Builder.Builders;

public class TrampolinePatcherBuild : IPostStagingBuild
{
    public int executeOrder => 1;
    public void Execute(List<string> files, Dictionary<string, Assembly> assemblyDictionary)
    {
        var assemblies = files.Where(it => it.EndsWith(".dll")).ToArray();
        
        var assemblyPath = assemblies.First();

        var stagingPath = Path.GetDirectoryName(assemblyPath);

        if (stagingPath is null)
            throw new DirectoryNotFoundException($"Could not find staging directory from {assemblyPath}");

        String[] directories = {stagingPath};
        
        var context = new RuntimeContext(targetRuntime: DotNetRuntimeInfo.NetFramework(4, 0),
            searchDirectories: directories);
        
        var buildAssemblies = files.Where(it => it.EndsWith(".Build.dll")).ToArray();
        
        foreach (var assembly in buildAssemblies)
        { 
            context.LoadAssembly(assembly);
        }

        Console.WriteLine("Starting trampoline patch!");
        TrampolinePatcher.Patch(context, assemblyDictionary, "");
    }
}