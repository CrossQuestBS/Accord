using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Accord.TestCases
{
    public class ExampleClassGeneric<TBase> where TBase : ExampleClass
    {
        public void Example(TBase what)
        {
            Console.WriteLine(what.GetType());
        }
    }
    
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

        public static void ExampleStatic(string test)
        {
            Console.WriteLine(test);
        }
        
        public async Task ExampleAsync(string input)
        {
            await Task.Delay(12);
        }
        
        public void Specific(string input)
        {
            Console.WriteLine(input);
        }
        
        public void ExampleWithStruct(ExampleStruct? exampleStruct)
        {
            Console.WriteLine(exampleStruct.Value.what);
        }
        
        public void Example(string[] supportArray)
        {
            Console.WriteLine(supportArray);
        }
        
        public void Example(List<string> supportGenerics)
        {
            Console.WriteLine(supportGenerics);
        }
    }
}