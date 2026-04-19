namespace Accord.Common;

public static class FieldAccessor<T, U>
{
    /// <summary>
    /// A delegate for a field accessor taking a <typeparamref name="T"/> ref and returning a <typeparamref name="U"/> ref.
    /// </summary>
    /// <param name="obj">the object to access the field of</param>
    /// <returns>a reference to the field's value</returns>
    public delegate ref U Accessor(ref T obj);
}
