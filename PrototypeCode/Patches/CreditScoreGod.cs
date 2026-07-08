using System.Reflection;

namespace Eccord.Patches;
using PrefixDelegate = PatchedUser.CreditScorePrefix;
using PatchedUserManger = PatchManager<PatchedUser.CreditScorePrefix, PatchedUser.CreditScorePostfix>;


public class CreditScoreGood
{
    private readonly Patch<PrefixDelegate> PrefixPatch;

    private readonly int creditScore;
    
    // <generated>
    private readonly MethodInfo? prefixMethodInfo = typeof(CreditScoreGood).GetMethod("PrefixCreditScore");
    private readonly MethodInfo? conditionalMethodInfo = typeof(CreditScoreGood).GetMethod("PrefixCondition");
    
    public CreditScoreGood()
    {
        creditScore = Random.Shared.Next(10_000, 100_000);

        // <generated>
        PrefixPatch = new Patch<PrefixDelegate>
        {
            PatchDelegate = (PrefixDelegate)Delegate.CreateDelegate(typeof(PrefixDelegate), this, prefixMethodInfo!),
            ConditionDelegate = (Patch.ConditionalDelegate)Delegate.CreateDelegate(typeof(Patch.ConditionalDelegate), this, conditionalMethodInfo!)
        };
        
        PatchedUserManger.AddPrefix(PrefixPatch);
    }

    public bool PrefixCondition(Object instance)
    {
        if (instance is not User user)
            return false;

        return user.Country == "Norway";
    }

    public bool PrefixCreditScore(User instance, ref int result)
    {
        result = creditScore;
        return false;
    }
}
