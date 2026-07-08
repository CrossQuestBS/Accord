using System.Reflection;

namespace Eccord.Patches;
using PrefixDelegate = PatchedUser.CreditScorePrefix;
using PostfixDelegate = PatchedUser.CreditScorePostfix;

using PatchedUserManger = PatchManager<PatchedUser.CreditScorePrefix, PatchedUser.CreditScorePostfix>;


public class CreditScoreBad : IDisposable
{
    private readonly Patch<PrefixDelegate> PrefixPatch;
    private readonly Patch<PostfixDelegate> PostfixPatch;

    
    private readonly int creditScore;

    private readonly MethodInfo? prefixMethodInfo = typeof(CreditScoreBad).GetMethod("PrefixCreditScore");
    private readonly MethodInfo? conditionalMethodInfo = typeof(CreditScoreBad).GetMethod("PrefixCondition");
    
    private readonly MethodInfo? postfixMethodInfo = typeof(CreditScoreBad).GetMethod("PostfixCreditScore");
    private readonly MethodInfo? conditionalPostfixMethodInfo = typeof(CreditScoreBad).GetMethod("PrefixCondition");

    
    public CreditScoreBad()
    {
        PrefixPatch = new Patch<PrefixDelegate>
        {
            PatchDelegate = (PrefixDelegate)Delegate.CreateDelegate(typeof(PrefixDelegate), this, prefixMethodInfo!),
            ConditionDelegate = (Patch.ConditionalDelegate)Delegate.CreateDelegate(typeof(Patch.ConditionalDelegate), this, conditionalMethodInfo!)
        };
        
        PostfixPatch = new Patch<PostfixDelegate>
        {
            PatchDelegate = (PostfixDelegate)Delegate.CreateDelegate(typeof(PostfixDelegate), this, postfixMethodInfo!),
            ConditionDelegate = (Patch.ConditionalDelegate)Delegate.CreateDelegate(typeof(Patch.ConditionalDelegate), this, conditionalPostfixMethodInfo!)
        };
        
        PatchedUserManger.AddPrefix(PrefixPatch);
        PatchedUserManger.AddPostfix(PostfixPatch);
        creditScore = Random.Shared.Next(10);
    }

    public bool PrefixCondition(Object instance)
    {
        if (instance is not User user)
            return false;

        return user.Country != "Norway";
    }

    public bool PrefixCreditScore(User instance, ref int result)
    {
        Console.WriteLine($"[Prefix]: changing {result} to {creditScore}");
        result = creditScore;
        return false;
    }
    
    public bool PostfixCondition(Object instance)
    {
        if (instance is not User user)
            return false;

        return user.Country != "Norway";
    }

    public void PostfixCreditScore(User instance, ref int result)
    {
        Console.WriteLine($"[Postfix]: {result}");
    }

    public void Dispose()
    {
        PatchedUserManger.RemovePrefix(PrefixPatch);
        PatchedUserManger.RemovePrefix(PrefixPatch);

    }
}
