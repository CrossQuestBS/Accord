using System;
using System.Collections.Generic;
using Accord.Common.Attributes;

namespace Accord.TestCases
{
    [AccordPatch(typeof(ExampleClass), nameof(ExampleClass.Example), new []{typeof(List<string>)})]
    public partial class ExampleClassPatch
    {
        public ExampleClassPatch()
        {
            Patch();
        }
        
        public void Postfix(ExampleClass instance, ref List<string> arg1)
        {
            throw new NotImplementedException();
        }
    }
}