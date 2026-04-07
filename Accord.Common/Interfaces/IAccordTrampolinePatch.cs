namespace Accord.Common.Interfaces;

public interface IAccordTrampolinePatch<T>
{
    public static T Instance { set; get; }
}
