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
    
    private TypeDefinition? FindPatchType(PatcherInfo patch)
    {
        var type = _moduleDefinition?.GetAllTypes()
            .FirstOrDefault(type => type.FullName == patch.TypeFullName);
        return type;
    }
    
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

    public class PatchAll : AssemblyPatcherTests
    {
        private ModuleDefinition _patcherDefinition;

        [SetUp]
        public void SetupGeneratedPatcher()
        {
            var memoryStream = new MemoryStream();
            AssemblyGenerator.GeneratePatcherAssembly(_patcherInfo, [_assemblyPath, typeof(CrossAccord.Common.Attributes.AccordPatchAttribute).Assembly.Location], memoryStream);
            var assembly = AssemblyDefinition.FromStream(memoryStream, createRuntimeContext: false);
            _patcherDefinition = assembly.ManifestModule;
            _context.AddAssembly(assembly);
        }
        
        
        [Test]
        public void ShouldCreateOrigMethod()
        {
            AssemblyPatcherV2.PatchAll(_patcherInfo, _context, "", saveAssembly: false);

            foreach (var patcherInfo in _patcherInfo)
            {
                var type = FindPatchType(patcherInfo);
                Assert.That(type.Methods.Any(it => it.Name.ToString().StartsWith("Orig_")), Is.True);
                Assert.That(type.Methods.Count(it => !it.IsConstructor), Is.EqualTo(3));
            }
        }
        
    }

    [TestFixture]
    public class AddPatch : AssemblyPatcherTests
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
        public void ShouldCreateOrigMethod()
        {
            var patch = _patcherInfo[0];

            var type = FindPatchType(patch);
            
            var methodDefinition = AssemblyPatcherV2.FindOriginalMethod(patch, type);
            
            var patched = AssemblyPatcherV2.GetGeneratedPatcher(patch, _patcherDefinition);
            
            AssemblyPatcherV2.AddPatcher(_moduleDefinition, type, patched, methodDefinition);
            
            Assert.That(type.Methods.Any(it => it.Name.ToString().StartsWith("Orig_")), Is.True);
            Assert.That(type.Methods.Count(it => !it.IsConstructor), Is.EqualTo(3));
        }
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

            var type = FindPatchType(patch);
            var methodDefinition = AssemblyPatcherV2.FindOriginalMethod(patch, type);
            Assert.That(methodDefinition, !Is.Null);
            Assert.That(methodDefinition.FullName, Is.EqualTo(patch.MethodFullName));
        }
        
        [Test]
        public void ShouldReturnNullWithInvalidPatcherInfo()
        {
            var patch = _invalidPatcherInfo;
            var type = FindPatchType(patch);

            var methodDefinition = AssemblyPatcherV2.FindOriginalMethod(patch, type);
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