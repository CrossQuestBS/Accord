using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CrossAccord.TestCases
{
    public class ExampleClass
    {
        public void Example(string input)
        {
            Console.WriteLine(input);
        }
        
        public void Example(List<string> supportGenerics)
        {
            Console.WriteLine(supportGenerics);
        }
    }
}