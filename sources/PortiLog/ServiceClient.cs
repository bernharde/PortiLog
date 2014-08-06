using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;

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
            await PostAsync<object, DumpData>(null, dumpData);   
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

        async Task<T> PostAsync<T, PT>(string token, PT tPostData)
        {
            var postData = await JsonSerializeObjectAsync(tPostData);
            var result = await PostAsync<T>(token, postData);
            return result;
        }

        async Task<T> PostAsync<T>(string token, string postData)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var decodedUrl = Uri.EscapeUriString(Url);
                    StringContent postContent = new StringContent(postData, Encoding.UTF8, "application/json");
                    using (var response = await client.PostAsync(decodedUrl, postContent))
                    {
                        response.EnsureSuccessStatusCode();
                        var content = await response.Content.ReadAsStringAsync();
                        var value = await JsonDeserializeObjectAsync<T>(content);
                        return value;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}