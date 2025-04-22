namespace UnityPeekPlugin
{
	using HarmonyLib;
	using UnityEngine;
	using UnityEngine.SceneManagement;
	using UnityPeekPlugin.GameObjects;

	[HarmonyPatch(typeof(SceneManager))]
	public class UnityPeekAdderPatch
	{
		// On scene loaded, add the UnityPeekController if it does not exist already
		[HarmonyPatch(nameof(SceneManager.Internal_SceneLoaded))]
		[HarmonyPostfix]
		public static void SceneLoaded_Postfix()
		{
			if (UnityEngine.Object.FindObjectOfType<UnityPeekController>())
			{
				Debug.LogError("UnityPeek Object already exists");
				return;
			}

			// Give it a temp cube to see it in the scene
			GameObject unityPeekController = GameObject.CreatePrimitive(PrimitiveType.Cube);
			UnityPeekController controller = unityPeekController.AddComponent<UnityPeekController>();
			UnityPeekNetworking networking = unityPeekController.AddComponent<UnityPeekNetworking>();

			controller.SetUnityPeekNetworking(networking);
			networking.SetUnityPeekController(controller);

			unityPeekController.name = "UnityPeekController";
		}
	}
}
