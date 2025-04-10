using nanoFramework.Networking;
using nanoFramework.WebServer;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Threading;
using System.IO;
using System.Text;

namespace _0.ValidatePrerequisites
{
    public class Program
    {
        private static string MySsid = "dropitlikeaSquat";
        private static string MyPassword = "DaisyToddAndButt";

        public static void Main()
        {
            Debug.WriteLine("Attempting to connect to network with previously saved credentials...");

            bool success;
            CancellationTokenSource cs = new(60000);
            success = WifiNetworkHelper.Reconnect(requiresDateTime: true, token: cs.Token);

            if (!success)
            {
                Debug.WriteLine("Couldn't connect with previously saved credentials. Attempting to connect with SSID and password");
                success = WifiNetworkHelper.ConnectDhcp(MySsid, MyPassword, requiresDateTime: true, token: cs.Token);

                if (!success)
                {
                    Debug.WriteLine($"Can't get a proper IP address and DateTime, error: {NetworkHelper.Status}.");
                    if (NetworkHelper.HelperException != null)
                    {
                        Debug.WriteLine($"Exception: {NetworkHelper.HelperException}");
                    }
                    return;
                }
            }

            var ipaddres = IPGlobalProperties.GetIPAddress();
            Debug.WriteLine($"Connected with IP Address: {ipaddres.ToString()}");

            // Prepare filesystem
            FileController.MountSDCard();

            using (WebServer server = new WebServer(80, HttpProtocol.Http, new Type[] { typeof(HomeController), typeof(FileController) }))
            {
                server.CommandReceived += ServerCommandReceived;
                server.Start();
                Thread.Sleep(Timeout.Infinite);
            }
        }

        private static void ServerCommandReceived(object source, WebServerEventArgs e)
        {
            try
            {
                var url = e.Context.Request.RawUrl;
                Debug.WriteLine($"Command received: {url}, Method: {e.Context.Request.HttpMethod}");

                var fileName = url.Substring(1);
                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = "index.html";
                }
                else
                {
                    var stringBuilder = new StringBuilder(fileName);
                    stringBuilder.Replace("/", "\\");

                    fileName = stringBuilder.ToString();
                }

                Debug.WriteLine($"Request file: '{fileName}'");

                WebServer.SendFileOverHTTP(e.Context.Response, FileController.DirectoryPath + fileName, GetContentTypeFromFileName(fileName));
                return;
            }
            catch (IOException)
            {
                WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.NotFound);
            }
            catch (Exception)
            {
                WebServer.OutputHttpCode(e.Context.Response, HttpStatusCode.InternalServerError);
            }
        }

        private static string GetContentTypeFromFileName(string filename)
        {
            if (filename.EndsWith(".js"))
            {
                return "text/javascript";
            }

            return "";
        }


    }
}