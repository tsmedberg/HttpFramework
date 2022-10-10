using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HttpFramework
{
    public class Server
    {
        private static TcpListener server;
        private static Dictionary<string, Dictionary<HttpMethods,HttpRequstHandler>> routes = new Dictionary<string, Dictionary<HttpMethods, HttpRequstHandler>>();
        public static void StartServer(IPEndPoint ipEndPoint)
        {
            server = new TcpListener(ipEndPoint);
            server.Start();
            Console.WriteLine("[INFO]\tListening on "+ ipEndPoint.ToString());
            WaitForClients();
        }
        private static void WaitForClients()
        {
            server.BeginAcceptTcpClient(new System.AsyncCallback(OnClientConnected), null);
        }
        private static void OnClientConnected(IAsyncResult asyncResult)
        {
            try
            {
                TcpClient client = server.EndAcceptTcpClient(asyncResult);
                if (client != null)
                    Console.WriteLine("Received connection request from: " + client.Client.RemoteEndPoint.ToString());
                HandleClientRequest(client);
            }
            catch
            {
                throw;
            }
            WaitForClients();
        }
        private static void HandleClientRequest(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            if (!stream.DataAvailable)
            {
                stream.Dispose();
                client.Dispose();
                Console.WriteLine("No data");
                return;
            }
            byte[] buffer = new byte[256];
            List<byte> totalBuffer = new List<byte>();
            while (stream.DataAvailable)
            {
                Console.WriteLine("[INFO]\tlisten loop");
                stream.Read(buffer, 0, buffer.Length);
                stream.Flush();
                totalBuffer.AddRange(buffer);
            }
            // #Region router
            HttpRequest req = new HttpRequest(totalBuffer.ToArray());
            HttpResponse res = new HttpResponse();
            Router(ref req, ref res);

            // #End Region
            stream.Write(res.ToBytes());

            stream.Close();
            client.Close();
            Console.WriteLine("wowee");
        }
        public static void Add(HttpMethods method, string path, HttpRequstHandler callback)
        {
            if (routes.ContainsKey(path))
            {
                routes[path].Add(method, callback);
                return;
            }
            routes.Add(path, new Dictionary<HttpMethods, HttpRequstHandler> { { method, callback } });
        }
        private static void Router(ref HttpRequest req, ref HttpResponse res)
        {
            string? route = FindRoute(req.path);
            if (route != null)
            {
                var currentRoute = routes[route];
                if (currentRoute.ContainsKey(req.method))
                {
                    //full match
                    currentRoute[req.method](ref req, ref res); //executes the callback
                    return;
                }
                //no method match
                res.Status(StatusCodes.MethodNotAllowed);
                res.Text("Could not " + req.method + " " + req.path);
                return;
            }
            res.Status(StatusCodes.NotFound);
            res.Text(req.path + " not found");
        }
        private static string? FindRoute(string path)
        {
            if(routes.ContainsKey(path)) return path;
            return null;
        }
    }
}
