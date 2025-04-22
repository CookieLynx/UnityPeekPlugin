namespace UnityPeekPlugin
{
    using System;
    using BepInEx;
    using BepInEx.Bootstrap;
    using BepInEx.Logging;
    using BepInEx.Unity.Mono;
    using HarmonyLib;

	[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
	public class Plugin : BaseUnityPlugin
	{
		internal static new ManualLogSource Logger;

		private void Awake()
		{
			// Plugin startup logic
			Logger = base.Logger;

			var path = @"BepInEx/core/BepInEx.Core.dll";
			var version = System.Diagnostics.FileVersionInfo.GetVersionInfo(path).FileVersion;
			Console.WriteLine("BepInEx version: " + version);

			// if the version is less then 6.0.0.0 log an error
			if (Version.TryParse(version, out var parsedVersion) && parsedVersion < new Version(6, 0, 0, 0))
			{
				Logger.LogError("BepInEx version is less than 6.0.0.0. Please update to 6.0.0.0. or greater");
				throw new InvalidOperationException("Unsupported BepInEx version. Plugin cannot be loaded.");
			}

			Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

			ConfigManager.LoadConfig();

			Harmony harmony = new Harmony("com.UnityPeekPlugin.UnityPeekPlugin");
			Logger.LogInfo("About to patch Harmony");
			harmony.PatchAll();  // This will apply all Harmony patches in the assembly
			Logger.LogInfo("UnityPeek patch applied! Welcome to UnityPeek");
			Logger.LogInfo("Loaded UnityPeek");
		}
	}
}
