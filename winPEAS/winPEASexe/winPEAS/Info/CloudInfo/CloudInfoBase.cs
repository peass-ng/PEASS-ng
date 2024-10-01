using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace winPEAS.Info.CloudInfo
{
    internal abstract class CloudInfoBase
    {
        public abstract string Name { get; }
        
        public abstract bool IsCloud { get; }

        public abstract Dictionary<string, List<EndpointData>> EndpointDataList();

        public abstract bool TestConnection();

        private bool? _isAvailable;
        public bool IsAvailable
        {
            get
            {
                if (_isAvailable == null)
                {
                    _isAvailable = TestConnection();
                }

                return _isAvailable.Value;
            }
        }

        protected string CreateMetadataAPIRequest(string url, string method, WebHeaderCollection headers = null)
        {
            try
            {
                var request = WebRequest.CreateHttp(url);

                if (headers != null)
                {
                    request.Headers = headers;
                }
                
                request.Method = method;

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    using (var responseStream = response.GetResponseStream())
                    {
                        // Get a reader capable of reading the response stream
                        using (var myStreamReader = new StreamReader(responseStream, Encoding.UTF8))
                        {
                            // Read stream content as string
                            var content = myStreamReader.ReadToEnd();

                            return content;
                        }
                    }
                }
            }
            catch (WebException exception)
            {
                if (exception.InnerException != null)
                {
                    return typeof(SocketException) == exception.InnerException.GetType() ? null : string.Empty; 
                }
            }
            catch (Exception ex)
            {
                return string.Empty;
            }

            return string.Empty;
        }
    }
}
