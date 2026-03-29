using Mono.Cecil;

namespace CrossAccord.Builder.Trampoline;

public class TrampolineAttributeMono
{
    public TypeReference PatchedType;
    public string MethodName;
    public TypeReference ModTrampoline;

    public TrampolineAttributeMono(TypeReference patchedType, string methodName, TypeReference modTrampoline)
    {
        PatchedType = patchedType;
        MethodName = methodName;
        ModTrampoline = modTrampoline;
    }
}