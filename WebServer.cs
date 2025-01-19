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
                        responseString = System.Text.Encoding.UTF8.GetString(pageBytes,0,pageBytes.Length);
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
                        var abort = (string)hashPars["abort"];
                        if(abort==null)
                        {
                            response.ContentType = "text/html";
                            pageBytes = Resources.GetBytes(Resources.BinaryResources.shooting);
                            responseString = System.Text.Encoding.UTF8.GetString(pageBytes, 0, pageBytes.Length);
                        }
                        else
                        {
                            response.ContentType = "text/html";
                            pageBytes = Resources.GetBytes(Resources.BinaryResources.mainPage);
                            responseString = System.Text.Encoding.UTF8.GetString(pageBytes, 0, pageBytes.Length);
                        }
                    }
                    else if (originator == "shooting")
                    {
                        response.ContentType = "text/html";
                        pageBytes = Resources.GetBytes(Resources.BinaryResources.mainPage);
                        responseString = System.Text.Encoding.UTF8.GetString(pageBytes, 0, pageBytes.Length);
                    }

                    OutPutResponse(response, responseString);

                    break;
            }

            response.Close();

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
