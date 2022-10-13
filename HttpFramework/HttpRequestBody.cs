using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpFramework
{
    public class HttpRequestBody
    {
        byte[]? body;
        public string contentType;

        public HttpRequestBody(HttpRequest req, byte[] body)
        {
            this.body = body;
            this.contentType = req.headers["Content-Type"];
        }
        public byte[] Raw()
        {
            if (this.body == null)
                throw new ArgumentNullException("body is null");
            return body;
        }
        public string Text()
        {
            if (this.body == null)
                throw new ArgumentNullException("body is null");
            return Encoding.UTF8.GetString(body);
        }
        public Dictionary<string, MultiPartData> MultiPart()
        {
            if (!contentType.Contains("multipart/form-data"))
            {
                throw new ArgumentException("data is not multipart form");
            }
            Dictionary<string, MultiPartData> result = new Dictionary<string, MultiPartData>();
            string boundary = contentType.Split("boundary=")[1];
            foreach(string part in this.Text().Split("--"+boundary))
            {
                List<string> lines = part.Split("\r\n").ToList();
                lines.RemoveAt(0);
                if(lines.Count() < 4)
                    continue;
                string contentDisposition = lines[0];
                string data = lines.GetRange(lines.IndexOf("")+1,lines.Count()-lines.IndexOf("")-2).Aggregate((i, j) => i + "\r\n" + j).ToString();
                List<string> contentDispositionParts = contentDisposition.Split(";").ToList();
                string name = contentDispositionParts.Find(x => x.Contains("name=")).Split("name=")[1].Replace("\"", "");
                string? filename = contentDispositionParts.Find(x => x.Contains("filename="));
                if(filename != null)
                {
                    filename = filename.Split("filename=")[1].Replace("\"", "");
                }
                string? contentType = lines.Find(x => x.Contains("Content-Type"));
                if(contentType != null)
                {
                    contentType = contentType.Split(": ")[1];
                }
                result.Add(name, new MultiPartData(filename, contentType, data));
            }
            return result;
        }
        public Dictionary<string, string> UrlEncoded()
        {
            if (!contentType.Contains("application/x-www-form-urlencoded"))
            {
                throw new ArgumentException("data is not urlencoded form");
            }
            return HttpRequest.UrlParamDecode(this.Text());
        }
    }
    public class MultiPartData
    {
        public string? Filename = null;
        public string? ContentType = null;
        public string Data;
        
        public MultiPartData(string? filename, string? contenttype, string data)
        {
            Filename = filename;
            ContentType = contenttype;
            Data = data;
        }

    }
}
