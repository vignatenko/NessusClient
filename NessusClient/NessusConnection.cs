using System;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NessusClient
{
    public class NessusConnection : INessusConnection
    {
        private readonly string _server;

        /// <summary>Get Nessus server port</summary>
        /// <value> Nessus server port </value>
        private readonly int _port;

        /// <summary>Get Nessus username</summary>
        /// <value> Nessus username </value>
        private readonly string _userName;

        private readonly SecureString _password;

        /// <summary>Get Nessus session token</summary>
        /// <value> Nessus session token </value>
        private string _token;

        public int RequestTimeoutMillis { get; set; }

        public bool SkipServerCertificateValidation { get; set; } = true;

        public NessusConnection(string server, int port, string userName, SecureString password)
        {
            _server = server;
            _port = port;
            _userName = userName;
            _password = password;
            RequestTimeoutMillis = 10 * 60 * 1000;
        }


        public async Task OpenAsync(CancellationToken cancellationToken)
        {
            var r = CreateUnauthorizedRequest("session", WebRequestMethods.Http.Post);
            var bytes = Encoding.UTF8.GetBytes($"{{\"username\": \"{_userName}\", \"password\": \"");
            const string bodySuffix = "\"}";

            r.ContentLength = bytes.Length + _password.Length + bodySuffix.Length;
            
            using (var rs = await r.GetRequestStreamAsync())
            {
                
                await rs.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
            
                bytes = _password.ToBytes();
                
                try
                {                    
                    await rs.WriteAsync(bytes, 0, bytes.Length, cancellationToken);            
                }
                finally
                {
                    Array.Clear(bytes, 0, bytes.Length);                
                }

                bytes = Encoding.UTF8.GetBytes(bodySuffix);
                await rs.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
            
                await rs.FlushAsync(cancellationToken);
            
            }
            
            using (var response = await r.GetResponseAsync())
            {
                using (var responseStream = response.GetResponseStream())
                {
                    var js = new DataContractJsonSerializer(typeof(NessusSessionToken));
                    var obj = (NessusSessionToken) js.ReadObject(responseStream);
                    _token = obj.Token;
                }
            }            

        }

        public async Task CloseAsync()
        {
            if (string.IsNullOrWhiteSpace(_token))
                return;

            var request = CreateRequest("session", "DELETE");
            _token = null;
            await request.GetResponseAsync();
        }

        public WebRequest CreateRequest(string relativeEndpointUrl, string httpMethod = "GET")
        {           
            var webRequest = CreateUnauthorizedRequest(relativeEndpointUrl, httpMethod);
            
            webRequest.Headers["X-Cookie"] = $"token={_token}";

            webRequest.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            
            return webRequest;
        }

        private HttpWebRequest CreateUnauthorizedRequest(string relativeEndpointUrl, string httpMethod)
        {
            var webRequest = (HttpWebRequest)WebRequest.Create(new UriBuilder(Uri.UriSchemeHttps, _server, _port, relativeEndpointUrl).Uri);
            webRequest.Accept = "application/json, text/javascript, */*; q=0.01";
            webRequest.ContentType = "application/json";
            webRequest.Headers[HttpRequestHeader.AcceptEncoding] = "gzip, deflate";
            webRequest.Headers[HttpRequestHeader.AcceptLanguage] = "en-US,en;q=0.8";            

            webRequest.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            webRequest.Timeout = RequestTimeoutMillis;

            webRequest.Method = httpMethod;
            if (SkipServerCertificateValidation)
                webRequest.ServerCertificateValidationCallback = (sender, certificate, chain, errors) => true;
            return webRequest;
        }

        protected virtual void Dispose(bool disposing)
        {
            
            if (disposing)
            {
                CloseAsync().Wait();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~NessusConnection()
        {
            Dispose(false);
        }
        [DataContract]
        private class NessusSessionToken
        {
            [DataMember(Name = "token")]
            public string Token { get; set; }
        }
    }
}