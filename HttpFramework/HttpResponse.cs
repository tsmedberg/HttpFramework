using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace HttpFramework
{
    public enum StatusCodes
    {
        [Description("OK")]
        OK =200,
        [Description("Not Found")]
        NotFound=404,
        [Description("Internal Server Error")]
        InternalServerError=500,
        [Description("Moved Permanently")]
        MovedPermanently=301,
        [Description("Method Not Allowed")]
        MethodNotAllowed=405,

    }
    public class HttpResponse
    {
        StatusCodes statusCode = StatusCodes.OK;
        public Dictionary<string, string> headers = new Dictionary<string, string>();
        public byte[]? body;
        public HttpResponse()
        {
        }
        public byte[] ToBytes()
        {
            string responseHeaderString = $"HTTP/1.1 {(int)this.statusCode} {this.statusCode.ToString()}\r\n";
            foreach(KeyValuePair<string, string> kvp in headers)
            {
                responseHeaderString += $"{kvp.Key}: {kvp.Value}\r\n";
            }
            responseHeaderString += "\r\n"; //before body
            byte[] responseHeader = Encoding.UTF8.GetBytes(responseHeaderString);
            byte[] response = new byte[responseHeader.Length + (this.body != null ? this.body.Length : 0)];
            Buffer.BlockCopy(responseHeader,0,response,0,responseHeader.Length);
            if(this.body != null)
            {
                Buffer.BlockCopy(this.body, 0, response, responseHeader.Length, this.body.Length);
            }
            return response;
        }
        public void Status(StatusCodes method)
        {
            this.statusCode = method;
        }
        public void Text(string text)
        {
            this.headers.Add("Content-Type", "text/plain;charset=utf8");
            this.body = Encoding.UTF8.GetBytes(text);
        }
    }
}
