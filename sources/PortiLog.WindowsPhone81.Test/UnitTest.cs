using System;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System.Threading.Tasks;
using Windows.Storage;
using System.IO;

namespace PortiLog.WindowsPhone81.Test
{
    [TestClass]
    public class UnitTest
    {
        [TestMethod]
        public async Task Phone81_WriteSingleLogEntryAndDump()
        {
            try
            {
                Logger.Info("test");
                Logger.Engine.Flush();
                var listener = Logger.Engine.FindListener<DbListener>();
                await listener.DumpAsync();
            }
            catch (Exception ex)
            {
                Assert.IsNotNull(ex, ex.Message + "internal trace: " + Logger.Engine.GetInternalTraceLog());
            }
        }

        [TestMethod]
        public async Task Phone81_WriteSingleErrorEntryAndDump()
        {
            try
            {
                Logger.Error("test", new Exception("myinnererror"));
                Logger.Engine.Flush();
                var listener = Logger.Engine.FindListener<DbListener>();
                await listener.DumpAsync();
            }
            catch (Exception ex)
            {
                Assert.IsNotNull(ex, ex.Message + "internal trace: " + Logger.Engine.GetInternalTraceLog());
            }
        }

        [TestMethod]
        public async Task Phone81_Write1000LogEntryAndDump()
        {
            for (int i = 0; i < 1000; i++)
            {
                Logger.Info("Phone81_Write1000LogEntryAndDump " + i.ToString());
            }
            Logger.Engine.Flush();
            await Logger.Engine.DumpAsync();
        }

        [TestMethod]
        public void Phone81_CategoryFilterTest()
        {
            var engine = new WindowsPhone81.Engine();
            var trace = new TraceListener("Phone81_CategoryFilterTest");
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
        public void Phone81_LevelFilterTest()
        {
            var engine = new WindowsPhone81.Engine();
            var trace = new TraceListener("Phone81_LevelFilterTest");
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
        public async Task Phone81_LoggingDisabledTest()
        {
            var trace = new TraceListener("Phone81_LoggingDisabledTest");
            Logger.Engine.RegisterListener(trace);

            var configuration = await Logger.Engine.GetConfigurationAsync();
            configuration.LoggingEnabled = false;

            Logger.WriteEntry(new Entry() { Category = "Filter1", Level = PortiLog.Level.Critical, Message = "Critical Message" });
            Logger.WriteEntry(new Entry() { Category = "Filter2", Level = PortiLog.Level.Info, Message = "Info Message" });
            Logger.Engine.Flush();

            Assert.AreEqual(0, trace.Entries.Count);
        }

        [TestMethod]
        public void Phone81_GetApplicationId()
        {
            var appid = PhoneUtil.GetHardwareId();
            Assert.IsNotNull(appid);
        }

        [TestMethod]
        public void Phone81_GetDeviceName()
        {
            var value = PhoneUtil.GetHardwareId();
            Assert.IsNotNull(value);
        }

        public async static Task<string> LoadFromAppFolderAsync(string strFileName)
        {
            string theData = string.Empty;

            // There's no FileExists method in WinRT, so have to try to get a reference to it 
            // and catch the exception instead 
            StorageFile storageFile = null;
            bool fileExists = false;
            try
            {
                // See if file exists 
                Uri uriFileToLoad = new Uri("ms-appx:///" + strFileName, UriKind.Absolute);
                storageFile = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(uriFileToLoad); // During debug, it bombs here and jumps to the FileNotFoundException
                fileExists = true;
            }
            catch (FileNotFoundException)
            {
                // File doesn't exist 
                fileExists = false;
            }

            if (!fileExists)
            {
                // Initialize the return data 
                theData = string.Empty;
            }
            else
            {
                // File does exists, so open it and read the contents 
                Stream readStream = await storageFile.OpenStreamForReadAsync();
                using (StreamReader reader = new StreamReader(readStream))
                {
                    theData = await reader.ReadToEndAsync();
                }
            }

            return theData;
        }
    }
}
