using BepInEx;
using System.Security.Permissions;
using BepInEx.Logging;
using BepInEx.Configuration;
using UnityEngine;
using System.Reflection;
using System;
using static UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData;
using System.IO;
using RuntimeInspectorNamespace;
using LCRuntimeInspector.RuntimeInspector.RuntimeInspector;
using System.Collections.Generic;
using UnityEngine.Rendering;
using Mono.Cecil.Cil;
using MonoMod.Cil;

[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace LCRuntimeInspector
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    [BepInDependency("com.rune580.LethalCompanyInputUtils", "0.6.1")]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGUID = "evaisa.runtimeinspector";
        public const string ModName = MyPluginInfo.PLUGIN_NAME;
        public const string ModVersion = MyPluginInfo.PLUGIN_VERSION;

        public static ManualLogSource logger;
        public static ConfigFile config;


        public static PluginInfo pluginInfo;

        internal static Inputs Inputs = new Inputs();

        internal static AssetBundle bundle;
        internal static GameObject RuntimeInspectorPrefab;
        public static GameObject tooltipAsset;
        public static GameObject objectReferencePickerAsset;
        public static GameObject draggedReferenceItemAsset;
        public static GameObject colorPickerAsset;

        public Plugin()
        {
            ShaderInspector.PreInit();
        }

        private void Awake()
        {
            logger = Logger;
            config = Config;
            pluginInfo = Info;


            Logger.LogInfo("LethalThings Runtime Inspector Loaded!");

            bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "runtimeinspector"));

            tooltipAsset = bundle.LoadAsset<GameObject>("Assets/RuntimeInspector/Resources/RuntimeInspector/Tooltip.prefab");
            objectReferencePickerAsset = bundle.LoadAsset<GameObject>("Assets/RuntimeInspector/Resources/RuntimeInspector/ObjectReferencePicker.prefab");
            draggedReferenceItemAsset = bundle.LoadAsset<GameObject>("Assets/RuntimeInspector/Resources/RuntimeInspector/DraggedReferenceItem.prefab");
            colorPickerAsset = bundle.LoadAsset<GameObject>("Assets/RuntimeInspector/Resources/RuntimeInspector/ColorPicker.prefab");

            ShaderInspector.Init();

            RuntimeInspectorPrefab = bundle.LoadAsset<GameObject>("Assets/RuntimeInspector/RuntimeInspectorPrefab.prefab");

            var runtimeInspector = Instantiate(RuntimeInspectorPrefab);
            runtimeInspector.hideFlags = HideFlags.HideAndDontSave;

        }



        /*
        static bool first = true;

        private void RoundManager_Awake(On.RoundManager.orig_Awake orig, RoundManager self)
        {
            if (first)
            {
                var dungeon = self.dungeonFlowTypes[0];

                // clone the dungeon flow
                var newDungeon = Instantiate(dungeon);

                newDungeon.name = "LethalThingsDungeon";

                AudioClip audioClip = Content.MainAssets.LoadAsset<AudioClip>("Assets/Custom/LethalThings/brap.mp3");

                LethalLib.Modules.Dungeon.AddDungeon(newDungeon, 600, LethalLib.Modules.Levels.LevelTypes.All, audioClip);
            }
            orig(self);
        }*/
    }
}