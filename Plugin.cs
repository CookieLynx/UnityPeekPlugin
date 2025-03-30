using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.Mono;
using HarmonyLib;
using UnityEngine;

namespace UnityPeekPlugin;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    public static int counter = 0;

    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        ConfigManager.LoadConfig();


        Harmony Harmony = new Harmony("com.cookie.unitypeekplugin");
        Logger.LogInfo("About to patch harmony");
        Harmony.PatchAll();  // This will apply all Harmony patches in the assembly
        Logger.LogInfo("UnityPeek patch applied! WEEEE DID ITTTTT!!!!!");

       
        Logger.LogInfo("Loaded UnityPeek");
    }
}
