using System.Threading.Tasks;
using Accord.Common.Attributes;

namespace Accord.TestCases
{
    [AccordPatch(typeof(ExampleClass), nameof(ExampleClass.ExampleAsync), [typeof(string)])]
    public partial class ExampleClassPatchAsync
    {
        public ExampleClassPatchAsync()
        {
            Patch();
        }
        
        public void Postfix(ExampleClass instance, ref string input, ref Task returnValue)
        {
            returnValue = ExampleSleep2();
        }

        public Task ExampleSleep2()
        {
            return Task.Delay(10);
        }
    }
}