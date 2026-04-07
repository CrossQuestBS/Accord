namespace Accord.Builder.Extensions;

public static class GuidExtensions
{
    public static string ToClassSafeString(this Guid guid)
    {
        return guid.ToString().Replace("-", "").ToUpper();
    }
}