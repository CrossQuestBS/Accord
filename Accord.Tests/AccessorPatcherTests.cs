using System.Reflection;
using Accord.Builder.Transpiler;
using Accord.Common;
using Accord.Common.Attributes;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;

namespace Accord.Tests;



public class AccessorPatcherTests
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
        
        [TestFixture]
        public class Patch : AccessorPatcherTests
        {
                private CilMethodBody? _methodBody;
                private TypeDefinition? _definition;
                private TypeDefinition? _transpilerInstance;
        
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

                        _assemblyPath = @"../../../../Accord.TestCases/bin/Release/netstandard2.1/Accord.TestCases.dll";
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
            
                        AccessorPatcher.Patch(_context);
                }
 
        }
}