using System;
using Accord.Common.Attributes;

namespace Accord.TestCases
{
    [AccordTranspiler]
    public partial class TranspilerExample
    {
        public int ModifiedValue = 20;
    }
    
    public class TranspilerExampleClass
    {
        public void Example()
        {
            Console.WriteLine("Random function!!");
            var b = new Random();
            int randomNumber = b.Next() % 100;
            
            Console.WriteLine($"I do other stuff here!");
            
            if (randomNumber > 49)
            {
                Console.WriteLine("Over 100");
            } else
            {
                Console.WriteLine("Under 100");
            }
            
            Console.WriteLine($"Random number was {randomNumber}");
        }
    }
}