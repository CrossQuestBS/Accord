using AsmResolver;
using AsmResolver.DotNet;
using CrossAccord.Builder;

namespace CrossAccord.Tests;

public class AssemblyGeneratorTests
{
    private RuntimeContext _context;
    private string _assemblyPath;

    
    [SetUp]
    public void Setup()
    {
        _context = new RuntimeContext(
            targetRuntime: DotNetRuntimeInfo.NetStandard(2, 1));
            
        #if DEBUG
            _assemblyPath = @"../../../../CrossAccord.TestCases/bin/Debug/netstandard2.1/CrossAccord.TestCases.dll";
        #else
            _assemblyPath = @"../../../../CrossAccord.TestCases/bin/Release/netstandard2.1/CrossAccord.TestCases.dll";
        #endif
        var assembly = AssemblyDefinition.FromFile(_assemblyPath, createRuntimeContext: false);
        
        _context.AddAssembly(assembly);
    }

    public class GeneratePatcherAssembly : AssemblyGeneratorTests
    {
        private PatcherInfo[] _patcherInfos;
        
        [SetUp]
        public void Setup2()
        {
            _patcherInfos = AssemblyGenerator.GetPatches(_context);
        }

        [Test]
        public void ShouldGenerateValidAssembly()
        {
            var memoryStream = new MemoryStream();
            AssemblyGenerator.GeneratePatcherAssembly(_patcherInfos, [_assemblyPath, typeof(CrossAccord.Common.Attributes.AccordPatchAttribute).Assembly.Location], memoryStream);
            var assembly = AssemblyDefinition.FromStream(memoryStream);
            
            Assert.That(assembly, !Is.Null);
            Assert.That(assembly.Name.ToString(), Is.EqualTo("CrossAccord.Generated"));
        }
    }
    
    [TestFixture]
    public class GetPatches : AssemblyGeneratorTests
    {
        [Test]
        public void ShouldGetPatch()
        {
            var patches = AssemblyGenerator.GetPatches(_context);
       
            Assert.That(patches.Length, Is.EqualTo(1));

            var patch = patches[0];
            Assert.That(patch.AssemblyName, Is.EqualTo("CrossAccord.TestCases.dll"));
            Assert.That(patch.TypeFullName, Is.EqualTo("CrossAccord.TestCases.ExampleClass"));
            Assert.That(patch.MethodFullName, Is.EqualTo("System.Void CrossAccord.TestCases.ExampleClass::Example()"));
            Assert.That(patch.GeneratedCode, !Is.Null);
        }
        
        [Test]
        public void ShouldGetZeroPatches()
        {
            var emptyContext = new RuntimeContext(
                targetRuntime: DotNetRuntimeInfo.NetStandard(2, 1));
            
            var patches = AssemblyGenerator.GetPatches(emptyContext);
       
            Assert.That(patches.Length, Is.EqualTo(0));
        }
    }
}