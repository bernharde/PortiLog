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
            var client = new ServiceClient("http://localhost/logging2");
            await client.PostDumpData(dumpData);     
        }

       
    }
}
