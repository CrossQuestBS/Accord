using System.Reflection;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Collections;
using AsmResolver.DotNet.Signatures;
using Basic.Reference.Assemblies;
using Accord.Builder.Extensions;
using AsmResolver.PE.DotNet.Metadata.Tables;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace Accord.Builder.Detour;

public class DetourGenerator
{
    public static DetourPatchInfo[] GetPatches(RuntimeContext context)
    {
        List<DetourPatchInfo> output = new();

        foreach (var assembly in context.GetLoadedAssemblies())
        {
            if (assembly.ManifestModule is null)
                continue;
            
            var patches = GetPatchesFromModule(context, assembly.ManifestModule);
            
            output.AddRange(patches);
        }
        
        return output.GroupBy(it => $"{it.AssemblyName}_{it.MethodFullName}").Select(x => x.First()).ToArray();;
    }

    public static MethodDefinition? GetMethodFromNameAndArguments(List<MethodDefinition> methods, Utf8String name, List<Object> arguments)
    {
        var argumentsCount = arguments.Count;

        var methodsFound = methods.Where(it =>
            it.Parameters.Count == argumentsCount &&
            it.Name.Value == name.Value).ToList();

        if (methodsFound.Count <= 0) return null;

        if (methodsFound.Count == 1)
            return methodsFound[0];
        
        var methodForSure = methodsFound.FirstOrDefault(it =>
        {
            var a = Enumerable.Range(0, argumentsCount)
                .Where(i =>
                {
                    return it.Parameters[i].ParameterType.Name == ((TypeSignature)arguments[i]).Name;
                })
                .ToArray();

            return a.Length == argumentsCount;
        });
        
        Console.WriteLine($"Result is {methodForSure}");
        return methodForSure;
    }
    
    private static DetourPatchInfo[] GetPatchesFromModule(RuntimeContext context, ModuleDefinition moduleDefinition)
    {
        List<DetourPatchInfo> output = new();
        
        var patchAttributes = moduleDefinition.GetAllTypes().Select(GetPatchAttribute).Where(it => it != null);

        foreach (var patchAttribute in patchAttributes)
        {
            var arguments = patchAttribute!.Signature!.FixedArguments;

            var classType = (TypeDefOrRefSignature)arguments[0].Element!;
            var methodName = (Utf8String)arguments[1].Element;

            List<Object>? typeMethodArguments = null;
            
            if (arguments.Count == 3)
            {
                if (!arguments[2].IsNullArray)
                    typeMethodArguments = (List<Object>)arguments[2].Elements;
            }

            if (!classType.TryResolve(context, out TypeDefinition definition))
                continue;

            MethodDefinition methodDefinition;
            if (typeMethodArguments != null)
            {
                methodDefinition = GetMethodFromNameAndArguments(definition.Methods.ToList(), methodName, typeMethodArguments);
            }
            else 
                methodDefinition = definition.Methods.FirstOrDefault(it =>
                it.Name == methodName);

            if (methodDefinition is null)
                throw new Exception($"Failed to find method: {methodName} with {arguments.Count} & {typeMethodArguments}");

            var guid = Guid.NewGuid();
            var code = GetSyntaxTree(methodDefinition, guid);
            
            Console.WriteLine(methodDefinition.Name);
            Console.WriteLine(code);
            Console.WriteLine();
            Console.WriteLine();

            var patchInfo = new DetourPatchInfo(methodDefinition.DeclaringModule.Assembly.Name.ToString(), methodDefinition.FullName, classType.FullName, code, guid);
            
            output.Add(patchInfo);
        }

        return output.ToArray();
    }
    
    private static CustomAttribute? GetPatchAttribute(TypeDefinition typeDefinition)
    {
        if (!typeDefinition.HasCustomAttributes)
            return null;


        return typeDefinition.CustomAttributes.FirstOrDefault(it => it.Type?.Name == "AccordPatchAttribute");
    }

    private static string FormatParameter(TypeSignature parameterType)
    {
        if (parameterType is GenericInstanceTypeSignature genericType)
        {
            var output = "global::" +(genericType.GenericType.FullName).Split("`")[0];
            output += "<";
            foreach (var argument in genericType.TypeArguments)
            {
                output += FormatParameter(argument);
            }

            output += ">";
            
            return output.Trim().Replace("+", ".").Replace("modreq(System.Runtime.InteropServices.InAttribute)", "").Replace("&", "");
        }
        else
            return "global::" + parameterType.FullName.Replace("&", "").Replace("+", ".").Replace("modreq(System.Runtime.InteropServices.InAttribute)", "").Trim();

        return "";
    }
    
    private static SyntaxTree GetSyntaxTree(MethodDefinition methodDefinition, Guid guid)
    { 
        var fullClassName = methodDefinition.DeclaringType.FullName.Replace("+", ".");
        var methodName = methodDefinition.Name.ToString();
        var generatedClassName = $"{methodName}Patcher_{guid.ToClassSafeString()}".Replace(".ctor", "Constructor");

        
        var parameters = "";

        List<string> totalParameters = new();
        List<string> simpleParameters = new();

        var parameterSimpleValue = "";

        if (!methodDefinition.IsStatic)
        {
            totalParameters.Add($"global::{fullClassName} instance");
            simpleParameters.Add("instance");
        }
        
        if (methodDefinition.Parameters.Count > 0)
        {
            simpleParameters.AddRange(methodDefinition.Parameters.Select((it, idx) => $"ref arg{idx + 1}").ToArray());
            totalParameters.AddRange( methodDefinition.Parameters.Select((it, idx) => $"ref {FormatParameter(it.ParameterType)} arg{idx+1}").ToArray());
        }

        if (methodDefinition.Signature.ReturnType.Name != "Void")
        {
            totalParameters.Add($"ref global::{methodDefinition.Signature.ReturnType.FullName} returnValue");
            simpleParameters.Add("ref returnValue");
        }

        var arguments =
            String.Join(",", methodDefinition.Parameters.Select(it => $"typeof({FormatParameter(it.ParameterType)})"));

        parameters = string.Join(", ", totalParameters);
        parameterSimpleValue = string.Join(", ", simpleParameters);


        return CSharpSyntaxTree.ParseText($@"
using System.Runtime.CompilerServices;
using System;
using System.Collections.Generic;
using System.Reflection;
using Accord.Common.Interfaces;
using Accord.Common;

namespace Accord.Generated.{fullClassName}.{methodName.Replace(".", "Dot")};

public class {generatedClassName} : IAccordPatcher
{{
    public global::System.Type MethodType => typeof(global::{fullClassName});
    public string MethodName => ""{methodDefinition.Name.Value}"";
    public global::System.Type[] Arguments => new global::System.Type[] {{{arguments}}};

    private delegate bool PrefixDelegate({parameters});

    private delegate void PostfixDelegate({parameters});

    private static readonly Dictionary<IAccordPatch, PostfixDelegate> PostfixDict = new();
    private static readonly Dictionary<IAccordPatch, PrefixDelegate> PrefixDict = new();

    public static {generatedClassName} Instance {{ get; }} = new();

    private {generatedClassName}()
    {{
    }}

    public void Patch(IAccordPatch instance)
    {{
        var prefixMethodInfo = instance.GetPatchMethodInfo(""Prefix"");

        if (prefixMethodInfo is not null)
        {{
            PrefixDict.Add(instance, (PrefixDelegate)Delegate.CreateDelegate(typeof(PrefixDelegate), instance, prefixMethodInfo));
        }}

        var postfixMethodInfo = instance.GetPatchMethodInfo(""Postfix"");

        if (postfixMethodInfo is not null)
        {{
            PostfixDict.Add(instance, (PostfixDelegate)Delegate.CreateDelegate(typeof(PostfixDelegate), instance, postfixMethodInfo));
        }}
    }}

    public void Unpatch(IAccordPatch instance)
    {{
        PrefixDict.Remove(instance);
        PostfixDict.Remove(instance);
    }}

    bool Prefix({parameters})
    {{
        foreach (var keyValue in PrefixDict)
        {{
            try
            {{
                if (!keyValue.Value({parameterSimpleValue}))
                    return false;
            }}
            catch (Exception e)
            {{
                Unpatch(keyValue.Key);
            }}
        }}

        return true;
    }}

    void Postfix({parameters})
    {{
        foreach (var keyValue in PostfixDict)
        {{
            try
            {{
                keyValue.Value({parameterSimpleValue});
            }}
            catch (Exception e)
            {{
                Unpatch(keyValue.Key);
            }}
        }}
    }}
}}");
    }

    private static string Replace(Parameter it)
    {
        var parameterName = it.ParameterType.FullName;

        if (parameterName.Contains("`") && parameterName.Contains("<"))
        {
            var part1 = parameterName.Split("`")[0] + "<";
            var part2 = parameterName.Split("`")[1].Split("<")[1];
            parameterName = part1 + part2;
        }
        
        return parameterName.Replace("&", "").Replace("+", ".").Replace("modreq(System.Runtime.InteropServices.InAttribute)", "");
    }

    public static void GeneratePatcherAssembly(DetourPatchInfo[] allPatchers, string[] assemblies, Stream outputStream)
    {
        var patchers = allPatchers;
        
        List<MetadataReference> metadataReferences = new();
        
        metadataReferences.AddRange(NetStandard21.References.All);

        foreach (var assemblyPath in assemblies)
        {
            metadataReferences.Add(MetadataReference.CreateFromFile(assemblyPath));
        }

        var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
        var topLevelBinderFlagsProperty = typeof(CSharpCompilationOptions).GetProperty("TopLevelBinderFlags", BindingFlags.Instance | BindingFlags.NonPublic);
        topLevelBinderFlagsProperty.SetValue(compilationOptions, (uint)1 << 22);

#if DEBUG
        foreach (var patcher in patchers)
        {
            Console.WriteLine($"Trying to compile for {patcher.MethodFullName} {patcher.TypeFullName} {patcher.Guid} {patcher.AssemblyName}");
            CSharpCompilation compilation2 = CSharpCompilation.Create(
                "Accord.Generated",
                syntaxTrees: [patcher.GeneratedCode],
                references: metadataReferences.ToArray(),
                options: compilationOptions);
      
            using var ms2 = new MemoryStream();

            EmitResult result2 = compilation2.Emit(ms2);

            if (!result2.Success)
            {
                IEnumerable<Diagnostic> failures = result2.Diagnostics.Where(diagnostic =>
                    diagnostic.IsWarningAsError ||
                    diagnostic.Severity == DiagnosticSeverity.Error);
            
                foreach (Diagnostic diagnostic in failures)
                {
                    throw new Exception(string.Format("Failed to compile code '{0}'! {1}: {2}", diagnostic.AdditionalLocations, diagnostic.Id,
                        diagnostic.GetMessage()));
                }

                throw new Exception("Unknown error while compiling code");
            }
        } 
#endif
     
        
        
        CSharpCompilation compilation = CSharpCompilation.Create(
            "Accord.Generated",
            syntaxTrees: patchers.Select(it => it.GeneratedCode),
            references: metadataReferences.ToArray(),
            options: compilationOptions);
      
        using var ms = new MemoryStream();

        EmitResult result = compilation.Emit(ms);

        if (!result.Success)
        {
            IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                diagnostic.IsWarningAsError ||
                diagnostic.Severity == DiagnosticSeverity.Error);
            
            foreach (Diagnostic diagnostic in failures)
            {
                throw new Exception(string.Format("Failed to compile code '{0}'! {1}: {2}", diagnostic.AdditionalLocations, diagnostic.Id,
                    diagnostic.GetMessage()));
            }

            throw new Exception("Unknown error while compiling code");
        }
        
        ms.Seek(0, SeekOrigin.Begin);
        ms.CopyTo(outputStream);
    }
}