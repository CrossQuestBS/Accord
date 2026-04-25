using System;
using System.Collections.Generic;
using System.Linq;
using Accord.Transpiler.Attributes;
using Accord.Transpiler.Interfaces;
using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;

namespace Accord.TestCases.Build
{
    [AccordTranspiler(typeof(TranspilerExampleClass), 
        nameof(TranspilerExampleClass.Example), new Type[0], 
        typeof(TranspilerExample))]
    public class TranspilerPatch : IAccordTranspilerInstance
    {
        public IAccordTranspiler[] TranspilerList => [new ExampleTranspiler()];

        public class ExampleTranspiler : IAccordTranspiler
        {
            public IEnumerable<Func<CilInstruction, CilMatch>> Match()
            {
                yield return (instruction => instruction.OpCode == CilOpCodes.Newobj ? CilMatch.Start : CilMatch.None);
                yield return (instruction =>
                {
                    if (instruction.OpCode != CilOpCodes.Callvirt ||
                        instruction.Operand is not MemberReference memberReference ||
                        memberReference.Name != "Next")
                        return CilMatch.None;

                    return CilMatch.Strict;
                });
                yield return (instruction => instruction.OpCode == CilOpCodes.Ldc_I4_S ? CilMatch.Strict : CilMatch.None);
                yield return (instruction => instruction.OpCode == CilOpCodes.Rem ? CilMatch.Strict : CilMatch.None);
                yield return (instruction => instruction.OpCode == CilOpCodes.Stloc_0 ? CilMatch.End : CilMatch.None);
            }

            public IEnumerable<CilInstruction> Modify(IEnumerable<CilInstruction> instructions, TypeDefinition definition, IMethodDefOrRef getInstance, ReferenceImporter importer)
            {
                yield return new CilInstruction(CilOpCodes.Call, getInstance);
                yield return new CilInstruction(CilOpCodes.Ldfld, importer.ImportField(definition.Fields.FirstOrDefault(it => it.Name.Contains("ModifiedValue"))));
                yield return new CilInstruction(CilOpCodes.Stloc_0);
            }
        }
    }
}