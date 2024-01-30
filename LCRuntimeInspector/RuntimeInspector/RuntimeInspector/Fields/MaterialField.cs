using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LCRuntimeInspector;
using UnityEngine;

namespace RuntimeInspectorNamespace;

public class MaterialField : ObjectField
{
    public override bool SupportsType(Type type)
    {
        return typeof(Material).IsAssignableFrom(type);
    }

    private static IEnumerable<MemberInfo> ShaderVariablesForMaterial(Material material)
        => ShaderInspector.GetShaderPropertyInfos(material.shader);

    private IEnumerable<MemberInfo> ShaderVariablesForValueMaterial
        => ShaderVariablesForMaterial(Value as Material);

    protected override IEnumerable<MemberInfo> ExposedVariablesForValueType
        => base.ExposedVariablesForValueType.Concat(ShaderVariablesForValueMaterial);
}
