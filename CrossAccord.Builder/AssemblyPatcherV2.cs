using AsmResolver.DotNet;

namespace CrossAccord.Builder;

public class AssemblyPatcherV2
{
    public static MethodDefinition? FindOriginalMethod(PatcherInfo info, ModuleDefinition? moduleDefinition)
    {
        var type = moduleDefinition?.GetAllTypes()
            .FirstOrDefault(type => type.FullName == info.TypeFullName);

        var method = type?.Methods.FirstOrDefault(it => it.FullName == info.MethodFullName);

        return method;
    }


    public static TypeDefinition? GetGeneratedPatcher(PatcherInfo patch, ModuleDefinition? moduleDefinition)
    {
        return moduleDefinition?.GetAllTypes()
            .FirstOrDefault(it => it.FullName.EndsWith(patch.Guid.ToClassSafeString()));
    }
}