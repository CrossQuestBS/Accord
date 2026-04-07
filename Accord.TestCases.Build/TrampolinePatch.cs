using System;
using System.Collections.Generic;
using System.Linq;
using Accord.ILTrampoline.Attributes;
using Accord.ILTrampoline.Interfaces;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;

namespace Accord.TestCases.Build
{
    [AccordTrampolineBuild(typeof(TrampolineExampleClass), nameof(TrampolineExampleClass.Example), new Type[0], typeof(ExampleTrampolineMod))]
    public class TrampolinePatch : IAccordTrampolineBuild
    {
        public IEnumerable<Func<CilInstruction, CilMatch>> MatchInstructions()
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

        public IEnumerable<CilInstruction> PatchTrampoline(IEnumerable<CilInstruction> instructions, TypeDefinition definition, CilLocalVariable instance, ReferenceImporter importer)
        {
            yield return new CilInstruction(CilOpCodes.Ldloc, instance);
            yield return new CilInstruction(CilOpCodes.Ldfld, importer.ImportField(definition.Fields.FirstOrDefault(it => it.Name.Contains("ModifiedValue"))));
            yield return new CilInstruction(CilOpCodes.Stloc_0);

        }
    }
}