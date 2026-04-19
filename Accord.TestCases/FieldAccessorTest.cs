using System;
using Accord.Common;
using Accord.Common.Attributes;

namespace Accord.TestCases;



public class ExampleReadOnly
{
    public readonly string fieldTest;

    public ExampleReadOnly()
    {
        fieldTest = "What!";
    }
}

[Accessor]
public static class FieldAccessorTest
{
    /*
     * TODO: Make this part of generator (?)
     */
    [FieldAccessor("fieldTest")]
    private static ref string FieldTestDelegate(ref ExampleReadOnly target)
    {
        throw new NotImplementedException();
    }
    
    public static FieldAccessor<ExampleReadOnly, string>.Accessor _fieldTest = FieldTestDelegate;
}
