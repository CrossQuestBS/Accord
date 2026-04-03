using System;
using System.Reflection;
using CrossAccord.Common.Attributes;

namespace CrossAccord.TestCases
{
    [AccordPatch(typeof(ExampleClass), nameof(ExampleClass.Example))]
    public partial class ExampleClassPatch
    {
        public MemberInfo MemberMethod => typeof(ExampleClass).GetMethod(nameof(ExampleClass.Example))!;
        public void Postfix(ExampleClass instance)
        {
            throw new NotImplementedException();
        }
    }
}