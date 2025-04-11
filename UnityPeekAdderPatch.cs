using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityPeekPlugin.GameObjects;

namespace UnityPeekPlugin
{
	[HarmonyPatch(typeof(SceneManager))]
	class UnityPeekAdderPatch
	{
		//On scene loaded, add the UnityPeekController if it does not exist already
		[HarmonyPatch(nameof(SceneManager.Internal_SceneLoaded))]
		[HarmonyPostfix]
		static void SceneLoaded_Postfix()
		{

			if (UnityEngine.Object.FindObjectOfType<UnityPeekController>())
			{
				Debug.LogError("UnityPeek Object already exists");
				return;
			}

			//Give it a temp cube to see it in the scene
			GameObject UnityPeekController = GameObject.CreatePrimitive(PrimitiveType.Cube);
			UnityPeekController controller = UnityPeekController.AddComponent<UnityPeekController>();
			UnityPeekNetworking networking = UnityPeekController.AddComponent<UnityPeekNetworking>();

			controller.SetUnityPeekNetworking(networking);
			networking.SetUnityPeekController(controller);


			UnityPeekController.name = "UnityPeekController";



		}
	}
}
