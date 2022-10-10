using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json.Nodes;

namespace HttpFramework
{
    class MainApp
    {
        public static void Main()
        {
            try
            {
                Server.Add(HttpMethods.GET,"/",delegate(ref HttpRequest req,ref HttpResponse res){
                    //status 200 is default
                    res.Text("Hej världen!");
                });
                Server.Add(HttpMethods.GET, "/hell", delegate (ref HttpRequest req, ref HttpResponse res) {
                    res.Text("Welcome to hell 🔥");
                });
                Server.Add(HttpMethods.GET, "/ua", delegate (ref HttpRequest req, ref HttpResponse res) {
                    res.Text("Your user agent string is\n" + req.headers["User-Agent"]);
                });
                Server.StartServer(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 3000));
                //to stop program from exiting
                Console.Read();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
    }
}