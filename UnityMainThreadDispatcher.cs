using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityPeekPlugin
{

    //Main Thread dispacher https://github.com/PimDeWitte/UnityMainThreadDispatcher
    //Used for calling Unity functions from other threads (like the network thread)

    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static readonly Queue<Action> _executionQueue = new Queue<Action>();

        private static UnityMainThreadDispatcher _instance;

        public static UnityMainThreadDispatcher Instance()
        {
            if (_instance == null)
            {
                var obj = new GameObject("MainThreadDispatcher");
                _instance = obj.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(obj);
            }
            return _instance;
        }

        public void Enqueue(Action action)
        {
            lock (_executionQueue)
            {
                _executionQueue.Enqueue(action);
            }
        }

        void Update()
        {
            while (_executionQueue.Count > 0)
            {
                Action action;
                lock (_executionQueue)
                {
                    action = _executionQueue.Dequeue();
                }
                action?.Invoke();
            }
        }
    }
}
