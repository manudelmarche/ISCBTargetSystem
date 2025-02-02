//
// Copyright (c) .NET Foundation and Contributors
// See LICENSE file in the project root for full license information.
// Modified by E. DELMARCHE
//

using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Net.WebSockets;
using nanoFramework.Runtime.Native;
using System.Net.WebSockets.Server;
using System.Net.Http;
using Iot.Device.MulticastDns.Entities;

namespace ISCBTargetSystem
{
    public class WebServer
    {
        HttpListener _listener;
        Thread _serverThread;
        WebSocketServer _ws;

        public void Start(WebSocketServer ws)
        {
            if (_listener == null)
            {
                _ws = ws;
                _listener = new HttpListener("http");
                _serverThread = new Thread(RunServer);
                _serverThread.Start();
            }
        }

        public void Stop()
        {
            if (_listener != null)
                _listener.Stop();
        }
        private void RunServer()
        {
            _listener.Start();

            while (_listener.IsListening)
            {
                var context = _listener.GetContext();
                if (context != null)
                {
                    if (ProcessRequest(context))
                    {
                        //HttpClient client = new HttpClient();
                        //client.BaseAddress = new Uri("http://192.168.4.1");
                        //string formData = "Originator=Shooting";
                        //byte[] formDataBytes = Encoding.UTF8.GetBytes(formData);
                        //HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "http://192.168.4.1")
                        //{
                        //    Content = new ByteArrayContent(formDataBytes)
                        //};
                        //request.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                        //HttpResponseMessage response = client.Send(request);

                    }

                }
            }
            _listener.Close();

            _listener = null;
        }

        private bool ProcessRequest(HttpListenerContext context, bool isShooting = false)
        {
            var request = context.Request;
            var response = context.Response;
            string responseString = "";
            string timeShown = null;
            string timeHidden = null;
            string repetitions = null;
            string countdown = null;
            string originator = null;
            byte[] pageBytes;
            const int defaultTimeVisible = 3;
            const int defaultTimeHidden = 7;
            const int defaultRepetitions = 10;
            const int defaultCountdown = 10;

            Thread shootingLoop = new Thread(() => Shoot(_ws));

            try
            {
                switch (request.HttpMethod)
                {
                    case "GET":
                        string[] url = request.RawUrl.Split('?');
                        if (url[0] == "/favicon.ico")
                        {
                            response.ContentType = "image/png";
                            byte[] responseBytes = Resources.GetBytes(Resources.BinaryResources.favicon);
                            OutPutByteResponse(response, responseBytes);
                        }
                        else
                        {
                            response.ContentType = "text/html";
                            pageBytes = Resources.GetBytes(Resources.BinaryResources.mainPage);
                            responseString = ProcessMainPage(System.Text.Encoding.UTF8.GetString(pageBytes, 0, pageBytes.Length), defaultTimeVisible, defaultTimeHidden, defaultRepetitions, defaultCountdown);
                            OutPutResponse(response, responseString);
                        }
                        break;

                    case "POST":
                        // Pick up POST parameters from Input Stream
                        Hashtable hashPars = ParseParamsFromStream(request.InputStream);
                        originator = (string)hashPars["originator"];

                        if (originator == "main")
                        {
                            timeShown = (string)hashPars["timeShown"];
                            ShootingData.timeVisible = Int32.Parse(timeShown);

                            timeHidden = (string)hashPars["timeHidden"];
                            ShootingData.timeHidden = Int32.Parse(timeHidden);

                            repetitions = (string)hashPars["repetition"];
                            ShootingData.repetitions = Int32.Parse(repetitions);

                            countdown = (string)hashPars["countdown"];
                            ShootingData.countdown = Int32.Parse(countdown);

                            Debug.WriteLine($"Time targets are shown:" + timeShown);
                            Debug.WriteLine($"Time targets are hidden:" + timeHidden);
                            Debug.WriteLine($"Repetitions:" + repetitions);
                            Debug.WriteLine($"Countdown:" + countdown);

                            response.ContentType = "text/html";
                            pageBytes = Resources.GetBytes(Resources.BinaryResources.countdown);
                            responseString = ProcessCountdown(System.Text.Encoding.UTF8.GetString(pageBytes, 0, pageBytes.Length), countdown);
                            isShooting = false;

                        }
                        else if (originator == "countdown")
                        {
                            var abort = (string)hashPars["abort"];
                            if (abort == null)
                            {
                                response.ContentType = "text/html";
                                pageBytes = Resources.GetBytes(Resources.BinaryResources.shooting);
                                responseString = System.Text.Encoding.UTF8.GetString(pageBytes, 0, pageBytes.Length);
                                isShooting = true;
                            }
                            else
                            {
                                response.ContentType = "text/html";
                                pageBytes = Resources.GetBytes(Resources.BinaryResources.mainPage);
                                responseString = ProcessMainPage(System.Text.Encoding.UTF8.GetString(pageBytes, 0, pageBytes.Length), ShootingData.timeVisible, ShootingData.timeHidden, ShootingData.repetitions, ShootingData.countdown);
                                isShooting = false;
                            }
                        }
                        else if (originator == "shooting")
                        {
                            var abort = (string)hashPars["abort"];
                            if (abort!=null)
                            {   
                                //If the user aborts the shooting session, we cancel the shooting loop and reset the targets to visible position
                                shootingLoop.Abort();
                                //TODO: code to show targets
                            }
                            response.ContentType = "text/html";
                            pageBytes = Resources.GetBytes(Resources.BinaryResources.mainPage);
                            responseString = ProcessMainPage(System.Text.Encoding.UTF8.GetString(pageBytes, 0, pageBytes.Length), ShootingData.timeVisible, ShootingData.timeHidden, ShootingData.repetitions, ShootingData.countdown);
                        }

                        OutPutResponse(response, responseString);
                        break;
                }


                response.Close();

                Thread.Sleep(1000);

                if (isShooting)
                {
                    shootingLoop.Start();
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }            
            return isShooting;

        }

        private void Shoot(WebSocketServer ws)
        {
            bool isVisible = true;
            for (int i = 0; i < ShootingData.repetitions; i++)
            {
                //TODO: code to show targets

                //currentRep#maxReps#isVisible
                ws.BroadCast((i + 1).ToString() + "#" + ShootingData.repetitions.ToString() + "#" + (isVisible ? "visible" : "hidden"));
                Thread.Sleep(ShootingData.timeVisible * 1000);

                //TODO: code to hide targets
                isVisible = false;
                ws.BroadCast((i + 1).ToString() + "#" + ShootingData.repetitions.ToString() + "#" + (isVisible ? "visible" : "hidden"));
                Thread.Sleep(ShootingData.timeHidden * 1000);
                isVisible = true;
            }
            ws.BroadCast("Shooting finished");
            //TODO: code to show targets
        }

        private string ProcessMainPage(string page, int defaultTimeVisible, int defaultTimeHidden, int defaultRepetitions, int defaultCountdown)
        {
            StringBuilder stringBuilder = new StringBuilder(page);
            stringBuilder.Replace("{{timeVisible}}", defaultTimeVisible.ToString());
            stringBuilder.Replace("{{timeHidden}}",defaultTimeHidden.ToString());
            stringBuilder.Replace("{{repetitions}}",defaultRepetitions.ToString());
            stringBuilder.Replace("{{countdown}}",defaultCountdown.ToString());
            return stringBuilder.ToString();
        }

        private string ProcessCountdown(String page, string countdown)
        {
            StringBuilder stringBuilder = new StringBuilder(page);
            stringBuilder.Replace("{{countdownValue}}",countdown);
            return stringBuilder.ToString();
        }


        static void OutPutResponse(HttpListenerResponse response, string responseString)
        {
            var responseBytes = System.Text.Encoding.UTF8.GetBytes(responseString);
            OutPutByteResponse(response, System.Text.Encoding.UTF8.GetBytes(responseString));
        }
        static void OutPutByteResponse(HttpListenerResponse response, Byte[] responseBytes)
        {
            response.ContentLength64 = responseBytes.Length;
            response.OutputStream.Write(responseBytes, 0, responseBytes.Length);

        }

        static Hashtable ParseParamsFromStream(Stream inputStream)
        {
            byte[] buffer = new byte[inputStream.Length];
            inputStream.Read(buffer, 0, (int)inputStream.Length);

            return ParseParams(System.Text.Encoding.UTF8.GetString(buffer, 0, buffer.Length));
        }

        static Hashtable ParseParams(string rawParams)
        {
            Hashtable hash = new Hashtable();

            string[] parPairs = rawParams.Split('&');
            foreach (string pair in parPairs)
            {
                string[] nameValue = pair.Split('=');
                hash.Add(nameValue[0], nameValue[1]);
            }

            return hash;
        }
    }
}
