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
using nanoFramework.Runtime.Native;

namespace ISCBTargetSystem
{
    public class WebServer
    {
        HttpListener _listener;
        Thread _serverThread;

        public void Start()
        {
            if (_listener == null)
            {
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
                    ProcessRequest(context);
            }
            _listener.Close();

            _listener = null;
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            string responseString="";
            string timeShown = null;
            string timeHidden = null;
            string repetitions = null;
            string countdown = null;
            string originator=null;
            bool isApSet = false;
            byte[] pageBytes;

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
                        responseString = ReplaceMessage(System.Text.Encoding.UTF8.GetString(pageBytes,0,pageBytes.Length), "");
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
                        timeHidden = (string)hashPars["timeHidden"];
                        repetitions = (string)hashPars["repetition"];
                        countdown = (string)hashPars["countdown"];

                        Debug.WriteLine($"Time targets are shown:" + timeShown);
                        Debug.WriteLine($"Time targets are hidden:" + timeHidden);
                        Debug.WriteLine($"Repetitions:" + repetitions);
                        Debug.WriteLine($"Countdown:" + countdown);

                        response.ContentType = "text/html";
                        pageBytes = Resources.GetBytes(Resources.BinaryResources.countdown);
                        responseString = ProcessCountdown(System.Text.Encoding.UTF8.GetString(pageBytes, 0, pageBytes.Length),countdown);
                        
                    }
                    else if (originator == "countdown")
                    {
                        responseString = "Target practice started";
                    }


                    //bool res = Wireless80211.Configure(ssid, password);
                    //if (res)
                    //{
                    //    message += $"<p>And your new IP address should be {Wireless80211.GetCurrentIPAddress()}.</p>";
                    //}                                           

                    //responseString = CreateMainPage(message);



                    OutPutResponse(response, responseString);
                    isApSet = true;
                    break;
            }

            response.Close();

            //if (isApSet && (!string.IsNullOrEmpty(ssid)) && (!string.IsNullOrEmpty(password)))
            //{
            //    // Enable the Wireless station interface
            //    Wireless80211.Configure(ssid, password);

            //    // Disable the Soft AP
            //    WirelessAP.Disable();
            //    Thread.Sleep(200);
            //    Power.RebootDevice();
            //}
        }

        private string ProcessCountdown(String page, string countdown)
        {
            StringBuilder stringBuilder = new StringBuilder(page);
            stringBuilder.Replace("{{countdownValue}}",countdown);
            return stringBuilder.ToString();
        }

        static string ReplaceMessage(string page, string message)
        {
            int index = page.IndexOf("{message}");
            if (index >= 0)
            {
                return page.Substring(0, index) + message + page.Substring(index + 9);
            }

            return page;
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
        static string CreateMainPage(string message)
        {

            return $"<!DOCTYPE html><html>{GetCss()}<body>" +
                    "<h1>NanoFramework</h1>" +
                    "<form method='POST'>" +
                    "<fieldset><legend>Wireless configuration</legend>" +
                    "Ssid:</br><input type='input' name='ssid' value='' ></br>" +
                    "Password:</br><input type='password' name='password' value='' >" +
                    "<br><br>" +
                    "<input type='submit' value='Save'>" +
                    "</fieldset>" +
                    "<b>" + message + "</b>" +
                    "</form></body></html>";
        }

        static string GetCss()
        {
            return "<head><meta charset=\"UTF-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"><style>" +
                "*{box-sizing: border-box}" +
                "h1,legend {text-align:center;}" +
                "form {max-width: 250px;margin: 10px auto 0 auto;}" +
                "fieldset {border-radius: 5px;box-shadow: 3px 3px 15px hsl(0, 0%, 90%);font-size: large;}" +
                "input {width: 100%;padding: 4px;margin-bottom: 8px;border: 1px solid hsl(0, 0%, 50%);border-radius: 3px;font-size: medium;}" +
                "input[type=submit]:hover {cursor: pointer;background-color: hsl(0, 0%, 90%);transition: 0.5s;}" +
                " @media only screen and (max-width: 768px) { form {max-width: 100%;}} " +
                "</style><title>NanoFramework</title></head>";
        }
    }
}
