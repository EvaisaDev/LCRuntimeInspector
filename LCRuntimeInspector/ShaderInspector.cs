using BepInEx;
using BepInEx.Configuration;
using LCRuntimeInspector.RuntimeInspector.RuntimeInspector;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace LCRuntimeInspector
{
    public class ShaderInspector
    {

        public static ConfigEntry<string> nameFormat { get; private set; }
        public static ConfigEntry<string> scaleFormat { get; private set; }
        public static ConfigEntry<string> offsetFormat { get; private set; }
        public static ConfigEntry<bool> hideRedundantProperties { get; private set; }
        public static ConfigEntry<bool> hidePerRendererData { get; private set; }

        public static HashSet<string> redundantPropertyNames = new HashSet<string> { "mainTexture", "mainTextureOffset", "mainTextureScale", "color" };
        public static Dictionary<Shader, ShaderPropertyInfo[]> shaderPropertiesCache = new Dictionary<Shader, ShaderPropertyInfo[]>();
        public static Stack<Material> targetMats = new Stack<Material>();


        public static ShaderPropertyInfo[] GetShaderPropertyInfos(Shader shader)
        {
            if (shaderPropertiesCache.TryGetValue(shader, out var value))
            {
                return value;
            }
            int propertyCount = shader.GetPropertyCount();
            List<ShaderPropertyInfo> list = new List<ShaderPropertyInfo>(propertyCount);
            for (int i = 0; i < propertyCount; i++)
            {
                ShaderPropertyInfo shaderPropertyInfo = new ShaderPropertyInfo(shader, i);
                list.Add(shaderPropertyInfo);
                if (shaderPropertyInfo.propertyType == MaterialPropertyType.Texture && !shaderPropertyInfo.HasFlag(ShaderPropertyFlags.NoScaleOffset))
                {
                    list.AddRange(ShaderPropertyInfo.TextureScaleOffset(shaderPropertyInfo));
                }
            }
            value = list.ToArray();
            shaderPropertiesCache.Add(shader, value);
            return value;
        }

        public static void PreInit()
        {
            HashSet<string> obj = new HashSet<string>();
            obj.Add("mainTexture");
            obj.Add("mainTextureOffset");
            obj.Add("mainTextureScale");
            obj.Add("color");
            redundantPropertyNames = obj;
            shaderPropertiesCache = new Dictionary<Shader, ShaderPropertyInfo[]>();
            targetMats = new Stack<Material>();
        }

        public static void Init()
        {


            nameFormat = LCRuntimeInspector.Plugin.config.Bind("ShaderInspector", "nameFormat", "{0}");
            scaleFormat = LCRuntimeInspector.Plugin.config.Bind("ShaderInspector", "scaleFormat", "{0}");
            offsetFormat = LCRuntimeInspector.Plugin.config.Bind("ShaderInspector", "offsetFormat", "{0}");
            hideRedundantProperties = LCRuntimeInspector.Plugin.config.Bind("ShaderInspector", "hideRedundantProperties", true);
            hidePerRendererData = LCRuntimeInspector.Plugin.config.Bind("ShaderInspector", "hidePerRendererData", true);

            nameFormat = Plugin.config.Bind<string>("MaterialInspector", "Display Name Format", "{0}", "Format string for shader property names. {0} is the descriptive external name and {1} is the internal identifier name.");
            scaleFormat = Plugin.config.Bind<string>("MaterialInspector", "Texture Tiling Format", "Tiling", "Format string for texture tiling properties. {0} is external name of the texture.");
            offsetFormat = Plugin.config.Bind<string>("MaterialInspector", "Texture Offset Format", "Offset", "Format string for texture offset properties. {0} is external name of the texture.");
            hideRedundantProperties = Plugin.config.Bind<bool>("MaterialInspector", "Hide Redundant Properties", true, "Hide the Main Texture and Color material properties that are already represented in the shader properties.");
            hidePerRendererData = Plugin.config.Bind<bool>("MaterialInspector", "Hide Per Renderer Data", true, "Hide properties that are usually managed per renderer.");
        }

    }
}
