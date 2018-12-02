﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace Sabresaurus.EditorNetworking
{
    /* Editor:
     * 
     * Starts listening for UDP messages (acts as UDP Server)
     * When it gets one it connects to the IP over TCP (acts as TCP Client)
     * 
     * Device:
     * 
     * Starts broadcasting UDP messages (acts as UDP client)
     * Also listeners for TCP connections (acts as TCP server)
     * 
     */
    public static class EditorMessaging
    {
        static Action<byte[]> responseCallback = null;

        // ip address to display name mappings
        static Dictionary<string, string> knownEndpoints = new Dictionary<string, string>()
        {
            //{"127.0.0.1", "Local Loopback"}
        };


        static TcpClient pendingClient = null;
        private static UdpClient receivingUdpClient;

        public static Dictionary<string, string> KnownEndpoints
        {
            get
            {
                // Return a copy
                return new Dictionary<string, string>(knownEndpoints);
            }
        }

        public static string ConnectedIP
        {
            get
            {
                string activeEndpoint = null;
                foreach (var pair in knownEndpoints)
                {
                    activeEndpoint = pair.Key;
                }
                return activeEndpoint;
            }
        }


        public struct UdpState
        {
            public UdpClient udpClient;
            public IPEndPoint endPoint;
        }

        public static bool Started
        {
            get
            {
                return receivingUdpClient != null;
            }
        }

        public static void Start()
        {
            StartReceivingBroadcasts();
        }

        public static void RegisterForResponses(Action<byte[]> callback)
        {
            responseCallback -= callback;
            responseCallback += callback;
        }

        public static void StartReceivingBroadcasts()
        {
            // Receive a message and write it to the console.
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, PlayerMessaging.BROADCAST_PORT);
            UdpClient udpClient = new UdpClient(endPoint);
            udpClient.Client.SendTimeout = 5000;
            udpClient.Client.ReceiveTimeout = 5000;

            UdpState udpState = new UdpState();
            udpState.endPoint = endPoint;
            udpState.udpClient = udpClient;

#if SIDEKICK_DEBUG
            Debug.Log("Listening for player broadcasts");
#endif
            udpClient.BeginReceive(new AsyncCallback(ReceivedBroadcastFromPlayer), udpState);
            receivingUdpClient = udpClient;
        }

        /// <summary>
        /// Fired when a player's UDP broadcast reaches us
        /// </summary>
        public static void ReceivedBroadcastFromPlayer(IAsyncResult ar)
        {
            UdpState udpState = (UdpState)ar.AsyncState;
            UdpClient udpClient = udpState.udpClient;
            IPEndPoint endPoint = udpState.endPoint;

            byte[] receivedBytes = udpClient.EndReceive(ar, ref endPoint);
            string receivedString = Encoding.UTF8.GetString(receivedBytes);

            if (!knownEndpoints.ContainsKey(endPoint.Address.ToString()))
            {
                knownEndpoints.Add(endPoint.Address.ToString(), receivedString);
#if SIDEKICK_DEBUG
                Debug.Log(string.Format("New EndPoint: {0} from {1}", receivedString, endPoint));
#endif
            }
            udpClient.BeginReceive(new AsyncCallback(ReceivedBroadcastFromPlayer), ((UdpState)(ar.AsyncState)));
        }

        public static void SendRequest(byte[] sendingBuffer)
        {
            SendRequest(ConnectedIP, sendingBuffer);
        }
        public static void SendRequest(string targetIP, byte[] sendingBuffer)
        {
            try
            {
                TcpClient client;
                if (pendingClient != null && pendingClient.Connected)
                {
                    client = pendingClient;
                }
                else
                {
                    client = new TcpClient();

                    client.Client.SendTimeout = 5000;
                    client.Client.ReceiveTimeout = 5000;
#if SIDEKICK_DEBUG
                    Debug.Log("Connecting...");
#endif
                    client.Connect(targetIP, PlayerMessaging.REQUEST_PORT);


#if SIDEKICK_DEBUG
                    Debug.Log("Connected to player");
#endif
                }

                NetworkStream stream = client.GetStream();

#if SIDEKICK_DEBUG
                Debug.Log("Sending request to Player");
#endif

                stream.Write(sendingBuffer, 0, sendingBuffer.Length);

                // Player should respond so wait until it does
                pendingClient = client;
            }
            catch(SocketException e)
            {
                pendingClient = null;
                if(knownEndpoints.ContainsKey(targetIP))
                {
                    knownEndpoints.Remove(targetIP);
                }
            }
        }

        public static void Tick()
        {

            if (pendingClient != null)
            {
                var stream = pendingClient.GetStream();
                if (stream.DataAvailable)
                {
                    while (stream.DataAvailable)
                    {
                        byte[] networkResponseBuffer = new byte[100000];
                        var count = stream.Read(networkResponseBuffer, 0, networkResponseBuffer.Length);

                        byte[] limitedBuffer = new byte[networkResponseBuffer.Length];
                        Array.Copy(networkResponseBuffer, limitedBuffer, count);
#if SIDEKICK_DEBUG
                        Debug.Log(string.Format("Response received in editor, length is {0}", count));
#endif
                        if (responseCallback != null)
                        {
                            responseCallback(limitedBuffer);
                        }
                    }
                }
            }
        }
    }
}