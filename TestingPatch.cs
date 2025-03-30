using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityPeekPlugin
{
    [HarmonyPatch(typeof(SceneManager))]
    class TestingPatch
    {
        [HarmonyPatch(nameof(SceneManager.Internal_SceneLoaded))]
        [HarmonyPostfix]
        static void SceneLoaded_Postfix()
        {

            if(UnityEngine.Object.FindObjectOfType<UnityPeekController>())
            {
                Debug.LogError("UnityPeek Object already exists");
                return;
            }

            GameObject UnityPeekController = GameObject.CreatePrimitive(PrimitiveType.Cube);
            UnityPeekController.AddComponent<UnityPeekController>();
            


        }
    }
}
