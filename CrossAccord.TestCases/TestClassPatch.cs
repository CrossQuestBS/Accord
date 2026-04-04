using System;
using System.Collections.Generic;
using System.Reflection;
using CrossAccord.Common.Attributes;

namespace CrossAccord.TestCases
{
    [AccordPatch(typeof(ExampleClass), nameof(ExampleClass.Example), new []{typeof(List<string>)})]
    public partial class ExampleClassPatch
    {
        public void Postfix(ExampleClass instance, ref List<string> arg1)
        {
            throw new NotImplementedException();
        }
    }
}