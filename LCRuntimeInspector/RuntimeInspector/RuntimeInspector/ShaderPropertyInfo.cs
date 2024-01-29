using System;
using System.Collections.Generic;
using System.Reflection;
using RuntimeInspectorNamespace;
using UnityEngine;
using UnityEngine.Rendering;

namespace LCRuntimeInspector.RuntimeInspector.RuntimeInspector
{
    public class ShaderPropertyInfo : MemberInfo
    {
        public override Type DeclaringType => typeof(Material);

        public override MemberTypes MemberType => MemberTypes.Custom;

        public override string Name => name;

        public override Type ReflectedType => typeof(Material);

        public override object[] GetCustomAttributes(bool inherit)
        {
            if (isRange)
            {
                return new RangeAttribute[1] { rangeAttribute };
            }
            return Array.Empty<object>();
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (isRange && attributeType == typeof(RangeAttribute))
            {
                return new RangeAttribute[1] { rangeAttribute };
            }
            return Array.Empty<object>();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            if (isRange && attributeType == typeof(RangeAttribute))
            {
                return true;
            }
            return false;
        }

        public ShaderPropertyInfo(Shader shader, int propertyIndex)
        {
            name = shader.GetPropertyName(propertyIndex);
            description = shader.GetPropertyDescription(propertyIndex);
            if ((propertyType = (MaterialPropertyType)shader.GetPropertyType(propertyIndex)) == (MaterialPropertyType)3)
            {
                propertyType = MaterialPropertyType.Float;
                Vector2 propertyRangeLimits = shader.GetPropertyRangeLimits(propertyIndex);
                rangeAttribute = new RangeAttribute(propertyRangeLimits.x, propertyRangeLimits.y);
            }
            flags = shader.GetPropertyFlags(propertyIndex);
            string[] propertyAttributes = shader.GetPropertyAttributes(propertyIndex);
            for (int i = 0; i < propertyAttributes.Length; i++)
            {
                HandleAttribute(this, propertyAttributes[i]);
            }
            if (propertyType != MaterialPropertyType.Enum && name.IndexOf("Blend", StringComparison.OrdinalIgnoreCase) >= 0 && name.IndexOf("Internal", StringComparison.OrdinalIgnoreCase) < 0)
            {
                enumType = typeof(BlendMode);
                propertyType = MaterialPropertyType.Enum;
                flags &= ~ShaderPropertyFlags.HideInInspector;
            }
        }

        private ShaderPropertyInfo()
        {
        }

        public static IEnumerable<ShaderPropertyInfo> TextureScaleOffset(ShaderPropertyInfo textureProperty)
        {
            yield return new ShaderPropertyInfo
            {
                name = textureProperty.name,
                description = string.Format(ShaderInspector.scaleFormat.Value, textureProperty.description),
                propertyType = MaterialPropertyType.TextureScale
            };
            yield return new ShaderPropertyInfo
            {
                name = textureProperty.name,
                description = string.Format(ShaderInspector.offsetFormat.Value, textureProperty.description),
                propertyType = MaterialPropertyType.TextureOffset
            };
        }

        public static void HandleAttribute(ShaderPropertyInfo prop, string attribute)
        {
            //Plugin.logger.LogInfo($"HandleAttribute: {prop.name} {attribute}");
            if (TryGetShaderAttribute(attribute, "Toggle", out var substring))
            {
                prop.toggleKeyword = substring;
                prop.propertyType = MaterialPropertyType.Toggle;
            }
            else
            {
                if (!TryGetShaderAttribute(attribute, "MaterialEnum", out var substring2) && !TryGetShaderAttribute(attribute, "Enum", out substring2))
                {
                    return;
                }
                string[] array = substring2.Split(new char[1] { ',' });
                if (array.Length == 1)
                {
                    Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    for (int i = 0; i < assemblies.Length; i++)
                    {
                        Type type = assemblies[i].GetType(array[0]);
                        if (type != null)
                        {
                            prop.enumType = type;
                            prop.propertyType = MaterialPropertyType.Enum;
                            break;
                        }
                    }
                }
                else
                {
                    if (array.Length < 2)
                    {
                        return;
                    }
                    List<string> list = new List<string>();
                    List<object> list2 = new List<object>();
                    for (int j = 1; j < array.Length; j += 2)
                    {
                        if (int.TryParse(array[j], out var result))
                        {
                            list.Add(array[j - 1]);
                            list2.Add((MaterialEnum)result);
                        }
                    }
                    prop.explicitEnumNames = list.ToArray();
                    prop.explicitEnumValues = list2.ToArray();
                    prop.propertyType = MaterialPropertyType.Enum;
                }
            }
        }

        public static bool TryGetShaderAttribute(string attributeString, string name, out string substring)
        {
            if (attributeString.Trim().StartsWith(name))
            {
                int num = attributeString.IndexOf('(');
                int num2 = attributeString.IndexOf(')');
                if (num >= 0 && num2 >= 0)
                {
                    substring = attributeString.Substring(num + 1, num2 - num - 1);
                    return true;
                }
            }
            substring = null;
            return false;
        }

        public static void SetToggleKeyword(Material mat, string keyword, string name, bool enabled)
        {
            if ((enabled ? 1U : 0U) != (mat.IsKeywordEnabled(keyword) ? 1U : 0U))
            {
                if (enabled)
                {
                    mat.EnableKeyword(keyword);
                    mat.SetFloat(name, 1f);
                }
                else
                {
                    mat.DisableKeyword(keyword);
                    mat.SetFloat(name, 0f);
                }
            }
        }

        public Type GetPropertyType()
        {
            switch (propertyType)
            {
                case MaterialPropertyType.Color:
                    return typeof(Color);
                case MaterialPropertyType.Vector4:
                    return typeof(Vector4);
                case MaterialPropertyType.Float:
                    return typeof(float);
                case MaterialPropertyType.Texture:
                    return typeof(Texture);
                case MaterialPropertyType.TextureScale:
                case MaterialPropertyType.TextureOffset:
                    return typeof(Vector2);
                case MaterialPropertyType.Toggle:
                    return typeof(bool);
                case MaterialPropertyType.Enum:
                    return enumType ?? typeof(MaterialEnum);
                default:
                    return null;
            }
        }

        public InspectorField.Getter GetGetter(InspectorField parent)
        {
            switch (propertyType)
            {
                case MaterialPropertyType.Color:
                    return () => (parent.Value is Material material) ? material.GetColor(name) : default(Color);
                case MaterialPropertyType.Vector4:
                    return () => (parent.Value is Material material2) ? material2.GetVector(name) : default(Vector4);
                case MaterialPropertyType.Float:
                    return () => (parent.Value is Material material3) ? material3.GetFloat(name) : 0f;
                case MaterialPropertyType.Texture:
                    return () => (parent.Value is Material material4) ? material4.GetTexture(name) : null;
                case MaterialPropertyType.TextureScale:
                    return () => (parent.Value is Material material5) ? material5.GetTextureScale(name) : default(Vector2);
                case MaterialPropertyType.TextureOffset:
                    return () => (parent.Value is Material material6) ? material6.GetTextureOffset(name) : default(Vector2);
                case MaterialPropertyType.Toggle:
                    return () => ((parent.Value is Material material7 && material7.IsKeywordEnabled(toggleKeyword)) ? ((byte)1) : ((byte)0)) != 0;
                case MaterialPropertyType.Enum:
                    return () => (parent.Value is Material material8) ? Enum.ToObject(enumType ?? typeof(MaterialEnum), material8.GetInt(name)) : ((object)0);
                default:
                    return null;
            }
        }

        public InspectorField.Setter GetSetter(InspectorField parent)
        {
            switch (propertyType)
            {
                case MaterialPropertyType.Color:
                    return delegate (object value)
                    {
                        if (parent.Value is Material material)
                        {
                            material.SetColor(name, (Color)(object)value);
                        }
                    };
                case MaterialPropertyType.Vector4:
                    return delegate (object value)
                    {
                        if (parent.Value is Material material2)
                        {
                            material2.SetVector(name, (Vector4)(object)value);
                        }
                    };
                case MaterialPropertyType.Float:
                    return delegate (object value)
                    {
                        if (parent.Value is Material material3)
                        {
                            material3.SetFloat(name, (float)(object)value);
                        }
                    };
                case MaterialPropertyType.Texture:
                    return delegate (object value)
                    {
                        if (parent.Value is Material material4)
                        {
                            material4.SetTexture(name, (Texture)value);
                        }
                    };
                case MaterialPropertyType.TextureScale:
                    return delegate (object value)
                    {
                        if (parent.Value is Material material5)
                        {
                            material5.SetTextureScale(name, (Vector2)(object)value);
                        }
                    };
                case MaterialPropertyType.TextureOffset:
                    return delegate (object value)
                    {
                        if (parent.Value is Material material6)
                        {
                            material6.SetTextureOffset(name, (Vector2)(object)value);
                        }
                    };
                case MaterialPropertyType.Toggle:
                    return delegate (object value)
                    {
                        if (parent.Value is Material mat)
                        {
                            SetToggleKeyword(mat, toggleKeyword, name, (bool)(object)value);
                        }
                    };
                case MaterialPropertyType.Enum:
                    return delegate (object value)
                    {
                        if (parent.Value is Material material7)
                        {
                            material7.SetInt(name, (int)(object)value);
                        }
                    };
                default:
                    return null;
            }
        }

        public bool HasFlag(ShaderPropertyFlags flag)
        {
            return (((flags & flag) > ShaderPropertyFlags.None) ? ((byte)1) : ((byte)0)) != 0;
        }

        public bool isHidden => ((HasFlag(ShaderPropertyFlags.HideInInspector) || (ShaderInspector.hidePerRendererData.Value && HasFlag(ShaderPropertyFlags.PerRendererData))) ? ((byte)1) : ((byte)0)) != 0;

        public bool isRange => ((rangeAttribute != null) ? ((byte)1) : ((byte)0)) != 0;

        public string description;

        public string name;

        public MaterialPropertyType propertyType;

        public ShaderPropertyFlags flags;

        public RangeAttribute rangeAttribute = null;

        public string toggleKeyword = null;

        public Type enumType = null;

        public string[] explicitEnumNames = null;

        public object[] explicitEnumValues = null;

        public enum MaterialEnum
        {

        }
    }
}
