using System;

namespace CrossAccord.TestCases
{
    public class ExampleTrampolineMod
    {
        public static ExampleTrampolineMod Instance => new ExampleTrampolineMod();
    }
    
    public class TrampolineExampleClass
    {
        public void Example()
        {
            Console.WriteLine("Random function!!");
            var b = new Random();
            int randomNumber = b.Next() % 100;

            Console.WriteLine("I do other stuff here!");
            
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