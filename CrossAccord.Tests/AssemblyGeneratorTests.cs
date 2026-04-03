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
    public class GetAllPatches : AssemblyGeneratorTests
    {
        [Test]
        public void GetPatchInfo()
        {
            var patches = AssemblyGenerator_V2.GetPatches(_context);
       
            Assert.That(patches.Length, Is.EqualTo(1));
        }
    }
}