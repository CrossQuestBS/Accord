using System;
using System.Collections.Generic;

namespace Accord.TestCases
{
    public class ExampleClass
    {
        public struct ExampleStruct
        {
            public bool what;
        }
        
        public void Example(string input)
        {
            Console.WriteLine(input);
        }
        
        public void ExampleWithStruct(ExampleStruct? exampleStruct)
        {
            Console.WriteLine(exampleStruct.Value.what);
        }
        
        public void Example(List<string> supportGenerics)
        {
            Console.WriteLine(supportGenerics);
        }
    }
}