using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;
using System.Net;
using System.IO;

namespace PortiLog
{
    /// <summary>
    /// Defines the client of the move service.
    /// </summary>
    public class ServiceClient
    {
        const string Token = "/api/logging";

        public string Url { get; set; }

        public ServiceClient(string baseUrl)
        {
            Url = baseUrl + Token;
        }

        public async Task PostDumpData(DumpData dumpData)
        {
            await PostAsync<DumpData>(null, dumpData);   
        }

        static async Task<T> JsonDeserializeObjectAsync<T>(string sValue)
        {
            var result = await Task.Factory.StartNew<T>(() => { return JsonConvert.DeserializeObject<T>(sValue); });
            return result;
        }

        static async Task<string> JsonSerializeObjectAsync<T>(T value)
        {
            var result = await Task.Factory.StartNew<string>(() => { return JsonConvert.SerializeObject(value); });
            return result;
        }

        async Task PostAsync<PT>(string token, PT tPostData)
        {
            var postData = await JsonSerializeObjectAsync(tPostData);
            await PostAsync(token, postData);
        }

        async Task PostAsync(string token, string postData)
        {
            try
            {
                var decodedUrl = Uri.EscapeUriString(Url);
                var request = (HttpWebRequest) HttpWebRequest.Create(decodedUrl);
                request.Accept = "application/json";
                request.ContentType = "application/json";
                request.Method = "POST";

                byte[] data = Encoding.UTF8.GetBytes(postData);
                //request.ContentLength = data.Length;

                using (var requestStream = await Task<Stream>.Factory.FromAsync(request.BeginGetRequestStream, request.EndGetRequestStream, request))
                {
                    await requestStream.WriteAsync(data, 0, data.Length);
                }

                using (var responseObject = await Task<WebResponse>.Factory.FromAsync(request.BeginGetResponse, request.EndGetResponse, request))
                {
                    var responseStream = responseObject.GetResponseStream();
                    var sr = new StreamReader(responseStream);
                    string received = await sr.ReadToEndAsync();
                    return;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}