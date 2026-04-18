using System;
using System.Collections.Generic;
using Accord.Common.Attributes;

namespace Accord.TestCases
{
    [AccordPatch(typeof(ExampleClass), nameof(ExampleClass.ExampleWithStruct), [typeof(Nullable<ExampleClass.ExampleStruct>)])]
    public partial class ExampleClassPatch3
    {
        public ExampleClassPatch3()
        {
            Patch();
        }

        public void Postfix(ExampleClass instance, ref ExampleClass.ExampleStruct? arg1)
        {
            throw new NotImplementedException();
        }
    }
}