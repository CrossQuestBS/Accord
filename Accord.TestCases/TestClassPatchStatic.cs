using System;
using Accord.Common.Attributes;

namespace Accord.TestCases
{
    [AccordPatch(typeof(ExampleClass), nameof(ExampleClass.ExampleStatic), [typeof(string)])]
    public partial class ExampleClassStaticPatch
    {
        public ExampleClassStaticPatch()
        {
            Patch();
        }

        public void Postfix(ref string test)
        {
            throw new NotImplementedException();
        }
    }
}