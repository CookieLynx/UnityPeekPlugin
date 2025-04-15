using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace UnityPeekPlugin.GameObjects
{
	//This call handles the connection of serial data between the UnityPeek and the UnityPeekPlugin

	class UnityPeekController : MonoBehaviour
	{
		private UnityPeekNetworking unityPeekNetworking;

		public void SetUnityPeekNetworking(UnityPeekNetworking networking)
		{
			unityPeekNetworking = networking;
		}
		void Awake()
		{
			DontDestroyOnLoad(this);
		}
		void Start()
		{
			Debug.LogError("UnityPeek Object attached and running!");

		}

		public bool shouldBeTransmitting = false;

		private Transform itemToTransmit;

		public float sendInterval = 0.5f;

		private float lastSendTime = 0f;


		void Update()
		{
			transform.Rotate(Vector3.up * Time.deltaTime * 20f);

			if (shouldBeTransmitting)
			{
				//Check if we are past the send interval
				if (lastSendTime + sendInterval < Time.time)
				{
					lastSendTime = Time.time;
					if (itemToTransmit != null)
					{
						//Plugin.Logger.LogError(itemToTransmit.name);
						//Plugin.Logger.LogError(itemToTransmit.rotation);
						//Plugin.Logger.LogInfo("Transmitting: " + itemToTransmit.name);
						unityPeekNetworking.SendObject(itemToTransmit);
					}
					else
					{
						shouldBeTransmitting = false;
					}
				}
			}



		}

		private byte[] chunks;
		Helpers.HierachyStructure root;
		//This function is called to fetch the hierarchy of the game objects in the scene
		//First it gets the parent child relation of all the game objects in the scene
		//Then using the Helpers.HierachyStructure it makes a tree of the game objects
		//Then it serializes the tree into a byte array and sends it
		public void FetchHierachy()
		{
			//Root node
			root = new Helpers.HierachyStructure();
			Transform[] transforms = FindObjectsOfType<Transform>();
			root.name = "Root";
			root.id = "-1";

			Plugin.Logger.LogInfo("Total number of transforms in the scene: " + transforms.Length);

			foreach (Transform t in transforms)
			{

				//setup each object to have a node on it, by default it will be a child of the root node
				Helpers.HierachyStructure node = new Helpers.HierachyStructure();
				node.name = t.name;
				node.id = t.GetInstanceID().ToString();
				node.parent = root;
				node.siblingIndex = t.GetSiblingIndex();
				root.children.Add(node);
			}



			foreach (Transform t in transforms)
			{
				//get the HierachyStructure node that is on this object
				Helpers.HierachyStructure node = Array.Find(root.children.ToArray(), x => x.id == t.GetInstanceID().ToString());

				if (t.parent == null)
				{
					//if this transform is a root transform aka has no parent
					node.parent = root;
					root.children.Add(node);
				}
				else
				{
					// otherwise it has a parent object, so we need to find its HierachyStructure object and add it as a child and set the parent
					Transform parentTransform = Array.Find(transforms, x => x.GetInstanceID() == t.parent.GetInstanceID());
					if (parentTransform != null)
					{
						//we have found the parent transform for this current transform
						Helpers.HierachyStructure parent = Array.Find(root.children.ToArray(), x => x.id == parentTransform.GetInstanceID().ToString());
						if (parent != null)
						{
							node.siblingIndex = t.GetSiblingIndex();
							node.parent = parent;
							parent.children.Add(node);
						}
					}
				}
			}


			//PrintHierarchy(root, "-");
			Plugin.Logger.LogInfo(root.ToString());
			Plugin.Logger.LogInfo("Making Chunks");
			byte[] bytes = SerializeHierarchy();
			Plugin.Logger.LogInfo("Serialized");
			chunks = bytes;
			Plugin.Logger.LogInfo("Chunks Made");
			Plugin.Logger.LogInfo(chunks.Length);
			unityPeekNetworking.SendChunks(chunks);
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

			itemToTransmit = foundTransform;
			shouldBeTransmitting = true;



		}




		/// <summary>
		/// Prints the hierarchy of the game objects in the scene
		/// </summary>
		/// <param name="node">Root node</param>
		/// <param name="indent">Indentation character</param>
		private void PrintHierarchy(Helpers.HierachyStructure node, string indent = "")
		{
			Plugin.Logger.LogInfo(indent + node.name); // Print the current node with indentation

			foreach (Helpers.HierachyStructure child in node.children)
			{
				PrintHierarchy(child, indent + "  "); // Recursively print children with increased indentation
			}
		}


		public byte[] SerializeHierarchy()
		{
			Plugin.Logger.LogInfo("Serializing");
			using (MemoryStream ms = new MemoryStream())
			using (BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8))
			{
				//Plugin.Logger.LogInfo("Using BinaryWriter");
				WriteNode(writer, root);
				return ms.ToArray();
			}
		}


		private void WriteNode(BinaryWriter writer, Helpers.HierachyStructure node)
		{
			try
			{
				//Plugin.Logger.LogInfo("Writing Node!" + node.name);
				writer.Write(node.name);
				//Plugin.Logger.LogInfo("Writing Node Name!");
				writer.Write(node.id);
				//Plugin.Logger.LogInfo("Writing Node ID!");
				writer.Write(node.siblingIndex);

				writer.Write(node.children.Count); // Write number of children
												   //Plugin.Logger.LogInfo("Writing Child Count");

				foreach (var child in node.children)
				{
					//Plugin.Logger.LogInfo("Writing Node Child!");
					WriteNode(writer, child); // Recursively write child nodes
				}
				//Plugin.Logger.LogInfo("Done with those nodes");
			}
			catch (Exception e)
			{
				Plugin.Logger.LogError(e);
			}
		}

		private static List<byte[]> ChunkData(byte[] data, int chunkSize = 1024)
		{
			//Plugin.Logger.LogInfo("Chunking Data");
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
