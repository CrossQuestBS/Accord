namespace Eccord;

public class Patch
{
    public delegate bool ConditionalDelegate(Object instance);
}

public class Patch<T> where T : Delegate
{
    public required T PatchDelegate;
    public required Patch.ConditionalDelegate ConditionDelegate;
}