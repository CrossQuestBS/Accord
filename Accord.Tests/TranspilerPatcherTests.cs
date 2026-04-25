using System.Reflection;
using Accord.Builder.Transpiler;
using Accord.Transpiler.Helper;
using Accord.Transpiler.Interfaces;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;

namespace Accord.Tests;

public class TranspilerPatcherTests
{
    private RuntimeContext _context;
    private string _assemblyPath;
    private AssemblyDefinition? _assemblyDefinition;


    [SetUp]
    public void Setup()
    {
        _context = new RuntimeContext(
            targetRuntime: DotNetRuntimeInfo.NetStandard(2, 1));

        _assemblyPath = @"../../../../Accord.TestCases/bin/Release/netstandard2.1/Accord.TestCases.dll";
        var assembly = AssemblyDefinition.FromFile(_assemblyPath, createRuntimeContext: false);

        _context.AddAssembly(assembly);
        _assemblyDefinition = assembly;
    }

    

    public class TestTranspilerInstance : IAccordTranspilerInstance
    {
        public readonly IAccordTranspiler[] transpilers = [new Match1()];
        public IAccordTranspiler[] TranspilerList => transpilers;

        public class Match1 : IAccordTranspiler
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

            public IEnumerable<CilInstruction> Modify(IEnumerable<CilInstruction> instructions, TypeDefinition definition, IMethodDefOrRef instance,
                ReferenceImporter importer)
            {
                throw new NotImplementedException();
            }
        }
    }

    [TestFixture]
    public class Patch : TranspilerPatcherTests
    {
        private CilMethodBody? _methodBody;
        private TypeDefinition? _definition;
        private TypeDefinition? _transpilerInstance;


        public void OrigTest(ref string a)
        {
            a = "What!";
        }
        public void Test(string a)
        {
            if (PrefixTest(ref a))
            {
                OrigTest(ref a);
                PostfixTest(ref a);
            }
        }
        public bool PrefixTest(ref string a)
        {
            Console.WriteLine($"Prefix: {a}");
            return true;
        }
        public void PostfixTest(ref string a)
        {
            Console.WriteLine($"Postfix: {a}");
        }
        
        [SetUp]
        public void FixtureSetup()
        {
            List<string> searchDirectories = [
                "../../../../Accord.TestCases.Build/bin/Release/netstandard2.1",
                "../../../../Accord.TestCases/bin/Release/netstandard2.1",
            ];
            _context = new RuntimeContext(
                targetRuntime: DotNetRuntimeInfo.NetStandard(2, 1),
                searchDirectories: searchDirectories);

            _assemblyPath = @"../../../../Accord.TestCases.Build/bin/Release/netstandard2.1/Accord.TestCases.Build.dll";
            var assembly = AssemblyDefinition.FromFile(_assemblyPath, createRuntimeContext: false);
            _context.AddAssembly(assembly);
            _assemblyDefinition = assembly;
        }

        [Test]
        public void Test()
        {
            Test("Hello");
            var loadAssembly = Assembly.LoadFrom(_assemblyPath);

            Dictionary<string, Assembly> dictionary = new () { };
            dictionary.Add(Path.GetFileName(loadAssembly.Location), loadAssembly);
            
            TranspilerPatcher.Patch(_context, dictionary);
        }
 
    }

    [TestFixture]
    public class GetMatchedInstructions : TranspilerPatcherTests
    {
        private CilMethodBody? _methodBody;
        private TypeDefinition? _definition;
        private TypeDefinition? _transpilerInstance;

        [SetUp]
        public void FixtureSetup()
        {
            _definition = _assemblyDefinition.ManifestModule.GetAllTypes()
                .FirstOrDefault(it => it.Name.Contains("TranspilerExampleClass"));
            _methodBody = _definition.Methods.FirstOrDefault(it => it.Name == "Example").CilMethodBody;

            _transpilerInstance = _assemblyDefinition.ManifestModule.GetAllTypes()
                .FirstOrDefault(it => it.Name == "TranspilerExample");
        }

        [Test]
        public void Test()
        {
            var (matches, _) = TranspilerPatcher.GetMatchedInstructions(
                _methodBody.Instructions.ToArray(),
                new TestTranspilerInstance().transpilers[0]
            );
            
            Assert.That(matches.Length, Is.EqualTo(5));
            Assert.That(matches[0].OpCode, Is.EqualTo(CilOpCodes.Newobj));
            Assert.That(matches[^1].OpCode, Is.EqualTo(CilOpCodes.Stloc_0));
        }
    }

    [TestFixture]
    public class AddTranspiler : TranspilerPatcherTests
    {
        private CilMethodBody? _methodBody;
        private TypeDefinition? _definition;
        private TypeDefinition? _transpilerInstance;

        public class CorrectPatchInstance : IAccordTranspilerInstance
        {
            private IAccordTranspiler[] _transpilers = [new CorrectPatch()];
            public IAccordTranspiler[] TranspilerList => _transpilers;
            public class CorrectPatch : IAccordTranspiler
            {
                public IEnumerable<Func<CilInstruction, CilMatch>> Match()
                {
                    yield return ins => ins.MatchStart(CilOpCodes.Newobj);
                    yield return ins => ins.Calls(CilMatch.Strict, "Next");
                    yield return ins => ins.Match(CilOpCodes.Ldc_I4_S);
                    yield return ins => ins.Match(CilOpCodes.Rem);
                    yield return ins => ins.MatchEnd(CilOpCodes.Stloc_0);
                }

                public IEnumerable<CilInstruction> Modify(IEnumerable<CilInstruction> instructions, TypeDefinition definition, IMethodDefOrRef getInstance,
                    ReferenceImporter importer)
                {
                    throw new NotImplementedException();
                }
            }

        }
        
        public class InvalidTranspiler : IAccordTranspilerInstance
        {
            public IAccordTranspiler[] TranspilerList { get; } = [new InvalidPatch()];

            public class InvalidPatch : IAccordTranspiler
            {
                public IEnumerable<Func<CilInstruction, CilMatch>> Match()
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

                public IEnumerable<CilInstruction> Modify(IEnumerable<CilInstruction> instructions, TypeDefinition definition, IMethodDefOrRef getInstance,
                    ReferenceImporter importer)
                {
                    throw new NotImplementedException();
                }
            }
        }

        [SetUp]
        public void Setup2()
        {
            _definition = _assemblyDefinition.ManifestModule.GetAllTypes()
                .FirstOrDefault(it => it.Name.Contains("TranspilerExampleClass"));
            _methodBody = _definition.Methods.FirstOrDefault(it => it.Name == "Example").CilMethodBody;

            _transpilerInstance = _assemblyDefinition.ManifestModule.GetAllTypes()
                .FirstOrDefault(it => it.Name == "TranspilerExample");
        }

        

        public IEnumerable<CilInstruction> Modified()
        {
            yield return CilInstruction.CreateLdcI4(40);
            yield return new CilInstruction(CilOpCodes.Stloc_0);
        }

        [Test]
        public void ShouldThrowExceptionIfInvalidStackTranspiler()
        {
            var (matched, _) = TranspilerPatcher.GetMatchedInstructions(
                _methodBody.Instructions.ToArray(),
                new InvalidTranspiler().TranspilerList[0]
            );
            var trampolineInstanceMethod = _transpilerInstance.Methods.FirstOrDefault(it => it.Name == "get_Instance");

            var memberReference = trampolineInstanceMethod;

            var methodRef = _assemblyDefinition.ManifestModule.DefaultImporter.ImportMethod(memberReference);
            
            var info = new TranspilerInfo(matched, Modified(), methodRef);

            TranspilerPatcher.AddTranspiler(_methodBody, info);

            var formatter = new CilInstructionFormatter();
            _methodBody.Instructions.CalculateOffsets();
            foreach (CilInstruction _instruction in _methodBody.Instructions)
                Console.WriteLine(formatter.FormatInstruction(_instruction));

            _methodBody.VerifyLabels();

            Assert.Throws<StackImbalanceException>(() => { _methodBody.ComputeMaxStack(); });
        }

        [Test]
        public void ShouldTranspile()
        {
            var (matched, _) = TranspilerPatcher.GetMatchedInstructions(
                _methodBody.Instructions.ToArray(),
                new CorrectPatchInstance().TranspilerList[0]
            );
            var trampolineModSignature = _transpilerInstance.ToTypeSignature();
            var trampolineInstanceMethod = _transpilerInstance.Methods.FirstOrDefault(it => it.Name == "get_Instance");

            var memberReference = trampolineInstanceMethod;

            var methodRef = _assemblyDefinition.ManifestModule.DefaultImporter.ImportMethod(memberReference);
            var info = new TranspilerInfo(matched, Modified(), methodRef);

            TranspilerPatcher.AddTranspiler(_methodBody, info);

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