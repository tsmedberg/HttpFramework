using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
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
                    Console.WriteLine("[INFO]\tconnection request from " + client.Client.RemoteEndPoint.ToString());
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
                Console.WriteLine("[DEBUG]\tlisten loop");
                stream.Read(buffer, 0, buffer.Length);
                stream.Flush();
                totalBuffer.AddRange(buffer);
            }
            // #Region router
            HttpRequest req = new HttpRequest(totalBuffer.ToArray());
            HttpResponse res = new HttpResponse();
            try
            {
                Router(ref req, ref res);
            }
            catch(Exception e)
            {
                res.Status(StatusCodes.InternalServerError);
                res.Text(e.ToString());
                PrintResponseInfo(req, res);
                Console.WriteLine(e.ToString());
            }

            // #End Region
            stream.Write(res.ToBytes());

            stream.Close();
            client.Close();
            Console.WriteLine("[INFO]\tClosing client");
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
                if(IsDynamic(route))
                {
                    req.urlParameters = ExtractUrlParameters(req.path, route);
                }
                var currentRoute = routes[route];
                if (currentRoute.ContainsKey(req.method) || currentRoute.ContainsKey(HttpMethods.ALL))
                {
                    HttpMethods method = req.method;
                    if(!currentRoute.ContainsKey(method))
                    {
                        method = HttpMethods.ALL;
                    }
                    //full match
                    currentRoute[method](ref req, ref res); //executes the callback
                    PrintResponseInfo(req, res);
                    return;
                }
                else
                {
                    //no method match
                    res.Status(StatusCodes.MethodNotAllowed);
                    res.Text("Could not " + req.method + " " + req.path);
                    PrintResponseInfo(req, res);
                    return;
                }
            }
            res.Status(StatusCodes.NotFound);
            res.Text(req.path + " not found");
            PrintResponseInfo(req, res);
        }
        private static string? FindRoute(string path)
        {
            if(routes.ContainsKey(path)) return path;
            string? route = RegexRoutes(path);
            if (route != null)
                return route;
            return null;
        }
        private static string? RegexRoutes(string path)
        {
            foreach (string key in routes.Keys)
            {
                Regex rx = ParseDymanicRoute(key);
                if (rx.IsMatch(path))
                    return key;
            }
            return null;
        }
        private static Regex ParseDymanicRoute(string route)
        {
            Regex urlParams = new Regex(@":[a-zA-Z0-9]+");
            return new Regex("^"+urlParams.Replace(route, delegate (Match m)
            {
                return "[a-zA-Z0-9]+";
            })+"$");

        }
        private static Dictionary<string,string> ExtractUrlParameters(string path, string route)
        {
            Dictionary<string, string> urlParams = new Dictionary<string, string>();
            string[] pathParts = path.Split("/");
            List<string> routeParts = route.Split("/").ToList();
            Regex rx = new Regex(@":[a-zA-Z0-9]+");
            foreach (string rp in routeParts)
            {
                if(rx.IsMatch(rp))
                {
                    urlParams.Add(rp.Replace(":", ""), pathParts[routeParts.IndexOf(rp)]);
                }
            }
            return urlParams;
        }
        private static bool IsDynamic(string route)
        {
            return new Regex(@":[a-zA-Z0-9]+").IsMatch(route);
        }
        private static void PrintResponseInfo(HttpRequest req, HttpResponse res)
        {
            ConsoleColor defaultColor = Console.ForegroundColor;
            ConsoleColor color = defaultColor;
            if((int)res.statusCode < 200)
            {
                color = ConsoleColor.Blue;
            }
            else if((int)res.statusCode < 300)
            {
                color = ConsoleColor.Green;
            }
            else if ((int)res.statusCode < 400)
            {
                color = ConsoleColor.Gray;
            }
            else if ((int)res.statusCode < 500)
            {
                color = ConsoleColor.Yellow;
            }
            else if ((int)res.statusCode < 600)
            {
                color = ConsoleColor.Red;
            }
            Console.Write(DateTime.Now.ToString()+$"\t {req.method}\t {req.path}\t ");
            Console.ForegroundColor = color;
            Console.WriteLine((int)res.statusCode);
            Console.ForegroundColor = defaultColor;
        }
    }
}
