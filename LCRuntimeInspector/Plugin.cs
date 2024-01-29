using System.IO;
using System.Reflection;
using System.Security.Permissions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace LCRuntimeInspector
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    [BepInDependency("com.rune580.LethalCompanyInputUtils", "0.6.1")]
    public class Plugin : BaseUnityPlugin
    {
        public const string ModGUID = MyPluginInfo.PLUGIN_GUID;
        public const string ModName = MyPluginInfo.PLUGIN_NAME;
        public const string ModVersion = MyPluginInfo.PLUGIN_VERSION;

        public static ManualLogSource logger;
        public static ConfigFile config;


        public static PluginInfo pluginInfo;

        internal static Inputs Inputs;


        internal static AssetBundle bundle;
        internal static GameObject RuntimeInspectorPrefab;

        // runtime inspector assets
        public static GameObject tooltipAsset;
        public static GameObject objectReferencePickerAsset;
        public static GameObject draggedReferenceItemAsset;
        public static GameObject colorPickerAsset;

        // dynamic panel
        public static GameObject dynamicPanel;
        public static GameObject dynamicPanelPreview;
        public static GameObject dynamicPanelTab;

        public Plugin()
        {
            ShaderInspector.PreInit();
        }

        [RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Editor()
        {
            // if running in unity editor
            if (Application.isEditor)
            {
                Debug.Log("LethalThings Runtime Inspector Loaded!");

                ShaderInspector.PreInit();

                // load assets from resources instead of bundle
                RuntimeInspectorPrefab = Resources.Load<GameObject>("RuntimeInspectorPrefab");
                tooltipAsset = Resources.Load<GameObject>("RuntimeInspector/Tooltip");
                objectReferencePickerAsset = Resources.Load<GameObject>("RuntimeInspector/ObjectReferencePicker");
                draggedReferenceItemAsset = Resources.Load<GameObject>("RuntimeInspector/DraggedReferenceItem");
                colorPickerAsset = Resources.Load<GameObject>("RuntimeInspector/ColorPicker");

                dynamicPanel = Resources.Load<GameObject>("DynamicPanel");
                dynamicPanelPreview = Resources.Load<GameObject>("DynamicPanelPreview");
                dynamicPanelTab = Resources.Load<GameObject>("DynamicPanelTab");

                // print out the assets to see if they loaded correctly
                Debug.Log("RuntimeInspectorPrefab: " + RuntimeInspectorPrefab);
                Debug.Log("tooltipAsset: " + tooltipAsset);
                Debug.Log("objectReferencePickerAsset: " + objectReferencePickerAsset);
                Debug.Log("draggedReferenceItemAsset: " + draggedReferenceItemAsset);
                Debug.Log("colorPickerAsset: " + colorPickerAsset);

                Debug.Log("dynamicPanel: " + dynamicPanel);
                Debug.Log("dynamicPanelPreview: " + dynamicPanelPreview);
                Debug.Log("dynamicPanelTab: " + dynamicPanelTab);


            }
        }

        private void Awake()
        {
            Inputs = new Inputs();

            logger = Logger;
            config = Config;
            pluginInfo = Info;

            bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "bundles", "runtimeinspector"));

            tooltipAsset = bundle.LoadAsset<GameObject>("Assets/RuntimeInspector/Resources/RuntimeInspector/Tooltip.prefab");
            objectReferencePickerAsset = bundle.LoadAsset<GameObject>("Assets/RuntimeInspector/Resources/RuntimeInspector/ObjectReferencePicker.prefab");
            draggedReferenceItemAsset = bundle.LoadAsset<GameObject>("Assets/RuntimeInspector/Resources/RuntimeInspector/DraggedReferenceItem.prefab");
            colorPickerAsset = bundle.LoadAsset<GameObject>("Assets/RuntimeInspector/Resources/RuntimeInspector/ColorPicker.prefab");

            dynamicPanel = bundle.LoadAsset<GameObject>("Assets/DynamicPanels/Resources/DynamicPanel.prefab");
            dynamicPanelPreview = bundle.LoadAsset<GameObject>("Assets/DynamicPanels/Resources/DynamicPanelPreview.prefab");
            dynamicPanelTab = bundle.LoadAsset<GameObject>("Assets/DynamicPanels/Resources/DynamicPanelTab.prefab");

            RuntimeInspectorPrefab = bundle.LoadAsset<GameObject>("Assets/RuntimeInspector/Resources/RuntimeInspectorPrefab.prefab");

            Logger.LogInfo("LethalThings Runtime Inspector Loaded!");

            ShaderInspector.Init();

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
