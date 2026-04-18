using System;
using System.Collections.Generic;
using Accord.Common.Attributes;

namespace Accord.TestCases
{
    [AccordPatch(typeof(ExampleClass), nameof(ExampleClass.Specific), null)]
    public partial class ExampleClassPatchSpecific
    {
        public ExampleClassPatchSpecific()
        {
            Patch();
        }
        
        public void Postfix(ExampleClass instance, ref string input)
        {
            throw new NotImplementedException();
        }
    }
}