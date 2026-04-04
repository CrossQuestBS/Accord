using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CrossAccord.Common.Attributes;
using CrossAccord.Common.Interfaces;

namespace CrossAccord;

public class RuntimeManager
{
    private readonly List<IAccordPatcher> Patchers = new();

    public static RuntimeManager Instance { get; } = new();

    private RuntimeManager()
    {
        var interfacePatcher = typeof(IAccordPatcher);

        var generatedAssembly =
            Assembly.Load("CrossAccord.Generated, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");

        var types = generatedAssembly.GetTypes()
            .Where(interfacePatcher.IsAssignableFrom);

        foreach (var patcherType in types)
        {
            var getInstance = patcherType.GetMethod("get_Instance", (global::System.Reflection.BindingFlags)~0);
            object[] array = [];

            if (getInstance == null)
                continue;

            var patcherInstance = (IAccordPatcher)getInstance.Invoke(null, array);

            if (patcherInstance is not null)
                Patchers.Add(patcherInstance);
        }
    }

    public void Patch(IAccordPatch patch)
    {
        foreach (var patcher in Patchers)
        {
            var attribute = patch.GetType().GetCustomAttribute<AccordPatchAttribute>(true);
            
            if (patcher.MethodName != attribute.MethodName)
                continue;
            
            if (patcher.MethodType != attribute.DeclaringType)
                continue;
            
            if (!patcher.Arguments.SequenceEqual(attribute.Arguments))
                continue;
            
            patcher.Patch(patch);
        }
    }

    public void Unpatch(IAccordPatch patch)
    {
        foreach (var patcher in Patchers)
        {
            var attribute = patch.GetType().GetCustomAttribute<AccordPatchAttribute>(true);
            
            if (patcher.MethodName != attribute.MethodName)
                continue;
            
            if (patcher.MethodType != attribute.DeclaringType)
                continue;
            
            if (!patcher.Arguments.SequenceEqual(attribute.Arguments))
                continue;
            
            patcher.Unpatch(patch);
        }
    }
}