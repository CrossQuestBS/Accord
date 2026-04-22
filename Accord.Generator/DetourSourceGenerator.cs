using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;


namespace Accord.Generator;

[Generator]
public class DetourSourceGenerator : IIncrementalGenerator
{

    private static string FormatParameter(ITypeSymbol typeSymbol)
    {

        var specialType = typeSymbol.SpecialType != SpecialType.None;

        if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
        {
            if (arrayTypeSymbol.ElementType.SpecialType != SpecialType.None)
                return arrayTypeSymbol.ToDisplayString().Replace("<global namespace>", "");
            
            return "global::" + arrayTypeSymbol.ToDisplayString().Replace("<global namespace>", "");
        }
        
        var displayName = typeSymbol.ToDisplayString().Replace("<global namespace>", "");

        if (typeSymbol is INamedTypeSymbol parameterType && parameterType.IsGenericType && displayName.Contains("<"))
        {
            var output = (specialType ? "" : "global::") + parameterType.ToDisplayString().Replace("<global namespace>", "").Split('<')[0];
            output += "<";

            foreach (var argument in parameterType.TypeArguments)
            {
                output += FormatParameter(argument);
            }

            output += ">";

            return output;
        }

        
        
        return (specialType ? "" : "global::")  + displayName;
    }
    
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        
        var provider = context.SyntaxProvider
            .CreateSyntaxProvider(
                (s, _) => s is ClassDeclarationSyntax,
                (ctx, _) => GetClassDeclarationSyntaxForSourceGen(ctx))
            .Where(t => t.reportAttributeFound)
            .Select((t, _) => t.Item1);

        context.RegisterSourceOutput(context.CompilationProvider.Combine(provider.Collect()),
            ((ctx, t) => GenerateCode(ctx, t.Left, t.Right)));
    }

    private static (ClassDeclarationSyntax, bool reportAttributeFound) GetClassDeclarationSyntaxForSourceGen(
        GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

        foreach (AttributeListSyntax attributeListSyntax in classDeclarationSyntax.AttributeLists)
        foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
        {
            if (context.SemanticModel.GetSymbolInfo(attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                continue;

            string attributeName = attributeSymbol.ContainingType.ToDisplayString();

            if (attributeName == "Accord.Common.Attributes.AccordPatchAttribute")
                return (classDeclarationSyntax, true);
        }

        return (classDeclarationSyntax, false);
    }

    private string GeneratePatchParameters(INamedTypeSymbol classSymbol, IMethodSymbol methodSymbol)
    {
        StringBuilder sb = new();

        List<string> listParameters = new();

        
        if (!methodSymbol.IsStatic)
        {
            var instanceParameter = "";

            instanceParameter += $"{FormatParameter(classSymbol)} instance";
            listParameters.Add(instanceParameter);
        }
        
        var methodArguments = methodSymbol.Parameters.ToList();
        
        if (methodArguments.Count > 0)
        {
            for (int i = 0; i < methodArguments.Count; i++)
            {
                var currParameter = "";
                var parameterSymbol = methodArguments[i];

                if (parameterSymbol.RefKind == RefKind.In)
                {
                    currParameter += "ref ";
                }
                else
                {
                    currParameter += "ref ";
                }
                
                currParameter += $"{FormatParameter(parameterSymbol.Type)} {parameterSymbol.Name}";
                
                listParameters.Add(currParameter);
            }
        }

        if (methodSymbol.ReturnType.Name == "Void") return string.Join(", ", listParameters);

        var returnTypeParameter = ("ref ");
        var returnTypeNamespace = methodSymbol.ReturnType.ContainingNamespace.ToDisplayString().Replace("<global namespace>", "global::");

        if (returnTypeNamespace.Length > 0 && returnTypeNamespace != "global::")
        {
            returnTypeParameter += (returnTypeNamespace + ".");
        }

        returnTypeParameter += ($"{methodSymbol.ReturnType.Name} returnValue");

        listParameters.Add(returnTypeParameter);
        
        return string.Join(", ", listParameters);
    }
    
    private void GenerateCode(SourceProductionContext context, Compilation compilation,
        ImmutableArray<ClassDeclarationSyntax> classDeclarationSyntaxes)
    {
        foreach (var classDeclarationSyntax in classDeclarationSyntaxes)
        {
            var semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);

            if (semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol classSymbol)
                continue;

            var namespaceName = classSymbol.ContainingNamespace.ToDisplayString().Replace("<global namespace>", "global::");

            var className = classDeclarationSyntax.Identifier.Text;
            
            var generatedAttribute = classSymbol.GetAttributes()
                .First(it => it.AttributeClass?.Name == "AccordPatchAttribute");

            if (generatedAttribute.ConstructorArguments[0].Value is not INamedTypeSymbol patchClassSymbol)
                continue;
            
            if (generatedAttribute.ConstructorArguments[1].Value is not string methodName)
                continue;

            List<ITypeSymbol>? argumentsTypes = null;

          
            
            if (generatedAttribute.ConstructorArguments.Length > 2 && !generatedAttribute.ConstructorArguments[2].IsNull)
            {
                var argument3 = generatedAttribute.ConstructorArguments[2];
                
                argumentsTypes = new List<ITypeSymbol>();
                foreach (var arguments in argument3.Values)
                {
                    if (arguments.Value is ITypeSymbol typeSymbol)
                        argumentsTypes.Add(typeSymbol);
                } 
            }
            
            var methods = patchClassSymbol.GetMembers().OfType<IMethodSymbol>().ToArray();
            
            if (methods.Length == 0)
                continue;

            IMethodSymbol? method = null;
            try
            {
                if (argumentsTypes is not null)
                {
                    var firstIter = patchClassSymbol.GetMembers()
                        .OfType<IMethodSymbol>()
                        .Where(it => it.Name == methodName && it.Parameters.Length == argumentsTypes.Count);

                    method = firstIter.First(it =>
                    {
                        var a = Enumerable.Range(0, argumentsTypes.Count)
                            .Where(i => it.Parameters[i].Type.Name == argumentsTypes[i].Name || 
                                        (it.Parameters[i].ContainingType != null && it.Parameters[i].ContainingType.Name == argumentsTypes[i].Name) || 
                                        (argumentsTypes[i].ToString() == it.Parameters[i].Type.ToString()))
                            .ToArray();

                        return a.Length == argumentsTypes.Count;
                    });
                }
            }
            catch (Exception e)
            {
                var allPotentialArguments = patchClassSymbol.GetMembers().OfType<IMethodSymbol>().Where(it => it.Name == methodName)
                    .Select(it => it.Parameters).Select(it => it.Select(a => a.Type.ToString()));

                var all = string.Join("     ",allPotentialArguments.Select(it => string.Join(",", it)));
                
                throw new Exception($"Wanted to find {String.Join(",", argumentsTypes.Select(it => it.ToString()))} Alternatives: {all}" + e.Message);
            }

            if (generatedAttribute.ConstructorArguments[2].IsNull)
            {
                var firstIter = patchClassSymbol.GetMembers()
                    .OfType<IMethodSymbol>()
                    .Where(it => it.Name == methodName).ToArray();

                if (firstIter.Length > 1)
                    throw new Exception("Arguments can't be null because multiple methods found with same name");

                if (firstIter.Length == 1)
                    method = firstIter[0];
            }
            
            if (method is null)
            {
                throw new Exception($"Failed to find method with name {methodName}");
            }
            
            StringBuilder interfaceCode = new();

            var attributes = classSymbol.GetAttributes().Select(it => it.AttributeClass?.Name ?? "").ToArray();

            var hasPostfixAttribute = attributes.Contains("AccordPostfixAttribute");

            var hasPrefixAttribute = attributes.Contains("AccordPrefixAttribute");
            
            if (hasPostfixAttribute || (!hasPrefixAttribute && !hasPostfixAttribute))
            {
                interfaceCode.Append($"void Postfix({GeneratePatchParameters(patchClassSymbol, method)});\n");
            }

            if (hasPrefixAttribute)
            {
                interfaceCode.Append($"bool Prefix({GeneratePatchParameters(patchClassSymbol, method)});\n");
            }
            
            // Build up the source code
            var code = $@"// <auto-generated/>
namespace {namespaceName} {{
    public partial interface I{className} : Accord.Common.Interfaces.IAccordPatch {{ 
        {interfaceCode}
    }}

    public partial class {className} : I{className} {{
        private void Patch() {{
            Accord.RuntimeManager.Instance.Patch(this);
        }}

        private void Unpatch() {{
            Accord.RuntimeManager.Instance.Unpatch(this);
        }}
    }}
}}
";

            // Add the source code to the compilation.
            context.AddSource($"{className}.g.cs", SourceText.From(code, Encoding.UTF8));
        }
    }
}