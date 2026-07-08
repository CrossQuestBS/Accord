namespace Eccord;

public class PatchManager<Prefix, Postfix> where Prefix : Delegate where Postfix : Delegate
{
    private static List<Patch<Prefix>> _prefixDelegates = new();
    private static List<Patch<Postfix>> _postfixDelegates = new();

    public static Action<Patch<Prefix>> onDisposedPrefix;
    public static Action<Patch<Postfix>> onDisposedPostfix;
    
    public static List<Patch<Prefix>> PrefixPatches(Object instance)
    {
        return _prefixDelegates.Where(it => it.ConditionDelegate(instance)).ToList();
    }
    
    public static List<Patch<Postfix>> PostfixPatches(Object instance)
    {
        return _postfixDelegates.Where(it => it.ConditionDelegate(instance)).ToList();
    }

    public static void RemovePrefix(Patch<Prefix> patch)
    {
        _prefixDelegates.Remove(patch);
        onDisposedPrefix(patch);
    }

    public static void AddPrefix(Patch<Prefix> patch)
    {
        _prefixDelegates.Add(patch);
    }
    
    public static void RemovePostfix(Patch<Postfix> patch)
    {
        _postfixDelegates.Remove(patch);
        onDisposedPostfix(patch);
    }

    public static void AddPostfix(Patch<Postfix> patch)
    {
        _postfixDelegates.Add(patch);
    }
}