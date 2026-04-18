using System;
using System.Collections.Generic;
using Accord.Common.Attributes;

namespace Accord.TestCases
{
    [AccordPatch(typeof(ExampleClass), nameof(ExampleClass.Example), [typeof(string[])])]
    public partial class ExampleClassPatch4
    {
        public ExampleClassPatch4()
        {
            Patch();
        }

        public void Postfix(ExampleClass instance, ref ExampleClass.ExampleStruct? arg1)
        {
            throw new NotImplementedException();
        }

        public void Postfix(ExampleClass instance, ref string[] supportArray)
        {
            throw new NotImplementedException();
        }
    }
}