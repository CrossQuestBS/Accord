using System.Reflection;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Cil;
using CrossAccord.Builder;
using CrossAccord.Builder.Trampoline;
using CrossAccord.ILTrampoline.Interfaces;

namespace CrossAccord.Tests;

public class TrampolinePatcherTests
{
    private RuntimeContext _context;
    private string _assemblyPath;
    private AssemblyDefinition? _assemblyDefinition;


    [SetUp]
    public void Setup()
    {
        _context = new RuntimeContext(
            targetRuntime: DotNetRuntimeInfo.NetStandard(2, 1));

        _assemblyPath = @"../../../../CrossAccord.TestCases/bin/Release/netstandard2.1/CrossAccord.TestCases.dll";
        var assembly = AssemblyDefinition.FromFile(_assemblyPath, createRuntimeContext: false);

        _context.AddAssembly(assembly);
        _assemblyDefinition = assembly;
    }


    public class TestTrampoline : IAccordTrampolineBuild
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

        public IEnumerable<CilInstruction> PatchTrampoline(IEnumerable<CilInstruction> instructions,
            TypeDefinition definition, CilLocalVariable instance, ReferenceImporter importer)
        {
            throw new NotImplementedException();
        }
    }

    [TestFixture]
    public class Patch : TrampolinePatcherTests
    {
        private CilMethodBody? _methodBody;
        private TypeDefinition? _definition;
        private TypeDefinition? _trampolineType;
        
        [SetUp]
        public void FixtureSetup()
        {
            List<string> searchDirectories = [
                "../../../../CrossAccord.TestCases.Build/bin/Release/netstandard2.1",
                "../../../../CrossAccord.TestCases/bin/Release/netstandard2.1",
            ];
            _context = new RuntimeContext(
                targetRuntime: DotNetRuntimeInfo.NetStandard(2, 1),
                searchDirectories: searchDirectories);

            _assemblyPath = @"../../../../CrossAccord.TestCases.Build/bin/Release/netstandard2.1/CrossAccord.TestCases.Build.dll";
            var assembly = AssemblyDefinition.FromFile(_assemblyPath, createRuntimeContext: false);
            _context.AddAssembly(assembly);
            _assemblyDefinition = assembly;
        }

        [Test]
        public void Test()
        {
            var loadAssembly = Assembly.LoadFrom(_assemblyPath);

            Dictionary<string, Assembly> dictionary = new () { };
            dictionary.Add(Path.GetFileName(loadAssembly.Location), loadAssembly);
            
            TrampolinePatcher.Patch(_context, dictionary);
        }
 
    }

    [TestFixture]
    public class GetMatchedInstructions : TrampolinePatcherTests
    {
        private CilMethodBody? _methodBody;
        private TypeDefinition? _definition;
        private TypeDefinition? _trampolineType;

        [SetUp]
        public void FixtureSetup()
        {
            _definition = _assemblyDefinition.ManifestModule.GetAllTypes()
                .FirstOrDefault(it => it.Name.Contains("TrampolineExampleClass"));
            _methodBody = _definition.Methods.FirstOrDefault(it => it.Name == "Example").CilMethodBody;

            _trampolineType = _assemblyDefinition.ManifestModule.GetAllTypes()
                .FirstOrDefault(it => it.Name == "ExampleTrampolineMod");
        }

        [Test]
        public void Test()
        {
            var matches = TrampolinePatcher.GetMatchedInstructions(
                _methodBody.Instructions,
                new TestTrampoline()
            );

            Assert.That(matches.Length, Is.EqualTo(5));
            Assert.That(matches[0].OpCode, Is.EqualTo(CilOpCodes.Newobj));
            Assert.That(matches[^1].OpCode, Is.EqualTo(CilOpCodes.Stloc_0));
        }
    }

    [TestFixture]
    public class AddTrampoline : TrampolinePatcherTests
    {
        private CilMethodBody? _methodBody;
        private TypeDefinition? _definition;
        private TypeDefinition? _trampolineType;

        public class CorrectTrampoline : IAccordTrampolineBuild
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

            public IEnumerable<CilInstruction> PatchTrampoline(IEnumerable<CilInstruction> instructions,
                TypeDefinition definition, CilLocalVariable instance, ReferenceImporter importer)
            {
                throw new NotImplementedException();
            }
        }
        
        public class InvalidTrampoline : IAccordTrampolineBuild
        {
            public IEnumerable<Func<CilInstruction, CilMatch>> MatchInstructions()
            {
                yield return (instruction =>
                {
                    if (instruction.OpCode != CilOpCodes.Callvirt ||
                        instruction.Operand is not MemberReference memberReference ||
                        memberReference.Name != "Next")
                        return CilMatch.None;

                    return CilMatch.Start;
                });
                yield return (instruction => instruction.OpCode == CilOpCodes.Ldc_I4_S ? CilMatch.Strict : CilMatch.None);
                yield return (instruction => instruction.OpCode == CilOpCodes.Rem ? CilMatch.Strict : CilMatch.None);
                yield return (instruction => instruction.OpCode == CilOpCodes.Stloc_0 ? CilMatch.End : CilMatch.None);
            }

            public IEnumerable<CilInstruction> PatchTrampoline(IEnumerable<CilInstruction> instructions,
                TypeDefinition definition, CilLocalVariable instance, ReferenceImporter importer)
            {
                throw new NotImplementedException();
            }
        }

        [SetUp]
        public void Setup2()
        {
            _definition = _assemblyDefinition.ManifestModule.GetAllTypes()
                .FirstOrDefault(it => it.Name.Contains("TrampolineExampleClass"));
            _methodBody = _definition.Methods.FirstOrDefault(it => it.Name == "Example").CilMethodBody;

            _trampolineType = _assemblyDefinition.ManifestModule.GetAllTypes()
                .FirstOrDefault(it => it.Name == "ExampleTrampolineMod");
        }

        

        public IEnumerable<CilInstruction> Modified()
        {
            yield return CilInstruction.CreateLdcI4(40);
            yield return new CilInstruction(CilOpCodes.Stloc_0);
        }

        [Test]
        public void ShouldThrowExceptionIfInvalidStackTrampoline()
        {
            var matched = TrampolinePatcher.GetMatchedInstructions(
                _methodBody.Instructions,
                new InvalidTrampoline()
            );
            var trampolineModSignature = _trampolineType.ToTypeSignature();
            var trampolineInstanceMethod = _trampolineType.Methods.FirstOrDefault(it => it.Name == "get_Instance");

            var memberReference = trampolineInstanceMethod;

            var methodRef = _assemblyDefinition.ManifestModule.DefaultImporter.ImportMethod(memberReference);
            var variable = new CilLocalVariable(trampolineModSignature);
            var info = new TrampolineCilInfo(matched, Modified(), methodRef, variable);

            TrampolinePatcher.AddTrampoline(_methodBody, info);

            var formatter = new CilInstructionFormatter();
            _methodBody.Instructions.CalculateOffsets();
            foreach (CilInstruction _instruction in _methodBody.Instructions)
                Console.WriteLine(formatter.FormatInstruction(_instruction));

            _methodBody.VerifyLabels();

            Assert.Throws<StackImbalanceException>(() => { _methodBody.ComputeMaxStack(); });
        }

        [Test]
        public void ShouldTrampoline()
        {
            var matched = TrampolinePatcher.GetMatchedInstructions(
                _methodBody.Instructions,
                new CorrectTrampoline()
            );
            var trampolineModSignature = _trampolineType.ToTypeSignature();
            var trampolineInstanceMethod = _trampolineType.Methods.FirstOrDefault(it => it.Name == "get_Instance");

            var memberReference = trampolineInstanceMethod;

            var methodRef = _assemblyDefinition.ManifestModule.DefaultImporter.ImportMethod(memberReference);

            var variable = new CilLocalVariable(trampolineModSignature);
            var info = new TrampolineCilInfo(matched, Modified(), methodRef, variable);

            TrampolinePatcher.AddTrampoline(_methodBody, info);

            var formatter = new CilInstructionFormatter();
            _methodBody.Instructions.CalculateOffsets();
            foreach (CilInstruction _instruction in _methodBody.Instructions)
                Console.WriteLine(formatter.FormatInstruction(_instruction));


            Assert.DoesNotThrow(() =>
            {
                _methodBody.VerifyLabels();
                _methodBody.ComputeMaxStack();
            });
        }
    }
}