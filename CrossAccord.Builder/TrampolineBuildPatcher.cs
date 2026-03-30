using System.Reflection;
using CrossAccord.Builder.Trampoline;
using IPA.BuildProcess.Interfaces;

namespace CrossAccord.Builder;

public class TrampolineBuildPatcher : IPostLinkerBuild
{
    public int executeOrder => 1;
    public void Execute(List<string> files, Dictionary<string, Assembly> assemblyDic)
    {
        var assemblies = files.Where(it => it.EndsWith(".dll")).ToArray();

        var assemblyPath = assemblies.First();

        var stagingPath = Path.GetDirectoryName(assemblyPath);

        if (stagingPath is null)
            throw new DirectoryNotFoundException($"Could not find staging directory from {assemblyPath}");

        var buildAssemblies = files.Where(it => it.EndsWith(".Build.dll")).ToArray();

        var patches = TrampolinePatcher.GetAllPatches(buildAssemblies, files.ToArray());

        Console.WriteLine("Running with patches!");

        
        foreach (var (key, value) in patches)
        {
            Console.WriteLine($"Running with patches for: {key}");

            foreach (var patch in value)
            {
                Console.WriteLine($"Method: {patch.MethodFullName}");
                Console.WriteLine($"TrampolineType: {patch.TrampolineTypeFullName}");
            }
            
            TrampolinePatcher.PatchAssembly(key, value, files.ToArray(), assemblyDic);
        }
    }
}