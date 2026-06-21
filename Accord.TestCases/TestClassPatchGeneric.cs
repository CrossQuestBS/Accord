using System;
using System.Collections.Generic;
using Accord.Common.Attributes;

namespace Accord.TestCases
{
    [AccordPatch(typeof(ExampleClassGeneric<ExampleClass>), nameof(ExampleClassGeneric<ExampleClass>.Example), new []{typeof(ExampleClass)})]
    public partial class ExampleClassGenericPatch
    {
        public ExampleClassGenericPatch()
        {
            Patch();
        }
        
        public void Postfix(ExampleClassGeneric<ExampleClass> instance, ref ExampleClass what)
        {
            Console.WriteLine("WHAT!");
        }
    }
}