using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Code.com.sfbgames.crowcountry;
using com.sfbgames.crowcountry;
using com.sfbgames.playmaker;
using HarmonyLib;
using UnityEngine;

namespace CrowCountryResolutionMod
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<int> _width;
        public static ConfigEntry<int> _height;
        public static ConfigEntry<float> _fov;
        public static ConfigEntry<bool> _enableSFBFilter;
        public static ConfigEntry<bool> _enablePSXFilter;
        public static ConfigEntry<bool> _enableCRTBlurFilter;
        public static ConfigEntry<bool> _enableCRTPostFilter;
        public static ConfigEntry<int> _internalResMultiplier;
        public static ConfigEntry<bool> _disableAllFilters;
        public static ManualLogSource _logger;
        private void Awake()
        {
            _width = Config.Bind<int>("1. Resolution", "Width", 1920, "");
            _height = Config.Bind<int>("1. Resolution", "Height", 1080, "");
            _fov = Config.Bind<float>("1. Resolution", "Fov", 10.1f, "Amount to set to Fov. For ultrawide, try around 15-20");
            _enableSFBFilter = Config.Bind<bool>("2. Filters", "Enable SFB Filter", true, "");
            _enablePSXFilter = Config.Bind<bool>("2. Filters", "Enable PSX Filter", true, "");
            _enableCRTBlurFilter = Config.Bind<bool>("2. Filters", "Enable CRT Blur Filter", true, "");
            _enableCRTPostFilter = Config.Bind<bool>("2. Filters", "Enable CRT Post Filter", true, "");
            _internalResMultiplier = Config.Bind<int>("2. Filters", "Internal Resolution Multiplier", 1, "Modifies the psx effect to use higher resolution buffer. 12 seems to be the max.");
            _disableAllFilters = Config.Bind<bool>("2. Filters", "Disable All Filters", false, "Completely disables the Cam Effect (beyond 4 filters above). Overrides previously set filters");
            _logger = Logger;
            // Plugin startup logic
            Harmony.CreateAndPatchAll(typeof(CrowCountryPatch));
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }
    }

    public class CrowCountryPatch
    {
        [HarmonyPatch(typeof(PMFullscreen), "SetResolution")]
        [HarmonyPostfix]
        private static void ResolutionPostfix(FullScreenMode fullscreenMode)
        {
            Plugin._logger.LogInfo("Changing Resolution");
            Screen.SetResolution(Plugin._width.Value, Plugin._height.Value, fullscreenMode);
        }

        [HarmonyPatch(typeof(CharacterAndCamera), "Activate")]
        [HarmonyPostfix]
        private static void FovPostfix(CharacterAndCamera __instance)
        {
            if (Plugin._fov.Value != 10.1f)
            {
                var mainCamera = Camera.main;
                mainCamera.fieldOfView = Plugin._fov.Value;

                //Disable poison frame because its visible if fov is increased when it shouldn't be. Find a better fix later
                GameObject gameObject = GameObject.Find("poison frame");
                if (gameObject != null)
                {
                    gameObject.gameObject.SetActive(false);
                }
            }
        }

        [HarmonyPatch(typeof(CrowCountryCamEffect), "Awake")]
        [HarmonyPrefix]
        private static bool FilterAwakePrefix(CrowCountryCamEffect __instance)
        {
            Plugin._logger.LogInfo("Setting Filters");
            __instance.enableCRTBlurFilter = Plugin._enableCRTBlurFilter.Value;
            __instance.enableCRTPostFilter = Plugin._enableCRTPostFilter.Value;
            __instance.enablePSXFilter = Plugin._enablePSXFilter.Value;
            __instance.enableSFBFilter = Plugin._enableSFBFilter.Value;
            return true;
        }

        [HarmonyPatch(typeof(CrowCountryCamEffect), "Awake")]
        [HarmonyPostfix]
        private static void FilterPostfix(CrowCountryCamEffect __instance, ref RenderTexture ___psxRenderTexture, ref RenderTexture ___fbBuffer, ref RenderTexture ___camRenderTexture1)
        {
            __instance.enabled = !Plugin._disableAllFilters.Value;

            var multiplier = Plugin._internalResMultiplier.Value;
            ___psxRenderTexture = new RenderTexture(550 * multiplier, 400 * multiplier, 16, RenderTextureFormat.ARGB32);
            ___psxRenderTexture.filterMode = FilterMode.Point;

            ___fbBuffer = new RenderTexture(416* multiplier, 234* multiplier, 0);
            ___fbBuffer.filterMode = FilterMode.Point;

            ___camRenderTexture1 = new RenderTexture(1248* multiplier, 702* multiplier, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        }
    }
}
