using System;
using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;

namespace Accord.Transpiler.Interfaces;

public enum CilMatch
{
    Start,
    End,
    Strict,
    None
}

public interface IAccordTranspiler
{
    public IEnumerable<Func<CilInstruction, CilMatch>> Match();
    public IEnumerable<CilInstruction> Modify(IEnumerable<CilInstruction> instructions, TypeDefinition definition, IMethodDefOrRef getInstance, ReferenceImporter importer);
}

public interface IAccordTranspilerInstance
{
    public IAccordTranspiler[] TranspilerList { get; }
}