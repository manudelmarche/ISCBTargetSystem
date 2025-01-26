//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
// Modified by E. DELMARCHE
//

using System;
using System.Device.Gpio;
using System.Device.Wifi;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.WebSockets;
using System.Net.WebSockets.Server;
using System.Text;
using System.Threading;
using Iot.Device.DhcpServer;
using nanoFramework.Networking;
using nanoFramework.Runtime.Native;


namespace ISCBTargetSystem
{
    public class Program
    {
        // Start Simple WebServer
        private static WebServer _server = new WebServer();
        private static bool _wifiApMode = false;

        // Connected Station count
        private static int _connectedCount = 0;

        // GPIO pin used to put device into AP set-up mode
        private const int SetupPin = 5;

        public static void Main()
        {
            Debug.WriteLine("Welcome to ISCB Target System");

            var gpioController = new GpioController();
            GpioPin setupButton = gpioController.OpenPin(SetupPin, PinMode.InputPullUp);

            // If Wireless station is not enabled then start Soft AP to allow Wireless configuration
            // or Button pressed
            if (setupButton.Read() == PinValue.High)
            {
                WirelessAP.SetWifiAp();
                //NetworkChange.NetworkAPStationChanged += NetworkChange_NetworkAPStationChanged;
                _wifiApMode = true;
            }
            else
            {
                _wifiApMode = Wireless80211.ConnectOrSetAp();
            }

            Console.WriteLine($"Connected with wifi credentials. IP Address: {(_wifiApMode ? WirelessAP.GetIP() : Wireless80211.GetCurrentIPAddress())}");           
            if( _wifiApMode )
            {
                _server.Start();

            }

            WebSocketServer webSocketServer = new WebSocketServer();
            webSocketServer.Start();
            //Let's echo all incomming messages from clients to all connected clients including the sender. 
            webSocketServer.MessageReceived += WebSocketServer_MesageReceived;
            Console.WriteLine($"WebSocket server is up and running, connect on: ws://127.0.0.1:{webSocketServer.Port}{webSocketServer.Prefix}");

            // Just wait for now
            // Here you would have the reset of your program using the client WiFI link
            Thread.Sleep(Timeout.Infinite);
        }

        private static void WebSocketServer_MesageReceived(object sender, MessageReceivedEventArgs e)
        {
            if (e.Frame.MessageType == System.Net.WebSockets.WebSocketFrame.WebSocketMessageType.Text)
            {
                Debug.WriteLine($"websocket message: {Encoding.UTF8.GetString(e.Frame.Buffer, 0, e.Frame.Buffer.Length)}");
            }
            else
            {
                Debug.WriteLine("websocket binary data");
            }
        }

        //private static void NetworkChange_NetworkAPStationChanged(int NetworkIndex, NetworkAPStationEventArgs e)
        //{
        //    Console.WriteLine("A device connected or disconnected");
        //    //WifiAdapter wifi = WifiAdapter.FindAllAdapters()[0];
        //    //WirelessAPConfiguration bucht=new WirelessAPConfiguration((uint)NetworkIndex);
        //    //var stations=bucht.GetConnectedStations();
        //    //foreach (var station in stations)
        //    //{
        //    //    Console.WriteLine(Encoding.UTF8.GetString(station.MacAddress, 0, station.MacAddress.Length));
        //    //}

        //}
        /// <summary>
        /// Event handler for Stations connecting or Disconnecting
        /// </summary>
        /// <param name="NetworkIndex">The index of Network Interface raising event</param>
        /// <param name="e">Event argument</param>
        //private static void NetworkChange_NetworkAPStationChanged(int NetworkIndex, NetworkAPStationEventArgs e)
        //{
        //    Debug.WriteLine($"NetworkAPStationChanged event Index:{NetworkIndex} Connected:{e.IsConnected} Station:{e.StationIndex} ");

        //    // if connected then get information on the connecting station 
        //    if (e.IsConnected)
        //    {
        //        WirelessAPConfiguration wapconf = WirelessAPConfiguration.GetAllWirelessAPConfigurations()[0];
        //        WirelessAPStation station = wapconf.GetConnectedStations(e.StationIndex);

        //        string macString = BitConverter.ToString(station.MacAddress);
        //        Debug.WriteLine($"Station mac {macString} Rssi:{station.Rssi} PhyMode:{station.PhyModes} ");

        //        _connectedCount++;

        //        // Start web server when it connects otherwise the bind to network will fail as 
        //        // no connected network. Start web server when first station connects 
        //        if (_connectedCount == 1)
        //        {
        //            // Wait for Station to be fully connected before starting web server
        //            // other you will get a Network error
        //            Thread.Sleep(2000);
        //            _server.Start();
        //        }
        //    }
        //    else
        //    {
        //        // Station disconnected. When no more station connected then stop web server
        //        if (_connectedCount > 0)
        //        {
        //            _connectedCount--;
        //            if (_connectedCount == 0)
        //                _server.Stop();
        //        }
        //    }

        //}
    }
}
