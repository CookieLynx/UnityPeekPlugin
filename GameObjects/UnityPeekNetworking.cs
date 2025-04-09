using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using UnityEngine;
using System.Threading;
using System.IO;

namespace UnityPeekPlugin.GameObjects
{
    class UnityPeekNetworking : MonoBehaviour
    {

        private UnityPeekController unityPeekController;

        public void SetUnityPeekController(UnityPeekController controller)
        {
            unityPeekController = controller;
        }


        void Start()
        {
            Debug.LogError("UnityPeek Networking Object attached and running!");
            //get the local address
            string localIP = GetLocalIPAddress();
            Debug.LogError("Local IP: " + localIP);
            Debug.LogError("Using IP " + ConfigManager.IP + ":" + ConfigManager.port);
            StartSocketServer(ConfigManager.IP, ConfigManager.port);
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

        private TcpListener server;
        private Thread socketThread;
        private bool isRunning = false;


        public void StartSocketServer(string address, string stringPort)
        {

            isRunning = true;

            socketThread = new Thread(() =>
            {
                try
                {
                    //Using the main thread dispatcher to log to the console
                    UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    {
                        Debug.LogError("Starting socket server...");
                    });


                    int port = int.Parse(stringPort);

                    // Start listening for connections
                    server = new TcpListener(IPAddress.Parse(address), port);
                    server.Start();
                    UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    {
                        Debug.LogError($"Server started on {address}:{port}");
                    });

                    //Check if the server is running
                    while (isRunning)
                    {
                        if (server.Pending())
                        {
                            TcpClient client = server.AcceptTcpClient();
                            UnityMainThreadDispatcher.Instance().Enqueue(() =>
                            {
                                Debug.Log("Client connected!");
                            });

                            // Handle the client in a separate thread
                            Thread clientThread = new Thread(() => HandleClient(client));
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

            socketThread.IsBackground = true;
            socketThread.Start();
        }

        private byte[] dataToSend = null;

        private byte[] chunks;
        public void SendChunks(byte[] chunks)
        {
            //sendChunks = true;
            //this.chunks = chunks;
            this.chunks = chunks;
            sendChunks = true;
        }

        bool sendChunks = false;


        void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            // Send initial response


            byte[] typeBytes = BitConverter.GetBytes(0); //String type
            byte[] data = Encoding.ASCII.GetBytes("Hello UnityPeek!"); //Our message
            byte[] packet = new byte[typeBytes.Length + data.Length]; //Make the packet the size of the type and data
            Array.Copy(typeBytes, 0, packet, 0, typeBytes.Length); //Copy the type bytes to the packet
            Array.Copy(data, 0, packet, typeBytes.Length, data.Length); //Copy the data bytes to the packet
            stream.Write(packet, 0, packet.Length); //Send the packet to the client


            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                Debug.Log("Sent greeting to UnityPeek!");
            });






            while (isRunning)
            {

                if (dataToSend != null)
                {
                    typeBytes = BitConverter.GetBytes(3); //Transform type
                    data = dataToSend; //Our message
                    packet = new byte[typeBytes.Length + data.Length]; //Make the packet the size of the type and data
                    Array.Copy(typeBytes, 0, packet, 0, typeBytes.Length); //Copy the type bytes to the packet
                    Array.Copy(data, 0, packet, typeBytes.Length, data.Length); //Copy the data bytes to the packet
                    stream.Write(packet, 0, packet.Length); //Send the packet to the client
                }

                packet = SendHierachy(stream, packet);

                //if(testChunks.Length > 0)
                //{
                //Plugin.Logger.LogInfo("Sending a chunk");
                //byte[] chunk = chunks[0];
                //stream.Write(chunk, 0, chunk.Length);
                //stream.Write(testChunks, 0, testChunks.Length);
                //chunks.RemoveAt(0);
                //testChunks.
                //}



                //Decode the received data
                if (stream.DataAvailable)
                {

                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        DecodeRecivedData(buffer, bytesRead);
                        // Echo message back to client
                        //byte[] responseData = Encoding.ASCII.GetBytes($"Server Received: {received}");
                        //stream.Write(responseData, 0, responseData.Length);
                    }
                }
                Thread.Sleep(10);
            }

            client.Close();
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                Debug.LogError("Client disconnected.");
            });
        }

        /// <summary>
        /// Sends the hierarchy data to the client in chunks
        /// </summary>
        /// <param name="stream">Network Stream</param>
        /// <param name="packet">The packet that contains the serialized node data</param>
        /// <returns></returns>
        private byte[] SendHierachy(NetworkStream stream, byte[] packet)
        {
            if (sendChunks)
            {
                sendChunks = false;
                byte[] header = BitConverter.GetBytes(1); //heirarchy type
                Plugin.Logger.LogInfo("Sending a chunk");
                //byte[] chunk = chunks[0];


                byte[] chunk = chunks;
                packet = new byte[header.Length + chunk.Length];
                Array.Copy(header, 0, packet, 0, header.Length);
                Array.Copy(chunk, 0, packet, header.Length, chunk.Length);


                Plugin.Logger.LogInfo("Sending Data");
                stream.Write(packet, 0, packet.Length);
                //chunks.RemoveAt(0);
            }

            return packet;
        }

        public void StopSocketConnection()
        {
            isRunning = false;
            socketThread?.Join();
            Debug.LogError("Socket connection stopped.");
        }

        private void OnApplicationQuit()
        {
            StopSocketConnection();
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
                //split the stuff before the :
                parts = received.Split(':');
                type = parts[0];
            }

            switch (type)
            {
                case "FetchHierarchy":
                    // Do something
                    unityPeekController.FetchHierachy();
                    break;
                case "SelectedNode":
                    unityPeekController.SelectedNode(parts[1]);
                    break;
                default:
                    Plugin.Logger.LogInfo("Unknown data recieved");
                    // Do something else
                    break;
            }
        }


        public void SendObject(Transform transformToTransmit)
        {
            // Serialize the position, rotation, and scale of the transform into bytes
            Vector3 position = transformToTransmit.position;
            Quaternion rotation = transformToTransmit.rotation;
            Vector3 scale = transformToTransmit.localScale;

            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(memoryStream))
                {
                    // Write position
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
                dataToSend = memoryStream.ToArray();
            }
        }
    }
}
