using AsmResolver;
using AsmResolver.DotNet;
using CrossAccord.Builder;

namespace CrossAccord.Tests;

public class AssemblyPatcherTests
{
    private RuntimeContext _context;
    private PatcherInfo[] _patcherInfo;
    private ModuleDefinition? _moduleDefinition;
    private readonly PatcherInfo _invalidPatcherInfo = new ("", "", "", null, new Guid());

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

        _moduleDefinition = assembly.ManifestModule ?? null;

        _patcherInfo = AssemblyGenerator.GetPatches(_context);
    }

    [TestFixture]
    public class GetGeneratedPatcher : AssemblyPatcherTests
    {

        private ModuleDefinition _patcherDefinition;
        
        [SetUp]
        public void SetupGeneratedPatcher()
        {
            var memoryStream = new MemoryStream();
            AssemblyGenerator.GeneratePatcherAssembly(_patcherInfo, [_assemblyPath, typeof(CrossAccord.Common.Attributes.AccordPatchAttribute).Assembly.Location], memoryStream);
            var assembly = AssemblyDefinition.FromStream(memoryStream);
            _patcherDefinition = assembly.ManifestModule;
        }
        
        [Test]
        public void ShouldReturnNullWithInvalidPatcherInfo()
        {
            var patch = _invalidPatcherInfo;
            
            var methodDefinition = AssemblyPatcherV2.GetGeneratedPatcher(patch, _patcherDefinition);
            Assert.That(methodDefinition, Is.Null);
        }
        
        [Test]
        public void ShouldReturnType()
        {
            var patch = _patcherInfo[0];
            
            var typeDefinition = AssemblyPatcherV2.GetGeneratedPatcher(patch, _patcherDefinition);
            Assert.That(typeDefinition, !Is.Null);
            Assert.That(typeDefinition.FullName.EndsWith(patch.Guid.ToClassSafeString()), Is.True);
        }
        
    }

    [TestFixture]
    public class FindOriginalMethod : AssemblyPatcherTests
    {

        [Test]
        public void ShouldGetMethod()
        {
            var patch = _patcherInfo[0];

            var methodDefinition = AssemblyPatcherV2.FindOriginalMethod(patch, _moduleDefinition);
            Assert.That(methodDefinition, !Is.Null);
            Assert.That(methodDefinition.FullName, Is.EqualTo(patch.MethodFullName));
        }
        
        [Test]
        public void ShouldReturnNullWithInvalidPatcherInfo()
        {
            var patch = _invalidPatcherInfo;

            var methodDefinition = AssemblyPatcherV2.FindOriginalMethod(patch, _moduleDefinition);
            Assert.That(methodDefinition, Is.Null);
        }
        
        [Test]
        public void ShouldReturnNullIfEmptyModule()
        {
            var patch = _invalidPatcherInfo;

            var methodDefinition = AssemblyPatcherV2.FindOriginalMethod(patch, null);
            Assert.That(methodDefinition, Is.Null);
        }
    }
}