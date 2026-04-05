using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.PE.DotNet.Cil;
using CrossAccord.Builder.Trampoline;

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

    [TestFixture]
    public class AddTrampoline : TrampolinePatcherTests
    {
        private CilMethodBody? _methodBody;
        private TypeDefinition? _definition;
        private TypeDefinition? _trampolineType;

        
        
        [SetUp]
        public void Setup2()
        {
            _definition = _assemblyDefinition.ManifestModule.GetAllTypes()
                .FirstOrDefault(it => it.Name.Contains("TrampolineExampleClass"));
            _methodBody = _definition.Methods.FirstOrDefault(it => it.Name == "Example").CilMethodBody;
            
            _trampolineType = _assemblyDefinition.ManifestModule.GetAllTypes()
                .FirstOrDefault(it => it.Name == "ExampleTrampolineMod");
        }

        public List<CilInstruction> CorrectFindInstructions(CilInstructionCollection instructionCollection)
        {
            var startOffset = instructionCollection.ToList().FindIndex(instruction => instruction.OpCode == CilOpCodes.Newobj);

            var offsetInstructions = instructionCollection.ToList()[startOffset..];
            
            
            var endOffset = offsetInstructions.ToList().FindIndex(instruction =>
            {
                return instruction.OpCode == CilOpCodes.Stloc || instruction.OpCode == CilOpCodes.Stloc_0;
            });

            return offsetInstructions[..(endOffset+1)];
        }
        
        public List<CilInstruction> WrongFindInstructions(CilInstructionCollection instructionCollection)
        {
            var startOffset = instructionCollection.ToList().FindIndex(instruction =>
            {
                var correctOpCode = instruction.OpCode == CilOpCodes.Callvirt;

                if (!correctOpCode)
                    return false;

                if (instruction.Operand is not MemberReference memberReference)
                    return false;

                return memberReference.Name == "Next";
            });

            var offsetInstructions = instructionCollection.ToList()[startOffset..];
            
            
            var endOffset = offsetInstructions.ToList().FindIndex(instruction =>
            {
                return instruction.OpCode == CilOpCodes.Stloc || instruction.OpCode == CilOpCodes.Stloc_0;
            });

            return offsetInstructions[..(endOffset+1)];
        }

        public IEnumerable<CilInstruction> Modified()
        {
            yield return CilInstruction.CreateLdcI4(40);
            yield return new CilInstruction(CilOpCodes.Stloc_0);
        }

        [Test]
        public void ShouldThrowExceptionIfInvalidStackTrampoline()
        {
            var instructions = _methodBody.Instructions;
            var matched = WrongFindInstructions(instructions);
            var trampolineModSignature = _trampolineType.ToTypeSignature(); 
            var trampolineInstanceMethod = _trampolineType.Methods.FirstOrDefault(it => it.Name == "get_Instance");
            
            var memberReference = trampolineInstanceMethod;

            var methodRef = _assemblyDefinition.ManifestModule.DefaultImporter.ImportMethod(memberReference);
            var info = new TrampolineCilInfo(matched, Modified(), methodRef, trampolineModSignature);
            
            TrampolinePatcherV2.AddTrampoline(_methodBody, info);
            
            var formatter = new CilInstructionFormatter();
            _methodBody.Instructions.CalculateOffsets();
            foreach (CilInstruction _instruction in _methodBody.Instructions)
                Console.WriteLine(formatter.FormatInstruction(_instruction));

            _methodBody.VerifyLabels();

            Assert.Throws<StackImbalanceException>(() =>
            {
                _methodBody.ComputeMaxStack();
            });
        }

        [Test]
        public void ShouldTrampoline()
        {
            var instructions = _methodBody.Instructions;
            var matched = CorrectFindInstructions(instructions);
            var trampolineModSignature = _trampolineType.ToTypeSignature(); 
            var trampolineInstanceMethod = _trampolineType.Methods.FirstOrDefault(it => it.Name == "get_Instance");

            var memberReference = trampolineInstanceMethod;

            var methodRef = _assemblyDefinition.ManifestModule.DefaultImporter.ImportMethod(memberReference);
            var info = new TrampolineCilInfo(matched, Modified(), methodRef, trampolineModSignature);
            
            TrampolinePatcherV2.AddTrampoline(_methodBody, info);
            
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