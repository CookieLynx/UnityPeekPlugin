namespace UnityPeekPlugin.GameObjects
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;
	using UnityEngine;
	using UnityEngine.SceneManagement;

	// This call handles the connection of serial data between the UnityPeek and the UnityPeekPlugin
	public class UnityPeekController : MonoBehaviour
	{
		public UnityPeekNetworking UnityPeekNetworking;
		public bool ShouldBeTransmitting = false;
		public float SendInterval = 0.5f;

		private Transform itemToTransmit;
		private float lastSendTime = 0f;
		private byte[] chunks;
		private Helpers.HierachyStructure root;

		public void SetUnityPeekNetworking(UnityPeekNetworking networking)
		{
			this.UnityPeekNetworking = networking;
		}

		public void Awake()
		{
			DontDestroyOnLoad(this);
		}

		public void Start()
		{
			Debug.LogError("UnityPeek Object attached and running!");
		}

		public void Update()
		{
			this.transform.Rotate(Vector3.up * Time.deltaTime * 20f);

			if (this.ShouldBeTransmitting)
			{
				// Check if we are past the send interval
				if (this.lastSendTime + this.SendInterval < Time.time)
				{
					this.lastSendTime = Time.time;
					if (this.itemToTransmit != null)
					{
						// Plugin.Logger.LogError(itemToTransmit.name);
						// Plugin.Logger.LogError(itemToTransmit.rotation);
						// Plugin.Logger.LogInfo("Transmitting: " + itemToTransmit.name);
						this.UnityPeekNetworking.SendObject(this.itemToTransmit);
					}
					else
					{
						this.ShouldBeTransmitting = false;
					}
				}
			}
		}

		public static List<Transform> GetAllTransformsIncludingInactiveAndDontDestroy()
		{
			List<Transform> transforms = new List<Transform>();

			// Step 1: Get all regular scene objects
			for (int i = 0; i < SceneManager.sceneCount; i++)
			{
				Scene scene = SceneManager.GetSceneAt(i);
				if (!scene.isLoaded)
				{
					continue;
				}

				GameObject[] rootObjects = scene.GetRootGameObjects();
				foreach (GameObject root in rootObjects)
				{
					AddAllChildTransforms(root.transform, transforms);
				}
			}

			// Step 2: Get DontDestroyOnLoad objects (black magic trick)
			transforms.AddRange(GetDontDestroyOnLoadTransforms());

			return transforms;
		}

		private static void AddAllChildTransforms(Transform parent, List<Transform> list)
		{
			list.Add(parent);
			foreach (Transform child in parent)
			{
				AddAllChildTransforms(child, list);
			}
		}

		private static List<Transform> GetDontDestroyOnLoadTransforms()
		{
			List<Transform> ddolTransforms = new List<Transform>();

			GameObject temp = new GameObject("TempSceneProbe");
			DontDestroyOnLoad(temp);

			Scene ddolScene = temp.scene;

			GameObject[] allRoots = ddolScene.GetRootGameObjects();
			foreach (GameObject root in allRoots)
			{
				if (root.name == "TempSceneProbe")
				{
					continue;
				}

				AddAllChildTransforms(root.transform, ddolTransforms);
			}

			DestroyImmediate(temp); // Clean up

			return ddolTransforms;
		}

		// This function is called to fetch the hierarchy of the game objects in the scene
		// First it gets the parent child relation of all the game objects in the scene
		// Then using the Helpers.HierachyStructure it makes a tree of the game objects
		// Then it serializes the tree into a byte array and sends it
		public void FetchHierachy()
		{
			// Root node
			this.root = new Helpers.HierachyStructure();

			// Transform[] transforms = FindObjectsOfType<Transform>();
			List<Transform> allTransforms = GetAllTransformsIncludingInactiveAndDontDestroy();
			Transform[] transforms = allTransforms.ToArray(); // Convert List to array
			this.root.Name = "Root";
			this.root.ID = "-1";

			Plugin.Logger.LogInfo("Total number of transforms in the scene: " + transforms.Length);

			foreach (Transform t in transforms)
			{
				// setup each object to have a node on it, by default it will be a child of the root node
				Helpers.HierachyStructure node = new Helpers.HierachyStructure();
				node.Name = t.name;
				node.ID = t.GetInstanceID().ToString();
				node.Parent = this.root;
				node.SiblingIndex = t.GetSiblingIndex();
				this.root.Children.Add(node);
			}

			foreach (Transform t in transforms)
			{
				// get the HierachyStructure node that is on this object
				Helpers.HierachyStructure node = Array.Find(this.root.Children.ToArray(), x => x.ID == t.GetInstanceID().ToString());

				if (t.parent == null)
				{
					// if this transform is a root transform aka has no parent
					node.Parent = this.root;
					this.root.Children.Add(node);
				}
				else
				{
					// otherwise it has a parent object, so we need to find its HierachyStructure object and add it as a child and set the parent
					Transform parentTransform = Array.Find(transforms, x => x.GetInstanceID() == t.parent.GetInstanceID());
					if (parentTransform != null)
					{
						// we have found the parent transform for this current transform
						Helpers.HierachyStructure parent = Array.Find(this.root.Children.ToArray(), x => x.ID == parentTransform.GetInstanceID().ToString());
						if (parent != null)
						{
							node.SiblingIndex = t.GetSiblingIndex();
							node.Parent = parent;
							parent.Children.Add(node);
						}
					}
				}
			}

			// PrintHierarchy(root, "-");
			Plugin.Logger.LogInfo(this.root.ToString());
			Plugin.Logger.LogInfo("Making Chunks");
			byte[] bytes = this.SerializeHierarchy();
			Plugin.Logger.LogInfo("Serialized");
			this.chunks = bytes;
			Plugin.Logger.LogInfo("Chunks Made");
			Plugin.Logger.LogInfo(this.chunks.Length);
			this.UnityPeekNetworking.SendChunks(this.chunks);
		}

		public void SelectedNode(string id)
		{
			int idInt = int.Parse(id);

			Plugin.Logger.LogInfo("Selected Node ID: " + idInt);

			UnityEngine.Object foundObject = Helpers.FindObjectFromInstanceID(idInt);
			if (foundObject == null)
			{
				Plugin.Logger.LogError("Found Object is null");
				return;
			}

			Transform foundTransform = foundObject as Transform;

			if (foundTransform == null)
			{
				Plugin.Logger.LogError("Found Transform is null");
				return;
			}

			this.itemToTransmit = foundTransform;
			this.ShouldBeTransmitting = true;
		}

		public void ToggleTransformActive(string id, string enabled)
		{
			int idInt = int.Parse(id);
			UnityEngine.Object foundObject = Helpers.FindObjectFromInstanceID(idInt);
			if (foundObject == null)
			{
				Plugin.Logger.LogError("Found Object is null");
				return;
			}

			Transform foundTransform = foundObject as Transform;

			if (foundTransform == null)
			{
				Plugin.Logger.LogError("Found Transform is null");
				return;
			}

			if (enabled == "True")
			{
				Plugin.Logger.LogInfo("Enabling Transform: " + foundTransform.name);
				foundTransform.gameObject.SetActive(true);
			}
			else
			{
				Plugin.Logger.LogInfo("Disabling Transform: " + foundTransform.name);
				foundTransform.gameObject.SetActive(false);
			}
		}

		/// <summary>
		/// Prints the hierarchy of the game objects in the scene.
		/// </summary>
		/// <param name="node">Root node.</param>
		/// <param name="indent">Indentation character.</param>
		private void PrintHierarchy(Helpers.HierachyStructure node, string indent = "")
		{
			Plugin.Logger.LogInfo(indent + node.Name); // Print the current node with indentation

			foreach (Helpers.HierachyStructure child in node.Children)
			{
				this.PrintHierarchy(child, indent + "  "); // Recursively print children with increased indentation
			}
		}

		public byte[] SerializeHierarchy()
		{
			Plugin.Logger.LogInfo("Serializing");
			using (MemoryStream ms = new MemoryStream())
			using (BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8))
			{
				// Plugin.Logger.LogInfo("Using BinaryWriter");
				this.WriteNode(writer, this.root);
				return ms.ToArray();
			}
		}

		private void WriteNode(BinaryWriter writer, Helpers.HierachyStructure node)
		{
			try
			{
				// Plugin.Logger.LogInfo("Writing Node!" + node.name);
				writer.Write(node.Name);

				// Plugin.Logger.LogInfo("Writing Node Name!");
				writer.Write(node.ID);

				// Plugin.Logger.LogInfo("Writing Node ID!");
				writer.Write(node.SiblingIndex);

				writer.Write(node.Children.Count); // Write number of children

				// Plugin.Logger.LogInfo("Writing Child Count");
				foreach (var child in node.Children)
				{
					// Plugin.Logger.LogInfo("Writing Node Child!");
					this.WriteNode(writer, child); // Recursively write child nodes
				}

				// Plugin.Logger.LogInfo("Done with those nodes");
			}
			catch (Exception e)
			{
				Plugin.Logger.LogError(e);
			}
		}

		private static List<byte[]> ChunkData(byte[] data, int chunkSize = 1024)
		{
			// Plugin.Logger.LogInfo("Chunking Data");
			List<byte[]> chunks = new List<byte[]>();

			for (int i = 0; i < data.Length; i += chunkSize)
			{
				int size = Math.Min(chunkSize, data.Length - i);
				byte[] chunk = new byte[size];
				Array.Copy(data, i, chunk, 0, size);
				chunks.Add(chunk);
			}

			return chunks;
		}
	}
}
