﻿using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BlueprintTweaks.BlueprintBrowserUIChanges;
using CommonAPI;
using CommonAPI.Systems;
using HarmonyLib;
using NebulaAPI;
using UnityEngine;

[module: UnverifiableCode]
#pragma warning disable 618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore 618

namespace BlueprintTweaks
{
    [BepInPlugin(MODGUID, MOD_DISP_NAME, VERSION)]
    [BepInDependency(NebulaModAPI.API_GUID)]
    [BepInDependency(CommonAPIPlugin.GUID)]
    [CommonAPISubmoduleDependency(nameof(ProtoRegistry), nameof(CustomKeyBindSystem), nameof(PickerExtensionsSystem))]
    public class BlueprintTweaksPlugin : BaseUnityPlugin, IMultiplayerMod
    {
        public const string MODNAME = "BlueprintTweaks";
        
        public const string MODGUID = "org.kremnev8.plugin.BlueprintTweaks";
        
        public const string MOD_DISP_NAME = "Blueprint Tweaks";
        
        public const string VERSION = "1.3.1";

        public const string FREE_FOUNDATIONS_GUID = "de.Hotte.DSP.FreeFoundations";
        public const string FREE_FOUNDATIONS_GUID_2 = "com.aekoch.mods.dsp.UnlimitedFoundations";
        
        
        // Features keys

        public const string DRAG_REMOVE = "DragRemove";
        public const string BLUEPRINT_FOUNDATIONS = "BlueprintFoundations";

        public static ManualLogSource logger;
        public static ResourceData resource;

        public static DragRemoveBuildTool tool;

        public static bool freeFoundationsIsInstalled;

        public static ConfigEntry<bool> cameraToggleEnabled;
        public static ConfigEntry<bool> addPasteButtonEnabled;
        
        public static ConfigEntry<bool> recipeChangeEnabled;
        public static ConfigEntry<bool> changeTierEnabled;
        public static ConfigEntry<bool> logisticCargoChangeEnabled;
        public static ConfigEntry<bool> beltHintsChangeEnable;

        public static ConfigEntry<bool> forcePasteEnabled;
        public static ConfigEntry<bool> axisLockEnabled;
        public static ConfigEntry<bool> gridControlFeature;
        public static ConfigEntry<bool> blueprintMirroring;
        public static ConfigEntry<bool> dragRemove;
        
        public static ConfigEntry<bool> blueprintFoundations;
        
        public static ConfigEntry<bool> resetFunctionsOnMenuExit;
        public static ConfigEntry<bool> canBlueprintOnGasGiants;

        private void Awake()
        {
            logger = Logger;

            cameraToggleEnabled = Config.Bind("Interface", "cameraToggle", true, "Allows toggling camera between 3rd person and god view\nAll values are applied on restart");
            addPasteButtonEnabled = Config.Bind("Interface", "addBluprintPasteButton", true, "If enabled new button will be added to Blueprint Browser. Pressing it will paste curretly selected blueprint\nAll values are applied on restart");
            
            recipeChangeEnabled = Config.Bind("Interface", "recipeChange", true, "Add recipe change panel to blueprint inspectors\nAll values are applied on restart");
            logisticCargoChangeEnabled = Config.Bind("Interface", "changeLogisticCargo", true, "Allows changing cargo requested/provided by logistic stations");
            changeTierEnabled = Config.Bind("Interface", "changeTier", true, "Allows using change tier functionality\nAll values are applied on restart");
            beltHintsChangeEnable = Config.Bind("Interface", "beltHintChange", true, "Add belt hint change panel to blueprint inspectors\nAll values are applied on restart");

            
            forcePasteEnabled = Config.Bind("Features", "forcePaste", true, "Allows using key to force blueprint placement\nAll values are applied on restart");
            axisLockEnabled = Config.Bind("Features", "axisLock", true, "Allows using Latitude/Longtitude axis locks\nAll values are applied on restart");
            gridControlFeature = Config.Bind("Features", "gridControl", true, "Allows changing grid size and its offset\nAll values are applied on restart");
            blueprintMirroring = Config.Bind("Features", "blueprintMirroring", true, "Allows mirroring Blueprints\nAll values are applied on restart");
            dragRemove = Config.Bind("Features", "dragRemove", true, "Allows using drag remove function\nAll values are applied on restart");
            
            blueprintFoundations = Config.Bind("Features", "blueprintFoundations", true, "Allow blueprinting foundations along with buildings.\nAll values are applied on restart");
            
            resetFunctionsOnMenuExit = Config.Bind("Misc", "resetOnExit", true, "If enabled when you exit build mode, some functions (Axis/Grid lock, Mirror) will reset their state");
            canBlueprintOnGasGiants = Config.Bind("Misc", "bpOnGasGiants", true, "Allow using Blueprints on Gas Giants\nAll values are applied on restart");

            Config.MigrateConfig<bool>("General", "Interface", new []{"cameraToggle", "recipeChange", "changeLogisticCargo", "changeTier"});
            Config.MigrateConfig<bool>("General", "Features", new []{"forcePaste", "axisLock", "gridControl", "gridControl", "blueprintFoundations"});
            Config.MigrateConfig<bool>("General", "Misc", new []{"bpOnGasGiants"});
            
            Config.Save();
            
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(FREE_FOUNDATIONS_GUID))
            {
                freeFoundationsIsInstalled = true;
            }
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(FREE_FOUNDATIONS_GUID_2))
            {
                freeFoundationsIsInstalled = true;
            }
            
            string pluginfolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            resource = new ResourceData(MODNAME, "blueprinttweaks", pluginfolder);
            resource.LoadAssetBundle("blueprinttweaks");
            ProtoRegistry.AddResource(resource);
            
            Harmony harmony = new Harmony(MODGUID);

            #region Strings

            ProtoRegistry.RegisterString("KEYToggleBPGodModeDesc", "Toggle Blueprint God Mode", "切换上帝模式浏览蓝图");
            ProtoRegistry.RegisterString("RecipesLabel", "Recipes", "配方");
            ProtoRegistry.RegisterString("ChangeTipText", "Left-click to change recipe", "左键点击更改配方");
            
            ProtoRegistry.RegisterString("ChangeTipTitle", "Change Recipe", "更改配方");
            ProtoRegistry.RegisterString("ChangeTipDesc", 
                "Left-click to change recipe. When you click, picker menu will open, where a new recipe can be selected. All machines that used the old recipe will now use selected recipe. This change will take effect after saving.", 
                "左键点击更改配方。点击将打开选择菜单，可在其中选择新配方。所有使用旧配方的机器将更新到选定的新配方。此更改将在保存后生效。");
            ProtoRegistry.RegisterString("KEYForceBPPlace", "Force Blueprint placement", "强制蓝图放置");
            
            ProtoRegistry.RegisterString("KEYLockLongAxis", "Lock Longitude axis", "经度锁定");
            ProtoRegistry.RegisterString("KEYLockLatAxis", "Lock Latitude axis", "纬度锁定");
            ProtoRegistry.RegisterString("KEYSetLocalOffset", "Set grid snapping offset", "设定网格捕捉偏移");
            
            ProtoRegistry.RegisterString("GridSizeLabel", "Blueprint Size", "蓝图尺寸");
            ProtoRegistry.RegisterString("GridLongSize", "Width", "宽度");
            ProtoRegistry.RegisterString("GridLatSize", "Height", "高度");
            
            ProtoRegistry.RegisterString("CantPasteThisInGasGiantWarn", 
                "This Blueprint can't be pasted on a Gas Giant.", 
                "此蓝图无法放置在气态/冰巨星上。");
            
            ProtoRegistry.RegisterString("FoundationsLabel", "Foundations", "地基");
            ProtoRegistry.RegisterString("foundationsBPCountLabel", "recorded", "块地基");
            ProtoRegistry.RegisterString("foundationBPEnabledLabel", "Blueprint foundations", "蓝图包含地基");
            
            ProtoRegistry.RegisterString("TransportLabel", "Logistics", "物流");
            ProtoRegistry.RegisterString("ChangeTipText2", "Left-click to change requested item", "左键点击更改物流清单物品");
            ProtoRegistry.RegisterString("ChangeTip2Title", "Change requested items", "更改物流清单物品");
            ProtoRegistry.RegisterString("ChangeTip2Desc", 
                "Left-click to change requested item. When you click, picker menu will open, where a new item can be selected. Logistic station that used the old item will now use selected item. This change will take effect after saving.", 
                "左键点击更改物流清单物品。点击将打开选择菜单，可在其中选择新的物流清单物品。使用旧物流清单物品的物流塔将更新到选定的新物流清单物品。此更改将在保存后生效。");

            
            ProtoRegistry.RegisterString("copyColorsLabel", "Copy Custom foundation colors", "附带自定义调色板");
            ProtoRegistry.RegisterString("copyColorsTip", "Copy Custom foundation colors", "附带自定义调色板");
            ProtoRegistry.RegisterString("copyColorsTipDesc", 
                "When enabled, Custom foundation colors will be saved with Blueprint Data. When such Blueprint will be pasted, current planet's Custom colors will be replaced with colors stored in the Blueprint",
                "启用后，地基的自定义调色板将与蓝图数据一同保存。粘贴此类蓝图时，当前行星的地基自定义调色板将被蓝图中的调色板替代。");
            
            ProtoRegistry.RegisterString("hasColorsLabel", "Contains Color data", "含有颜色数据");
            
            ProtoRegistry.RegisterString("foundationsBlueprintTip", "Blueprint Foundations", "蓝图包含地基");
            ProtoRegistry.RegisterString("foundationsBlueprintTipDesc", 
                "When enabled, all Foundations (Including their colors and types) in your selection will be saved to the Blueprint. If there are buildings that lack support, but blueprint has foundations under them they will successfully be pasted",
                "启用后，您选中的所有地基（包括它们的颜色和类型）都将保存到蓝图中。另外只要蓝图中的建筑下方包含地基，即便施工场地缺乏地基支撑，蓝图也能成功粘贴");

            ProtoRegistry.RegisterString("KEYMirrorLongAxis", "Mirror Blueprint in Longitude axis", "经向镜像");
            ProtoRegistry.RegisterString("KEYMirrorLatAxis", "Mirror Blueprint in Latitude axis", "纬向镜像");
            
            
            ProtoRegistry.RegisterString("BeltHintsLabel", "Belt Hints", "腰带提示");
            ProtoRegistry.RegisterString("HintsChangeTipText", "Left-click to change belt hints", "左键单击更改腰带提示");
            
            ProtoRegistry.RegisterString("ChangeHintsTipTitle", "You can change belt hints", "您可以更改腰带提示");
            ProtoRegistry.RegisterString("ChangeHintsTipDesc", 
                "Left-click to change hints on belts. When you click, picker menu will open, where a new icon can be selected. All belts that used the old icon will now use selected icon. This change will take effect after saving.", 
                "左键点击更改腰带提示。点击将打开选择菜单，可在其中选择新腰带提示。所有使用旧腰带提示的输送带将更新到选定的新腰带提示。此更改将在保存后生效。");
            
            #endregion
            
            UIBlueprintInspectorPatch.Init();
            BlueprintUtilsPatch2.Init();
            RegisterKeyBinds();

            NebulaModAPI.RegisterPackets(Assembly.GetExecutingAssembly());

            if (addPasteButtonEnabled.Value)
            {
                harmony.PatchAll(typeof(UIBlueprintBrowserPatch));
            }

            if (blueprintMirroring.Value)
            {
                harmony.PatchAll(typeof(BlueprintUtilsPatch2));
            }

            if (dragRemove.Value)
            {
                harmony.PatchAll(DRAG_REMOVE);
            }

            if (blueprintFoundations.Value)
            {
                harmony.PatchAll(BLUEPRINT_FOUNDATIONS);
            }

            if (changeTierEnabled.Value)
                harmony.PatchAll(typeof(UIBlueprintComponentItemPatch));
            
            if (canBlueprintOnGasGiants.Value)
            {
                harmony.PatchAll(typeof(PlayerControllerPatch));
                harmony.PatchAll(typeof(BuildTool_BlueprintPastePatch));
            }

            if (axisLockEnabled.Value || gridControlFeature.Value)
                harmony.PatchAll(typeof(GridSnappingPatches));
            if (cameraToggleEnabled.Value)
                harmony.PatchAll(typeof(CameraFixPatch));
            if (recipeChangeEnabled.Value || gridControlFeature.Value)
                harmony.PatchAll(typeof(UIBlueprintInspectorPatch));
            if (forcePasteEnabled.Value)
            {
                harmony.PatchAll(typeof(BlueprintPastePatch));
            }

            logger.LogInfo("Blueprint tweaks mod is initialized!");
        }

        private static void RegisterKeyBinds()
        {
            if (cameraToggleEnabled.Value)
            {
                CustomKeyBindSystem.RegisterKeyBind<PressKeyBind>(new BuiltinKey
                {
                    id = 100,
                    key = new CombineKey((int) KeyCode.J, 0, ECombineKeyAction.OnceClick, false),
                    conflictGroup = 3071,
                    name = "ToggleBPGodModeDesc",
                    canOverride = true
                });
            }

            if (forcePasteEnabled.Value)
            {
                CustomKeyBindSystem.RegisterKeyBind<HoldKeyBind>(new BuiltinKey
                {
                    id = 101,
                    key = new CombineKey(0, 1, ECombineKeyAction.OnceClick, false),
                    conflictGroup = 2052,
                    name = "ForceBPPlace",
                    canOverride = true
                });
            }

            if (axisLockEnabled.Value)
            {
                CustomKeyBindSystem.RegisterKeyBind<ReleaseKeyBind>(new BuiltinKey
                {
                    id = 102,
                    key = new CombineKey((int) KeyCode.G, CombineKey.CTRL_COMB, ECombineKeyAction.OnceClick, false),
                    conflictGroup = 2052,
                    name = "LockLongAxis",
                    canOverride = true
                });

                CustomKeyBindSystem.RegisterKeyBind<ReleaseKeyBind>(new BuiltinKey
                {
                    id = 103,
                    key = new CombineKey((int) KeyCode.T, CombineKey.CTRL_COMB, ECombineKeyAction.OnceClick, false),
                    conflictGroup = 2052,
                    name = "LockLatAxis",
                    canOverride = true
                });
            }

            if (gridControlFeature.Value)
            {
                CustomKeyBindSystem.RegisterKeyBind<ReleaseKeyBind>(new BuiltinKey
                {
                    id = 104,
                    key = new CombineKey((int) KeyCode.B, CombineKey.CTRL_COMB, ECombineKeyAction.OnceClick, false),
                    conflictGroup = 2052,
                    name = "SetLocalOffset",
                    canOverride = true
                });
            }
            
            if (blueprintMirroring.Value)
            {
                CustomKeyBindSystem.RegisterKeyBind<ReleaseKeyBind>(new BuiltinKey
                {
                    id = 105,
                    key = new CombineKey((int) KeyCode.G, CombineKey.SHIFT_COMB, ECombineKeyAction.OnceClick, false),
                    conflictGroup = 2052,
                    name = "MirrorLongAxis",
                    canOverride = true
                });

                CustomKeyBindSystem.RegisterKeyBind<ReleaseKeyBind>(new BuiltinKey
                {
                    id = 106,
                    key = new CombineKey((int) KeyCode.T, CombineKey.SHIFT_COMB, ECombineKeyAction.OnceClick, false),
                    conflictGroup = 2052,
                    name = "MirrorLatAxis",
                    canOverride = true
                });
            }
        }

        private void Update()
        {
            if (cameraToggleEnabled.Value && CustomKeyBindSystem.GetKeyBind("ToggleBPGodModeDesc").keyValue)
            {
                CameraFixPatch.mode = !CameraFixPatch.mode;
            }

            if (axisLockEnabled.Value && CustomKeyBindSystem.GetKeyBind("LockLongAxis").keyValue)
            {
                GridSnappingPatches.LockLongitude();
            }
            
            if (axisLockEnabled.Value &&  CustomKeyBindSystem.GetKeyBind("LockLatAxis").keyValue)
            {
                GridSnappingPatches.LockLatitude();
            }
            
            if (gridControlFeature.Value && CustomKeyBindSystem.GetKeyBind("SetLocalOffset").keyValue)
            {
                GridSnappingPatches.SetOffset();
            }

            if (forcePasteEnabled.Value)
            {
                BlueprintPastePatch.isEnabled = CustomKeyBindSystem.GetKeyBind("ForceBPPlace").keyValue;
            }
            
            if (blueprintMirroring.Value && CustomKeyBindSystem.GetKeyBind("MirrorLongAxis").keyValue)
            {
                BlueprintUtilsPatch2.mirrorLong = !BlueprintUtilsPatch2.mirrorLong;
                BlueprintUtilsPatch2.UpdateBlueprintDisplay();
            }
            
            if (blueprintMirroring.Value && CustomKeyBindSystem.GetKeyBind("MirrorLatAxis").keyValue)
            {
                BlueprintUtilsPatch2.mirrorLat = !BlueprintUtilsPatch2.mirrorLat;
                BlueprintUtilsPatch2.UpdateBlueprintDisplay();
            }
            
        }

        public bool CheckVersion(string hostVersion, string clientVersion)
        {
            return hostVersion.Equals(clientVersion);
        }

        public string Version => VERSION;
    }
}