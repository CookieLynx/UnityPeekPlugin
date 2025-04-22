namespace UnityPeekPlugin.GameObjects
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Net;
	using System.Net.Sockets;
	using System.Text;
	using System.Threading;
	using UnityEngine;

	public class UnityPeekNetworking : MonoBehaviour
	{
		public UnityPeekController UnityPeekController;
		private static int timeStamp = 0;
		private TcpListener server;
		private Thread socketThread;
		private bool isRunning = false;
		private byte[] dataToSend = null;
		private byte[] chunks;
		private bool sendChunks = false;

		public void SetUnityPeekController(UnityPeekController controller)
		{
			this.UnityPeekController = controller;
		}

		public void Start()
		{
			Debug.LogError("UnityPeek Networking Object attached and running!");

			// get the local address
			string localIP = this.GetLocalIPAddress();
			Debug.LogError("Local IP: " + localIP);
			Debug.LogError("Using IP " + ConfigManager.IP + ":" + ConfigManager.Port);
			this.StartSocketServer(ConfigManager.IP, ConfigManager.Port);
		}

		private string GetLocalIPAddress()
		{
			var host = Dns.GetHostEntry(Dns.GetHostName());
			foreach (var ip in host.AddressList)
			{
				if (ip.AddressFamily == AddressFamily.InterNetwork) // IPv4 only
				{
					return ip.ToString();
				}
			}

			Debug.LogError("No network adapters with an IPv4 address found.");
			throw new Exception("No network adapters with an IPv4 address found.");
		}

		public void StartSocketServer(string address, string stringPort)
		{
			this.isRunning = true;

			this.socketThread = new Thread(() =>
			{
				try
				{
					// Using the main thread dispatcher to log to the console
					UnityMainThreadDispatcher.Instance().Enqueue(() =>
					{
						Debug.LogError("Starting socket server...");
					});

					int port = int.Parse(stringPort);

					// Start listening for connections
					this.server = new TcpListener(IPAddress.Parse(address), port);
					this.server.Start();
					UnityMainThreadDispatcher.Instance().Enqueue(() =>
					{
						Debug.LogError($"Server started on {address}:{port}");
					});

					// Check if the server is running
					while (this.isRunning)
					{
						if (this.server.Pending())
						{
							TcpClient client = this.server.AcceptTcpClient();
							UnityMainThreadDispatcher.Instance().Enqueue(() =>
							{
								Debug.Log("Client connected!");
							});

							// Handle the client in a separate thread
							Thread clientThread = new Thread(() => this.HandleClient(client));
							clientThread.IsBackground = true;
							clientThread.Start();
						}

						Thread.Sleep(10); // Avoid high CPU usage
					}
				}
				catch (Exception ex)
				{
					UnityMainThreadDispatcher.Instance().Enqueue(() =>
					{
						Debug.LogError($"Socket server error: {ex.Message}");
					});
				}
			});

			this.socketThread.IsBackground = true;
			this.socketThread.Start();
		}

		public void SendChunks(byte[] chunks)
		{
			// sendChunks = true;
			// this.chunks = chunks;
			this.chunks = chunks;
			this.sendChunks = true;
		}

		private void SendPacket(byte[] packet, NetworkStream stream)
		{
			int length = packet.Length;

			// Plugin.Logger.LogError("THE LENGTH OF THE PACKET IS: " + length);
			byte[] lengthBytes = BitConverter.GetBytes(length); // First 4 bytes = message length
			byte[] packetWithLength = new byte[lengthBytes.Length + packet.Length];

			// Put together the full packet
			Array.Copy(lengthBytes, 0, packetWithLength, 0, lengthBytes.Length);      // Length
			Array.Copy(packet, 0, packetWithLength, lengthBytes.Length, packet.Length); // Message (starting with type ID)

			// Now write the full packet
			stream.Write(packetWithLength, 0, packetWithLength.Length);
		}

		private void HandleClient(TcpClient client)
		{
			NetworkStream stream = client.GetStream();
			byte[] buffer = new byte[1024];

			// Send initial response
			byte[] typeBytes = BitConverter.GetBytes(0); // String type
			byte[] data = Encoding.ASCII.GetBytes("Hello UnityPeek!"); // Our message
			byte[] packet = new byte[typeBytes.Length + data.Length]; // Make the packet the size of the type and data
			Array.Copy(typeBytes, 0, packet, 0, typeBytes.Length); // Copy the type bytes to the packet
			Array.Copy(data, 0, packet, typeBytes.Length, data.Length); // Copy the data bytes to the packet

			// stream.Write(packet, 0, packet.Length); //Send the packet to the client
			this.SendPacket(packet, stream);

			UnityMainThreadDispatcher.Instance().Enqueue(() =>
			{
				Debug.Log("Sent greeting to UnityPeek!");
			});

			while (this.isRunning)
			{
				if (client.Client.Poll(0, SelectMode.SelectRead) && client.Client.Available == 0)
				{
					break;
				}

				if (this.dataToSend != null)
				{
					typeBytes = BitConverter.GetBytes(3); // Transform type
					data = this.dataToSend; // Our message
					packet = new byte[typeBytes.Length + data.Length]; // Make the packet the size of the type and data
					Array.Copy(typeBytes, 0, packet, 0, typeBytes.Length); // Copy the type bytes to the packet
					Array.Copy(data, 0, packet, typeBytes.Length, data.Length); // Copy the data bytes to the packet

					// stream.Write(packet, 0, packet.Length); //Send the packet to the client
					this.SendPacket(packet, stream);
					this.dataToSend = null; // Reset the data to send
				}

				packet = this.SendHierachy(stream, packet);

				// Decode the received data
				if (stream.DataAvailable)
				{
					int bytesRead = stream.Read(buffer, 0, buffer.Length);
					if (bytesRead > 0)
					{
						this.DecodeRecivedData(buffer, bytesRead);

						// Echo message back to client
						// byte[] responseData = Encoding.ASCII.GetBytes($"Server Received: {received}");
						// stream.Write(responseData, 0, responseData.Length);
					}
				}

				Thread.Sleep(10);
			}

			client.Close();
			this.UnityPeekController.ShouldBeTransmitting = false;
			UnityMainThreadDispatcher.Instance().Enqueue(() =>
			{
				Debug.LogError("Client disconnected.");
			});
		}

		/// <summary>
		/// Sends the hierarchy data to the client in chunks.
		/// </summary>
		/// <param name="stream">Network Stream.</param>
		/// <param name="packet">The packet that contains the serialized node data.</param>
		/// <returns>The bytes that will be sent.</returns>
		private byte[] SendHierachy(NetworkStream stream, byte[] packet)
		{
			if (this.sendChunks)
			{
				this.sendChunks = false;
				byte[] header = BitConverter.GetBytes(1); // heirarchy type
				Plugin.Logger.LogInfo("Sending a chunk");

				// byte[] chunk = chunks[0];
				byte[] chunk = this.chunks;
				packet = new byte[header.Length + chunk.Length];
				Array.Copy(header, 0, packet, 0, header.Length);
				Array.Copy(chunk, 0, packet, header.Length, chunk.Length);

				Plugin.Logger.LogInfo("Sending Data");
				try
				{
					// stream.Write(packet, 0, packet.Length);
					this.SendPacket(packet, stream);
				}
				catch (Exception e)
				{
					Plugin.Logger.LogError(e);
				}

				// chunks.RemoveAt(0);
			}

			return packet;
		}

		public void StopSocketConnection()
		{
			this.isRunning = false;
			this.socketThread?.Join();
			Debug.LogError("Socket connection stopped.");
		}

		private void OnApplicationQuit()
		{
			this.StopSocketConnection();
		}

		private void DecodeRecivedData(byte[] buffer, int bytesRead)
		{
			string received = Encoding.ASCII.GetString(buffer, 0, bytesRead);
			UnityMainThreadDispatcher.Instance().Enqueue(() =>
			{
				Debug.Log($"Received: {received}");
			});

			string type;
			string[] parts = new string[0];
			if (!received.Contains(':'))
			{
				type = received;
			}
			else
			{
				// split the stuff before the :
				parts = received.Split(':');
				type = parts[0];
			}

			switch (type)
			{
				case "FetchHierarchy":
					// Do something
					this.UnityPeekController.FetchHierachy();
					break;
				case "SelectedNode":
					this.UnityPeekController.SelectedNode(parts[1]);
					break;
				case "ToggleTransformActive":
					this.UnityPeekController.ToggleTransformActive(parts[1], parts[2]);
					break;
				default:
					Plugin.Logger.LogInfo("Unknown data recieved");

					// Do something else
					break;
			}
		}

		public void SendObject(Transform transformToTransmit)
		{
			timeStamp++;

			// Serialize the position, rotation, and scale of the transform into bytes
			Vector3 position = transformToTransmit.position;
			Quaternion rotation = transformToTransmit.rotation;
			Vector3 scale = transformToTransmit.localScale;

			using (MemoryStream memoryStream = new MemoryStream())
			{
				using (BinaryWriter writer = new BinaryWriter(memoryStream))
				{
					// Write position
					// writer.Write((float)timeStamp);
					writer.Write(transformToTransmit.name);
					writer.Write(transformToTransmit.gameObject.activeSelf);

					writer.Write(position.x);
					writer.Write(position.y);
					writer.Write(position.z);

					// Write rotation
					writer.Write(rotation.x);
					writer.Write(rotation.y);
					writer.Write(rotation.z);
					writer.Write(rotation.w);

					// Write scale
					writer.Write(scale.x);
					writer.Write(scale.y);
					writer.Write(scale.z);
				}

				// Assign serialized data to dataToSend
				this.dataToSend = memoryStream.ToArray();
			}

			// Plugin.Logger.LogError("TIMESTAMP SENT: " + timeStamp);
		}
	}
}
