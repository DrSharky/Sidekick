﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace Sabresaurus.EditorNetworking
{
    public static class PlayerMessaging
    {
        public const int BROADCAST_PORT = 10061;
        public const int REQUEST_PORT = 10062;

        static DateTime lastSend = DateTime.MinValue;

        public delegate byte[] ReceivedRequestCallback(byte[] request);
        static ReceivedRequestCallback onReceivedRequest = null;

        static IAsyncResult pendingAsyncResult = null;
        static NetworkStream activeStream = null;

        public static void Start()
        {
            // Listen for connections from editors
            ListenForRequests();
            // Start telling editors we're here
            Broadcast();
        }

        public static void RegisterForRequests(ReceivedRequestCallback callback)
        {
            onReceivedRequest += callback;
        }

        public static void ListenForRequests() // server
        {
            TcpListener tcpListener = new TcpListener(IPAddress.Any, REQUEST_PORT);

            tcpListener.Start();

#if SIDEKICK_DEBUG
            Debug.Log(string.Format("The server is running at port " + REQUEST_PORT));
            Debug.Log(string.Format("The local endpoint is: " + tcpListener.LocalEndpoint));
            Debug.Log(string.Format("Waiting for a connection..."));
#endif
            tcpListener.BeginAcceptTcpClient(OnAcceptTcpClient, tcpListener);
        }

        static void OnAcceptTcpClient(IAsyncResult asyncResult)
        {
            pendingAsyncResult = asyncResult;
        }

        public static void Broadcast()
        {
            lastSend = DateTime.UtcNow;
            var Client = new UdpClient();

            Client.Client.SendTimeout = 5000;
            Client.Client.ReceiveTimeout = 5000;
            string broadcastString = GetDisplayName();
            var broadcastData = Encoding.UTF8.GetBytes(broadcastString);

            // Tell everyone I'm here
            Client.EnableBroadcast = true;
            Client.Send(broadcastData, broadcastData.Length, new IPEndPoint(IPAddress.Broadcast, BROADCAST_PORT));

            // TODO: Be good here to track which Editor's got it (if we can)
            //var ServerResponseData = Client.Receive(ref ServerEp);
            //var ServerResponse = Encoding.ASCII.GetString(ServerResponseData);
            //WriteLine("Recived {0} from {1}", ServerResponse, ServerEp.Address.ToString());
            Client.Close();
        }

        static string GetDisplayName()
        {
            if (SystemInfo.deviceType == DeviceType.Desktop)
            {
                return SystemInfo.deviceName + " " + Application.platform;
            }
            else
            {
#if UNITY_IOS
                var iosModel = UnityEngine.iOS.Device.generation;
                if (iosModel != UnityEngine.iOS.DeviceGeneration.Unknown
                   && iosModel != UnityEngine.iOS.DeviceGeneration.iPhoneUnknown
                   && iosModel != UnityEngine.iOS.DeviceGeneration.iPadUnknown
                   && iosModel != UnityEngine.iOS.DeviceGeneration.iPodTouchUnknown)
                {
                    return SystemInfo.deviceName + " " + iosModel;
                }

#endif
                return SystemInfo.deviceName + " " + Application.platform + " " + SystemInfo.deviceModel;
            }
        }

        public static void Tick()
        {
#if !UNITY_EDITOR
            // TODO: This timer should be more complicated:
            // It should fire rarely when an editor has made recent requests
            // It should fire frequently when the app has started or resumed
            // It should fire rarely when the app has been open a while
            if(DateTime.UtcNow - lastSend > TimeSpan.FromSeconds(5))
            {
                Broadcast();
            }
#endif

            if (pendingAsyncResult != null)
            {
                var tcpListener = (TcpListener)pendingAsyncResult.AsyncState;
                TcpClient result = tcpListener.EndAcceptTcpClient(pendingAsyncResult);
                pendingAsyncResult = null;
                activeStream = result.GetStream();
            }

            if(activeStream != null)
            {
                NetworkStream stream = activeStream;
                if (stream.DataAvailable)
                {
                    byte[] requestBuffer = new byte[1000];
                    var count = stream.Read(requestBuffer, 0, requestBuffer.Length);

                    if (onReceivedRequest != null)
                    {
                        byte[] responseBuffer = onReceivedRequest(requestBuffer);
                        stream.Write(responseBuffer, 0, responseBuffer.Length);
                    }
                    else
                    {
                        // TODO: display some helpful message
                        stream.WriteByte(0);
                    }
                }
            }
        }
    }
}