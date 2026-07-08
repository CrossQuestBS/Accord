namespace Eccord;

using PrefixDelegate = PatchedUser.CreditScorePrefix;
using PostfixDelegate = PatchedUser.CreditScorePostfix;
using PatchedUserManger = PatchManager<PatchedUser.CreditScorePrefix, PatchedUser.CreditScorePostfix>;

public class PatchedUser : User
{
    public delegate bool CreditScorePrefix(User instance, ref int result);
    public delegate void CreditScorePostfix(User instance, ref int result);

    private Patch<CreditScorePrefix>[] _creditScorePrefix;
    private Patch<CreditScorePostfix>[] _creditScorePostfix;
    
    public PatchedUser(string name, string country) : base(name, country)
    {
        _creditScorePrefix = PatchedUserManger.PrefixPatches(this).ToArray();
        _creditScorePostfix = PatchedUserManger.PostfixPatches(this).ToArray();
        
        PatchManager<PrefixDelegate, PostfixDelegate>.onDisposedPrefix += RemovePrefix;
        PatchManager<PrefixDelegate, PostfixDelegate>.onDisposedPostfix += RemovePostfix;
    }
    
    public void RemovePrefix(Patch<PrefixDelegate> patch)
    {
        var list = _creditScorePrefix.ToList();
        list.Remove(patch);
        _creditScorePrefix = list.ToArray();
    }
    
    public void RemovePostfix(Patch<PostfixDelegate> patch)
    {
        var list = _creditScorePostfix.ToList();
        list.Remove(patch);
        _creditScorePostfix = list.ToArray();
    }

    public int CreditScore()
    {
        int result = 0;
        bool run = true;

        foreach (var creditScorePrefixPatch in _creditScorePrefix)
        {
            run = creditScorePrefixPatch.PatchDelegate(this, ref result);
        }

        if (run)
            result = base.CreditScore();
        
        foreach (var creditScorePrefixPatch in _creditScorePostfix)
        {
            creditScorePrefixPatch.PatchDelegate(this, ref result);
        }

        return result;
    }
    
    public override string ToString()
    {
        return $"{Name} in {Country} has credit score: {CreditScore()}";
    }
}