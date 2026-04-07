using Accord.Builder.Detour;
using AsmResolver.DotNet;
using IPA.BuildProcess.Interfaces;

namespace Accord.Builder.Builders;

public class DetourGeneratorBuild : IPreStagingBuild
{
    public int executeOrder => 1;

    public void Execute(Dictionary<string,List<string>> files)
    {
        
        if (!files.TryGetValue("Libs", out var libDirectoryFiles))
            throw new DirectoryNotFoundException($"Did not find library directory");

        if (!files.TryGetValue("Mods", out var modsFiles))
            throw new DirectoryNotFoundException($"Did not find mods directory");
        
        if (!files.TryGetValue("BeatSaberData", out var beatSaberFiles))
            throw new DirectoryNotFoundException($"Did not find beatsaber data directory");
        
        if (!files.TryGetValue("UnityDependencies", out var unityFiles))
            throw new DirectoryNotFoundException($"Did not find unity dependencies directory");

        List<string> patchAssemblies = new List<string>();
        
        patchAssemblies.AddRange(libDirectoryFiles);
        patchAssemblies.AddRange(modsFiles);
        
        var extraPaths = files.Values.SelectMany(it => it.Select(path => Path.GetDirectoryName(path))).ToHashSet();
        
        var context = new RuntimeContext(
            targetRuntime: DotNetRuntimeInfo.NetFramework(4, 0),
            searchDirectories: extraPaths
        );

        foreach (var file in patchAssemblies)
        {
            context.AddAssembly(AssemblyDefinition.FromFile(file, createRuntimeContext: false));
        }
        
        List<string> assembliesToReference = new List<string>();
        
        assembliesToReference.AddRange(libDirectoryFiles);
        assembliesToReference.AddRange(modsFiles);
        assembliesToReference.AddRange(beatSaberFiles);
        assembliesToReference.AddRange(unityFiles);
        
        var patchers = DetourGenerator.GetPatches(context);
        
        SharedState.PatchInfos = patchers;

        var outputAssemblyPath = Path.Join(Path.GetDirectoryName(libDirectoryFiles[0]), "Accord.Generated.dll");

        if (File.Exists(outputAssemblyPath))
            File.Delete(outputAssemblyPath);
        
        var fileStream = File.Create(outputAssemblyPath);
      
        DetourGenerator.GeneratePatcherAssembly(patchers, assembliesToReference.ToArray(), fileStream);
        fileStream.Close();
    }
}