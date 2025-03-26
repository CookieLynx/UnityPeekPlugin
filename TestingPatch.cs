using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace UnityPeekPlugin
{
    [HarmonyPatch(typeof(Debug))]
    class TestingPatch
    {
        [HarmonyPatch(nameof(Debug.Log), new Type[] { typeof(object) })]
        [HarmonyPostfix]
        static void Log_Postfix()
        {
            Debug.LogError("This is a test log message");

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
