using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Windows.Data.Json;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using Windows.ApplicationModel;

namespace PortiLog.WindowsStore.Test
{
    [TestClass]
    public class UnitTest
    {
        [TestMethod]
        public async Task Store_WriteSingleLogEntryAsync()
        {
            Logger.Info("Writing a test entry");
            Logger.Engine.Flush();
            await Logger.Engine.DumpAsync();
        }

        [TestMethod]
        public async Task Store_WriteSingleLogEntryWithCategoryAsync()
        {
            Logger.WriteEntry(new Entry() { Level = PortiLog.Level.Error, Category = "Common", Id = 5, Message = "Direct entry" });
            Logger.Engine.Flush();
            await Logger.Engine.DumpAsync();
        }

        [TestMethod]
        public async Task Store_Write10000LogEntriesAsync()
        {
            for (int i = 0; i < 10000; i++)
            {
                Logger.Info(string.Format("Writing a test entry: {0}", i));
            }
            Logger.Engine.Flush();
            await Logger.Engine.DumpAsync();
        }

        [TestMethod]
        public void Store_Write1000LogEntriesWithoutDump()
        {
            int start = Environment.TickCount;
            for (int i = 0; i < 1000; i++)
            {
                Logger.Info(string.Format("Writing a test entry: {0}", i));
            }
            Logger.Engine.Flush();
        }

        [TestMethod]
        public async Task Store_CategoryFilterTest()
        {
            var engine = new WindowsStore.Engine();
            var trace = new TraceListener("Store_CategoryFilterTest");
            engine.RegisterListener(trace);

            var configuration = await trace.GetConfigurationAsync();
            configuration.Categories.Add("Filter1");
            engine.WriteEntry(new Entry() { Category = "Filter1", Message = "Filter1 Message" });
            engine.WriteEntry(new Entry() { Category = "Filter2", Message = "Filter2 Message" });
            engine.Flush();

            Assert.AreEqual(1, trace.Entries.Count);
            Assert.AreEqual("Filter1", trace.Entries[0].Category);
        }

        [TestMethod]
        public async Task Store_LevelFilterTest()
        {
            var engine = new WindowsStore.Engine();
            var trace = new TraceListener("Store_LevelFilterTest");
            engine.RegisterListener(trace);

            var configuration = await trace.GetConfigurationAsync();
            configuration.StartLevel = Level.Error;
            trace.UpdateConfiguration();

            engine.WriteEntry(new Entry() { Category = "Filter1", Level = PortiLog.Level.Critical, Message = "Critical Message" });
            engine.WriteEntry(new Entry() { Category = "Filter2", Level = PortiLog.Level.Info, Message = "Info Message" });
            engine.Flush();

            Assert.AreEqual(1, trace.Entries.Count);
            Assert.AreEqual(PortiLog.Level.Critical, trace.Entries[0].Level);
        }

        [TestMethod]
        public async Task Store_LoggingDisabledTest()
        {
            var trace = new TraceListener("Trace");
            Logger.Engine.RegisterListener(trace);

            var configuration = await Logger.Engine.GetConfigurationAsync();
            configuration.LoggingEnabled = false;

            Logger.WriteEntry(new Entry() { Category = "Filter1", Level = PortiLog.Level.Critical, Message = "Critical Message" });
            Logger.WriteEntry(new Entry() { Category = "Filter2", Level = PortiLog.Level.Info, Message = "Info Message" });
            Logger.Engine.Flush();

            Assert.AreEqual(0, trace.Entries.Count);
        }

        [TestMethod]
        public async Task Store_DumpWorkFlow()
        {
            DumpFileListener l1 = new DumpFileListener("App");

            var entrylist = CreateDummyEntryList(100);
            await l1.WriteEntriesWorkflowAsync(entrylist);

            await l1.DumpAsync();
        }

        [TestMethod]
        public async Task Store_CheckConfigurationAsync()
        {
            var engine = (WindowsStore.Engine)Logger.Engine;
            var globalConfig = await engine.GetConfigurationAsync();
            var dumpListener = engine.DumpListener;
            var configuration = await dumpListener.GetConfigurationAsync();
            Assert.IsNotNull(configuration.ServiceUrl);
        }

        [TestMethod]
        public void Store_GetComputerName()
        {
            var computerName = StoreUtil.GetComputerName();
            Assert.IsNotNull(computerName);
        }

        [TestMethod]
        public void Store_ConfigurationSerialize()
        {
            var config = new Configuration();
            config.ApplicationName = "appname";
            config.LoggingEnabled = false;
            var l = new ListenerConfiguration();
            l.Name = "lname";
            l.StartLevel = Level.Critical;
            l.ServiceUrl = "http://test";
            l.Categories.Add("cat1");
            config.Listeners.Add(l);
            var xml = Util.ToXml(config);
            return;
            //Assert.IsNotNull(computerName);
        }

        [TestMethod]
        public async Task Store_GetPrincipalName()
        {
            var name = await StoreUtil.GetUserNameAsync();
            Assert.IsNotNull(name);
        }

        [TestMethod]
        public void Store_PackageId()
        {
            var pid = Package.Current.Id;
            var spid = pid.ToString();
            return;
        }

        [TestMethod]
        public async Task Store_DumpAsync()
        {
            DumpFileListener l1 = ((WindowsStore.Engine)Logger.Engine).DumpListener;
            var folder = await l1.GetFolderAsync();

            var entryList = CreateDummyEntryList(100);
            await l1.WriteNewDumpFilesAsync(entryList, folder);
            await l1.DumpAsync();
            
            var files = await folder.GetFilesAsync();
            Assert.AreEqual(0, files.Count);
            return;
        }

        [TestMethod]
        public async Task Store_WriteNewDumpFilesAsync()
        {
            DumpFileListener l1 = new DumpFileListener("Store_WriteNewDumpFilesAsync");
            var folder = await l1.GetFolderAsync();

            var entryList = CreateDummyEntryList(100);
            await l1.WriteNewDumpFilesAsync(entryList, folder);
            var files = await folder.GetFilesAsync();

            int shouldBe = ResultInNumberOfFiles(l1, 100);
            Assert.AreEqual(files.Count, shouldBe);
            return;
        }

        [TestMethod]
        public async Task Store_CleanupDumpFilesAsync()
        {
            DumpFileListener l1 = new DumpFileListener("Store_CleanupDumpFilesAsync");
            var folder = await l1.GetFolderAsync();

            var entryList = CreateDummyEntryList(2000);
            await l1.WriteNewDumpFilesAsync(entryList, folder);
            await l1.CleanupDumpFilesAsync(folder);

            var files = await folder.GetFilesAsync();

            var shouldBe = ResultInNumberOfFiles(l1, 2000);
            Assert.AreEqual(files.Count, shouldBe);
            return;
        }

        int ResultInNumberOfFiles(DumpFileListener listener, int entryCount)
        {
            int result;
            if (entryCount >= listener.MaxDumpEntryCount)
            {
                result = (int)Math.Floor((double)listener.MaxDumpEntryCount / (double)listener.MaxDumpEntryCountPerFile);
            }
            else
            {
                result = (int)Math.Ceiling((double)entryCount / (double)listener.MaxDumpEntryCountPerFile);
            }
            return result;
        }

        List<Entry> CreateDummyEntryList(int counter)
        {
            var result = new List<Entry>();
            for (int i = 0; i < counter; i++)
            {
                result.Add(CreateDummyEntry(i));
            }
            return result;
        }

        Entry CreateDummyEntry(int index)
        {
            Entry entry = new Entry();
            entry.Message = "dummy message " + index.ToString();
            entry.Level = Level.Error;
            entry.Category = "category " + index.ToString();
            return entry;
        }

        [TestMethod]
        public void Store_JsonTest()
        {
            //JsonValue jv = new Windows.Data.Json.JsonValue();
            
            JsonObject o1 = new JsonObject();
            o1["GuidId"] = JsonValue.CreateStringValue(Guid.NewGuid().ToString());
            o1["Created"] = JsonValue.CreateNumberValue(DateTime.Now.Ticks);
            o1["Id"] = JsonValue.CreateNumberValue(0);
            o1["Level"] = JsonValue.CreateNumberValue((int)Level.Info);
            o1["Category"] = JsonValue.CreateStringValue("Category1");
            o1["Message"] = JsonValue.CreateStringValue("my message1");
            var o1s = o1.Stringify();

            JsonObject o2 = new JsonObject();
            o2["GuidId"] = JsonValue.CreateStringValue(Guid.NewGuid().ToString());
            o2["Created"] = JsonValue.CreateNumberValue(DateTime.Now.Ticks);
            o2["Id"] = JsonValue.CreateNumberValue(0);
            o2["Level"] = JsonValue.CreateNumberValue((int)Level.Error);
            o2["Category"] = JsonValue.CreateStringValue("Category2");
            o2["Message"] = JsonValue.CreateStringValue("my message2");
            var o2s = o2.Stringify();

            JsonObject o3 = new JsonObject();
            o3["GuidId"] = JsonValue.CreateStringValue(Guid.NewGuid().ToString());
            o3["Created"] = JsonValue.CreateNumberValue(DateTime.Now.Ticks);
            o3["Id"] = JsonValue.CreateNumberValue(0);
            o3["Level"] = JsonValue.CreateNumberValue((int)Level.Error);
            o3["Category"] = JsonValue.CreateStringValue("Category3");
            o3["Message"] = JsonValue.CreateStringValue("my message3");
            var o3s = o3.Stringify();

            JsonObject o4 = new JsonObject();
            o4["GuidId"] = JsonValue.CreateStringValue(Guid.NewGuid().ToString());
            o4["Created"] = JsonValue.CreateNumberValue(DateTime.Now.Ticks);
            o4["Id"] = JsonValue.CreateNumberValue(0);
            o4["Level"] = JsonValue.CreateNumberValue((int)Level.Error);
            o4["Category"] = JsonValue.CreateStringValue("Category4");
            o4["Message"] = JsonValue.CreateStringValue("my message4");
            var o4s = o4.Stringify();

            var two = o1s + o2s;
            var arr = new JsonArray();
            arr.Add(o1);
            arr.Add(o2);

            var arrs = arr.Stringify();
            //JsonValue arr = JsonValue.Parse(two);
            //var length = arr.Count;
            return;
        }
    }
}
