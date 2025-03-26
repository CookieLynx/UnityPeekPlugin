using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityPeekPlugin
{
    class UnityPeekController : MonoBehaviour
    {
        void Start()
        {
            Debug.LogError("UnityPeek Object attached and running!");
        }

        void Awake()
        {
            DontDestroyOnLoad(this);
        }

        void Update()
        {
            transform.Rotate(Vector3.up * Time.deltaTime * 20f);
        }
    }
}
