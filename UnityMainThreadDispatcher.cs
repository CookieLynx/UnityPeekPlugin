namespace UnityPeekPlugin
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;

	// Main Thread dispacher https://github.com/PimDeWitte/UnityMainThreadDispatcher
	// Used for calling Unity functions from other threads (like the network thread)
	public class UnityMainThreadDispatcher : MonoBehaviour
	{
		private static readonly Queue<Action> ExecutionQueue = new Queue<Action>();

		private static UnityMainThreadDispatcher instance;

		public static UnityMainThreadDispatcher Instance()
		{
			if (instance == null)
			{
				var obj = new GameObject("MainThreadDispatcher");
				instance = obj.AddComponent<UnityMainThreadDispatcher>();
				DontDestroyOnLoad(obj);
			}

			return instance;
		}

		public void Enqueue(Action action)
		{
			lock (ExecutionQueue)
			{
				ExecutionQueue.Enqueue(action);
			}
		}

		public void Update()
		{
			while (ExecutionQueue.Count > 0)
			{
				Action action;
				lock (ExecutionQueue)
				{
					action = ExecutionQueue.Dequeue();
				}

				action?.Invoke();
			}
		}
	}
}
