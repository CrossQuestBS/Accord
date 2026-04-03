using AsmResolver;
using AsmResolver.DotNet;
using CrossAccord.Builder;

namespace CrossAccord.Tests;

public class AssemblyGeneratorTests
{
    private RuntimeContext _context;
    
    [SetUp]
    public void Setup()
    {
        _context = new RuntimeContext(
            targetRuntime: DotNetRuntimeInfo.NetStandard(2, 1));
            
        #if DEBUG
            var assembly = AssemblyDefinition.FromFile(@"../../../../CrossAccord.TestCases/bin/Debug/netstandard2.1/CrossAccord.TestCases.dll", createRuntimeContext: false);
        #else
            var assembly = AssemblyDefinition.FromFile(@"../../../../CrossAccord.TestCases/bin/Release/netstandard2.1/CrossAccord.TestCases.dll", createRuntimeContext: false);
        #endif
        
        _context.AddAssembly(assembly);
    }

    [TestFixture]
    public class GetPatches : AssemblyGeneratorTests
    {
        [Test]
        public void ShouldGeneratePatch()
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
        public void ShouldNotGenerate()
        {
            var emptyContext = new RuntimeContext(
                targetRuntime: DotNetRuntimeInfo.NetStandard(2, 1));
            
            var patches = AssemblyGenerator.GetPatches(emptyContext);
       
            Assert.That(patches.Length, Is.EqualTo(0));
        }
    }
}