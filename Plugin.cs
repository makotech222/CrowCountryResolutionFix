using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Code.com.sfbgames.crowcountry;
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
        public static ManualLogSource _logger;
        private void Awake()
        {
            _width = Config.Bind<int>("Resolution", "Width", 1920, "");
            _height = Config.Bind<int>("Resolution", "Height", 1080, "");
            _fov = Config.Bind<float>("Resolution", "Fov", 10.1f, "Amount to set to Fov. For ultrawide, try around 15-20");
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
            if (Plugin._fov.Value != 0)
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
    }
}
