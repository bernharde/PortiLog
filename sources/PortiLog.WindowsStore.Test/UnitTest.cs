﻿using System;
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
        public void Store_CategoryFilterTest()
        {
            var engine = new WindowsStore.Engine();
            var trace = new TraceListener("Store_CategoryFilterTest");
            engine.RegisterListener(trace);

            var configuration = trace.CreateDefaultConfiguration();
            configuration.Categories.Add("Filter1");
            trace.Configuration = configuration;
            engine.WriteEntry(new Entry() { Category = "Filter1", Message = "Filter1 Message" });
            engine.WriteEntry(new Entry() { Category = "Filter2", Message = "Filter2 Message" });
            engine.Flush();

            Assert.AreEqual(1, trace.Entries.Count);
            Assert.AreEqual("Filter1", trace.Entries[0].Category);
        }

        [TestMethod]
        public void Store_LevelFilterTest()
        {
            var engine = new WindowsStore.Engine();
            var trace = new TraceListener("Store_LevelFilterTest");
            engine.RegisterListener(trace);

            var configuration = trace.CreateDefaultConfiguration();
            configuration.StartLevel = Level.Error;
            trace.Configuration = configuration;

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
        public void Store_TypeNameTest()
        {
            var type = this.GetType();
            var name = type.Name;
            Assert.IsNotNull(name);
        }

        [TestMethod]
        public void Store_IsFullyQualifiedNameTest()
        {
            var fully = LogUtil.IsFullyQualifiedName("FileListener");
            var fully2 = LogUtil.IsFullyQualifiedName("PortiLog.WindowsStore.Test.UnitTest, PortiLog.WindowsStore.Test");
            Assert.IsFalse(fully);
            Assert.IsTrue(fully2);
        }

        [TestMethod]
        public void Store_BuildFullyQualifiedNameTest_Simple()
        {
            var type = typeof(WindowsStore.Engine);
            var fully = LogUtil.BuildFullyQualifiedName(type, "FileListener");

            var foundType = Type.GetType(fully);
            Assert.IsNotNull(foundType);
        }

        [TestMethod]
        public void Store_BuildFullyQualifiedNameTest_Fully()
        {
            var type = typeof(WindowsStore.Engine);
            var fully = LogUtil.BuildFullyQualifiedName(type, "PortiLog.WindowsStore.FileListener, PortiLog.WindowsStore");
            var foundType = Type.GetType(fully);
            Assert.IsNotNull(foundType);
        }

        [TestMethod]
        public async Task Store_CheckConfigurationAsync()
        {
            var engine = (WindowsStore.Engine)Logger.Engine;
            var globalConfig = await engine.GetConfigurationAsync();
            var dumpListener = engine.FindListener<DumpFileListener>();
            var configuration = dumpListener.Configuration;
#if DEBUG
            Assert.IsNotNull(configuration.ServiceUrl);
#else
            Assert.IsNull(configuration.ServiceUrl);
#endif
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
            var xml = LogUtil.ToXml(config);
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
            var engine = new Engine();
            await engine.ConfigureAsync();
            DumpFileListener l1 = engine.FindListener<DumpFileListener>();
            var folder = await l1.GetFolderAsync();

            var entryList = CreateDummyEntryList(100);
            await l1.WriteNewDumpFilesAsync(entryList, folder);
            await l1.DumpAsync();
            
            var files = await folder.GetFilesAsync();
            Assert.AreEqual(0, files.Count, engine.GetInternalTraceLog());
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
            
            return;
        }
    }
}
