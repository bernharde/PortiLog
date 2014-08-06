using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Net.Http.Headers;

namespace PortiLog.Service.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public async Task TestMethod1()
        {
            DumpData dumpData = new DumpData();
            dumpData.DeviceId = "abcdeviceid";
            dumpData.ApplicationName = "service2test";
            dumpData.ApplicationVersion = "0.1";
            dumpData.Entries = new System.Collections.Generic.List<Entry>();
            dumpData.Entries.Add(new Entry() { Id = 1, Created = DateTime.Now, Message = "my message123" });
            await PostAsync<object, DumpData>(null, dumpData);        
        }

        public static async Task<T> JsonDeserializeObjectAsync<T>(string sValue)
        {
            var result = await Task.Factory.StartNew<T>(() => { return JsonConvert.DeserializeObject<T>(sValue); });
            return result;
        }

        public static async Task<string> JsonSerializeObjectAsync<T>(T value)
        {
            var result = await Task.Factory.StartNew<string>(() => { return JsonConvert.SerializeObject(value); });
            return result;
        }

        public async Task<T> PostAsync<T, PT>(string token, PT tPostData)
        {
            var postData = await JsonSerializeObjectAsync(tPostData);
            var result = await PostAsync<T>(token, postData);
            return result;
        }

        public async Task<T> PostAsync<T>(string token, string postData)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    //var url = await CreateUrlAsync(token);
                    var url = "http://localhost/logging2/api/logging";
                    
                    var decodedUrl = Uri.EscapeUriString(url);
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
                //Logger.Error("ServerService_PostAsync failed", ex);
                //throw new Models.ServerException(strings.ServerError, ex);
            }
        }
    }
}
