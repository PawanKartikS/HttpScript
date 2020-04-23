using System;
using System.IO;
using System.Net;

namespace Nebula
{
    internal static class Api
    {
        public static Tuple<string, HttpStatusCode> ReadResponse(string endpoint, string method, long timeout)
        {
            var request = (HttpWebRequest) WebRequest.Create(endpoint);
            
            request.Method = method;
            request.KeepAlive = true;
            request.Timeout = (int) timeout;
            
            var response = (HttpWebResponse) request.GetResponse();
            var stream = response.GetResponseStream();

            return stream != null
                ? new Tuple<string, HttpStatusCode>(new StreamReader(stream).ReadToEnd(), response.StatusCode)
                : new Tuple<string, HttpStatusCode>(null, HttpStatusCode.Unused);
        }
    }
}