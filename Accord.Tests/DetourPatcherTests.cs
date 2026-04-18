using Accord.Builder.Detour;
using Accord.Builder.Extensions;
using Accord.Common.Attributes;
using AsmResolver.DotNet;

namespace Accord.Tests;

public class DetourPatcherTests
{
    private RuntimeContext _context;
    private DetourPatchInfo[] _patcherInfo;
    private ModuleDefinition? _moduleDefinition;
    private readonly DetourPatchInfo _invalidDetourPatchInfo = new ("", "", "", null, new Guid());

    private string _assemblyPath;
    
    private TypeDefinition? FindPatchType(DetourPatchInfo patch)
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
            _assemblyPath = @"../../../../Accord.TestCases/bin/Debug/netstandard2.1/Accord.TestCases.dll";
        #else
            _assemblyPath = @"../../../../Accord.TestCases/bin/Release/netstandard2.1/Accord.TestCases.dll";
        #endif
        var assembly = AssemblyDefinition.FromFile(_assemblyPath, createRuntimeContext: false);
        
        _context.AddAssembly(assembly);

        _moduleDefinition = assembly.ManifestModule ?? null;

        _patcherInfo = DetourGenerator.GetPatches(_context);
    }

    public class PatchAll : DetourPatcherTests
    {
        private ModuleDefinition _patcherDefinition;

        [SetUp]
        public void SetupGeneratedPatcher()
        {
            var memoryStream = new MemoryStream();
            DetourGenerator.GeneratePatcherAssembly(_patcherInfo, [_assemblyPath, typeof(AccordPatchAttribute).Assembly.Location], memoryStream);
            var assembly = AssemblyDefinition.FromStream(memoryStream, createRuntimeContext: false);
            _patcherDefinition = assembly.ManifestModule;
            _context.AddAssembly(assembly);
        }
        
        
        [Test]
        public void ShouldCreateOrigMethod()
        {
            DetourPatcher.PatchAll(_patcherInfo, _context, "", saveAssembly: false);

            foreach (var patcherInfo in _patcherInfo)
            {
                var type = FindPatchType(patcherInfo);
                Assert.That(type.Methods.Any(it => it.Name.ToString().StartsWith("Orig_")), Is.True);
                Assert.That(type.Methods.Count(it => !it.IsConstructor), Is.EqualTo(7));
            }
        }
        
    }

    [TestFixture]
    public class AddPatch : DetourPatcherTests
    {
        private ModuleDefinition _patcherDefinition;

        [SetUp]
        public void SetupGeneratedPatcher()
        {
            var memoryStream = new MemoryStream();
            DetourGenerator.GeneratePatcherAssembly(_patcherInfo, [_assemblyPath, typeof(AccordPatchAttribute).Assembly.Location], memoryStream);
            var assembly = AssemblyDefinition.FromStream(memoryStream);
            _patcherDefinition = assembly.ManifestModule;
        }

        [Test]
        public void ShouldCreateOrigMethod()
        {
            var patch = _patcherInfo[0];

            var type = FindPatchType(patch);
            
            var methodDefinition = DetourPatcher.FindOriginalMethod(patch, type);
            
            var patched = DetourPatcher.GetGeneratedPatcher(patch, _patcherDefinition);
            
            DetourPatcher.AddPatcher(_moduleDefinition, type, patched, methodDefinition);
            
            Assert.That(type.Methods.Any(it => it.Name.ToString().StartsWith("Orig_")), Is.True);
            Assert.That(type.Methods.Count(it => !it.IsConstructor), Is.EqualTo(5));
        }
    }

    [TestFixture]
    public class GetGeneratedPatcher : DetourPatcherTests
    {

        private ModuleDefinition _patcherDefinition;
        
        [SetUp]
        public void SetupGeneratedPatcher()
        {
            var memoryStream = new MemoryStream();
            DetourGenerator.GeneratePatcherAssembly(_patcherInfo, [_assemblyPath, typeof(AccordPatchAttribute).Assembly.Location], memoryStream);
            var assembly = AssemblyDefinition.FromStream(memoryStream);
            _patcherDefinition = assembly.ManifestModule;
        }
        
        [Test]
        public void ShouldReturnNullWithInvalidPatcherInfo()
        {
            var patch = _invalidDetourPatchInfo;
            
            var methodDefinition = DetourPatcher.GetGeneratedPatcher(patch, _patcherDefinition);
            Assert.That(methodDefinition, Is.Null);
        }
        
        [Test]
        public void ShouldReturnType()
        {
            var patch = _patcherInfo[0];
            
            var typeDefinition = DetourPatcher.GetGeneratedPatcher(patch, _patcherDefinition);
            Assert.That(typeDefinition, !Is.Null);
            Assert.That(typeDefinition.FullName.EndsWith(patch.Guid.ToClassSafeString()), Is.True);
        }
        
    }

    [TestFixture]
    public class FindOriginalMethod : DetourPatcherTests
    {

        [Test]
        public void ShouldGetMethod()
        {
            var patch = _patcherInfo[0];

            var type = FindPatchType(patch);
            var methodDefinition = DetourPatcher.FindOriginalMethod(patch, type);
            Assert.That(methodDefinition, !Is.Null);
            Assert.That(methodDefinition.FullName, Is.EqualTo(patch.MethodFullName));
        }
        
        [Test]
        public void ShouldReturnNullWithInvalidPatcherInfo()
        {
            var patch = _invalidDetourPatchInfo;
            var type = FindPatchType(patch);

            var methodDefinition = DetourPatcher.FindOriginalMethod(patch, type);
            Assert.That(methodDefinition, Is.Null);
        }
        
        [Test]
        public void ShouldReturnNullIfEmptyModule()
        {
            var patch = _invalidDetourPatchInfo;

            var methodDefinition = DetourPatcher.FindOriginalMethod(patch, null);
            Assert.That(methodDefinition, Is.Null);
        }
    }
}