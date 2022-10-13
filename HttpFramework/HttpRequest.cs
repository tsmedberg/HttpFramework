using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Web;

namespace HttpFramework
{
    public enum HttpMethods
    {
        ALL,
        GET,
        HEAD,
        POST,
        PUT,
        DELETE,
        CONNECT,
        OPTIONS,
        TRACE,
        PATCH
    }
    public class HttpRequest
    {
        public Dictionary<string, string> headers = new Dictionary<string, string>();
        public Dictionary<string, string> queryParameters = new Dictionary<string, string>();
        public Dictionary<string, string> urlParameters = new Dictionary<string, string>();
        public string path;
        public HttpMethods method;
        public HttpRequestBody body;
        private string httpVersion;

        public HttpRequest(byte[] data)
        {
            int bodyIndex = findBodyStart(data);
            byte[]? tempBody = null;
            byte[] byteheaders = new byte[bodyIndex < 0 ? data.Length : bodyIndex];
            string headers;
            if (bodyIndex > -1)
            {
                Buffer.BlockCopy(data, 0, byteheaders, 0, bodyIndex);
                headers = Encoding.UTF8.GetString(byteheaders);
                tempBody = new byte[data.Length];
                Buffer.BlockCopy(data, bodyIndex+4, tempBody, 0, data.Length-bodyIndex-4);
                //this.body = new HttpRequestBody(this,tempBody);
            }
            else
            {
                headers = Encoding.UTF8.GetString(data);
            }
            string methodString = headers.Split(" ")[0];
            bool wasParsed = Enum.TryParse(methodString,true,out HttpMethods method);
            if (method == HttpMethods.ALL) // The method ALL is for internal use only, and should not be accepted in an incoming request
                wasParsed = false;
            if (!wasParsed)
            {
                throw new ArgumentException(methodString + " method not implemented");
            }
            this.method = method;
            this.path = headers.Split(" ")[1];

            //has query parameters
            if(this.path.Contains("?"))
            {
                string queryParts = this.path.Split("?")[1];
                this.path = UrlDecode(this.path.Split("?")[0]); // do url decoding at this stage to stop weird characters from becoming a problem
                this.queryParameters = UrlParamDecode(queryParts);
            }

            this.httpVersion = headers.Split(" ")[2];
            List<string> headerArray = headers.Split("\r\n").ToList();
            headerArray.RemoveAt(0);
            try
            {
                foreach (string row in headerArray)
                {
                    if(row == "")
                    {
                        continue;
                    }
                    string[] h = row.Split(": ");
                    this.headers.Add(h[0], h[1]);
                }
            }
            catch
            {
                throw new ArgumentException("Malformed headers");
            }
            //minimize the body array to the content length set in headers
            if(this.headers.ContainsKey("Content-Length"))
            {
                int length = int.Parse(this.headers["Content-Length"]);
                if (length < 0 || length > tempBody.Length)
                    throw new ArgumentException("Content-Length was out of range");
                byte[] newBody = new byte[length];
                Buffer.BlockCopy(tempBody, 0, newBody, 0, length);
                this.body = new HttpRequestBody(this,newBody);

            }
        }
        private int findBodyStart(byte[] data)
        {
            byte[] pattern = Encoding.UTF8.GetBytes("\r\n\r\n");
            int i = 0;
            while (true)
            {
                int ti = Array.IndexOf(data, pattern[0], i+1);
                if( ti == -1)
                    break;
                if (ti == i)
                    break;
                if (data[ti+1] == pattern[1] && data[ti+2] == pattern[2] && data[ti+3] == pattern[3])
                {
                    //found match
                    return ti;
                }
                i = ti;
            }
            return -1;
            
        }

        public static Dictionary<string,string> UrlParamDecode(string queryParts)
        {
            string[] parts;
            Dictionary<string, string> result = new Dictionary<string, string>();
            if (queryParts.Contains("&"))
            {
                parts = queryParts.Split("&");
            }
            else
            {
                parts = new string[] { queryParts };
            }
            foreach (string part in parts)
            {
                try
                {
                    string key = part.Split("=")[0];
                    string value = part.Split("=")[1];
                    key = UrlDecode(key);
                    value = UrlDecode(value);
                    result.Add(key, value);
                }
                catch (Exception e)
                {
                    Console.WriteLine("[ERROR]\tWhen parsing query parameters");
                    Console.WriteLine(e.ToString());
                    continue;
                }
            }
            return result;
        }
        public static string UrlDecode(string input)
        {
            int index = -1;
            string result = "";
            int continueAfter = 0;
            List<int> percentCharacters = new List<int>();
            while(true)
            {
                index = input.IndexOf("%", index+1);
                if (index == -1) break;
                percentCharacters.Add(index);
            }

            for(int i = 0; i < input.Length; i++)
            {
                if(i < continueAfter)
                {
                    continue;
                }
                char c = input[i];
                if(percentCharacters.Contains(i))
                {
                    c = (char)Convert.ToInt32(input.Substring(i + 1, 2), 16);
                    continueAfter = i + 3;
                }
                result += c;
            }
            return result;
        }
    }
}
