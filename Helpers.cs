namespace UnityPeekPlugin
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;

	[Serializable]
	public class Helpers
	{
		public static HierachyStructure DeserializeHierarchy(byte[] data)
		{
			using (MemoryStream ms = new MemoryStream(data))
			using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
			{
				return ReadNode(reader);
			}
		}

		// https://discussions.unity.com/t/how-to-find-object-using-instance-id-taken-from-getinstanceid/11242/7
		public static UnityEngine.Object FindObjectFromInstanceID(int id)
		{
			return (UnityEngine.Object)typeof(UnityEngine.Object)
				.GetMethod("FindObjectFromInstanceID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
				.Invoke(null, new object[] { id });
		}

		public static HierachyStructure GetNode(string id, HierachyStructure rootNode)
		{
			if (rootNode == null)
			{
				return null;
			}

			// Convert id to string for comparison
			string idStr = id;

			// Use a queue for breadth-first search
			Queue<HierachyStructure> queue = new Queue<HierachyStructure>();
			queue.Enqueue(rootNode);

			while (queue.Count > 0)
			{
				var currentNode = queue.Dequeue();

				// Check all children of the current node
				foreach (var child in currentNode.Children)
				{
					if (child.ID == idStr)
					{
						return child;
					}

					queue.Enqueue(child);
				}
			}

			return null; // Node with the given id was not found
		}

		private static HierachyStructure ReadNode(BinaryReader reader)
		{
			HierachyStructure node = new Helpers.HierachyStructure
			{
				Name = reader.ReadString(),
				ID = reader.ReadString(),
				SiblingIndex = reader.ReadInt32(),
				Children = new List<Helpers.HierachyStructure>(),
			};

			int childCount = reader.ReadInt32(); // Read the number of children
			for (int i = 0; i < childCount; i++)
			{
				var child = ReadNode(reader);
				child.Parent = node; // Set parent reference
				node.Children.Add(child);
			}

			return node;
		}

		// Helper class
		[Serializable]
		public class HierachyStructure
		{
			public string Name;
			public string ID;
			public int SiblingIndex;
			public HierachyStructure Parent;
			public List<HierachyStructure> Children = new List<HierachyStructure>();
		}
	}
}
