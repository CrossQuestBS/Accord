using System.Reflection;
using Accord.Builder.Transpiler;
using AsmResolver.DotNet;
using IPA.BuildProcess.Interfaces;

namespace Accord.Builder.Builders;

public class AccessorPatcherBuild : IPostStagingBuild
{
    public int executeOrder => 3;
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
        
        
        foreach (var assembly in assemblies)
        { 
            context.LoadAssembly(assembly);
        }

        Console.WriteLine("Starting Accessor patch!");
        AccessorPatcher.Patch(context, "");
    }
}