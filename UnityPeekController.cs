using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityPeekPlugin
{
    //This call handles the connection of serial data between the UnityPeek and the UnityPeekPlugin

    class UnityPeekController : MonoBehaviour
    {

        string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) // IPv4 only
                {
                    return ip.ToString();
                }
            }
            Debug.LogError("No network adapters with an IPv4 address found.");
            throw new System.Exception("No network adapters with an IPv4 address found.");
            
        }

        void Start()
        {
            Debug.LogError("UnityPeek Object attached and running!");


            //get the local address
            string localIP = GetLocalIPAddress();
            Debug.LogError("Local IP: " + localIP);
            Debug.LogError("Using IP " + ConfigManager.IP + ":" + ConfigManager.port);
            StartSocketServer(ConfigManager.IP, ConfigManager.port);


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

        void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            // Send initial response
            byte[] data = Encoding.ASCII.GetBytes("Hello UnityPeek!");
            stream.Write(data, 0, data.Length);
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                Debug.Log("Sent greeting to UnityPeek!");
            });

            while (isRunning)
            {
                if (stream.DataAvailable)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string received = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        UnityMainThreadDispatcher.Instance().Enqueue(() =>
                        {
                            Debug.Log($"Received: {received}");
                        });

                        UnityMainThreadDispatcher.Instance().Enqueue(() =>
                        {
                            Debug.Log($"Message on main thread: {received}");
                        });

                        // Echo message back to client
                        byte[] responseData = Encoding.ASCII.GetBytes($"Server Received: {received}");
                        stream.Write(responseData, 0, responseData.Length);
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
