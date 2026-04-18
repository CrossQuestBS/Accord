using Accord.Builder.Detour;
using Accord.Common.Attributes;
using AsmResolver.DotNet;

namespace Accord.Tests;

public class DetourGeneratorTests
{
    private RuntimeContext _context;
    private string _assemblyPath;

    
    [SetUp]
    public void Setup()
    {
        _context = new RuntimeContext(
            targetRuntime: DotNetRuntimeInfo.NetStandard(2, 1));
        
        _assemblyPath = @"../../../../Accord.TestCases/bin/Release/netstandard2.1/Accord.TestCases.dll";
        
        var assembly = AssemblyDefinition.FromFile(_assemblyPath, createRuntimeContext: false);
        
        _context.AddAssembly(assembly);
    }

    [TestFixture]
    public class GeneratePatcherDetour : DetourGeneratorTests
    {
        private DetourPatchInfo[] _patcherInfos;
        
        [SetUp]
        public void Setup2()
        {
            _patcherInfos = DetourGenerator.GetPatches(_context);
        }

        [Test]
        public void ShouldGenerateValidAssembly()
        {
            var memoryStream = new MemoryStream();
            DetourGenerator.GeneratePatcherAssembly(_patcherInfos, [_assemblyPath, typeof(AccordPatchAttribute).Assembly.Location], memoryStream);
            var assembly = AssemblyDefinition.FromStream(memoryStream);
            
            Assert.That(assembly, !Is.Null);
            Assert.That(assembly.Name.ToString(), Is.EqualTo("Accord.Generated"));
        }
    }
    
    [TestFixture]
    public class GetPatches : DetourGeneratorTests
    {
        [Test]
        public void ShouldGetPatch()
        {
            var patches = DetourGenerator.GetPatches(_context);
       
            Assert.That(patches.Length, Is.EqualTo(3));

            var patch = patches[0];
            Assert.That(patch.AssemblyName, Is.EqualTo("Accord.TestCases"));
            Assert.That(patch.TypeFullName, Is.EqualTo("Accord.TestCases.ExampleClass"));
            Assert.That(patch.MethodFullName, Is.EqualTo("System.Void Accord.TestCases.ExampleClass::Example(System.Collections.Generic.List`1<System.String>)"));
            Assert.That(patch.GeneratedCode, !Is.Null);
            
            var patch2 = patches[2];
            Assert.That(patch2.AssemblyName, Is.EqualTo("Accord.TestCases"));
            Assert.That(patch2.TypeFullName, Is.EqualTo("Accord.TestCases.ExampleClass"));
            Assert.That(patch2.MethodFullName, Is.EqualTo("System.Void Accord.TestCases.ExampleClass::ExampleWithStruct(System.Nullable`1<Accord.TestCases.ExampleClass+ExampleStruct>)"));
            Assert.That(patch2.GeneratedCode, !Is.Null);
        }
        
        [Test]
        public void ShouldGetZeroPatches()
        {
            var emptyContext = new RuntimeContext(
                targetRuntime: DotNetRuntimeInfo.NetStandard(2, 1));
            
            var patches = DetourGenerator.GetPatches(emptyContext);
       
            Assert.That(patches.Length, Is.EqualTo(0));
        }
    }
}