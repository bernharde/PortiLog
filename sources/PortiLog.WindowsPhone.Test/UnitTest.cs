using System;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Microsoft.Phone.Info;
using System.IO.IsolatedStorage;
using System.Xml.Serialization;
using System.Threading.Tasks;
using Windows.Storage;
using System.IO;
using Windows.Networking.Connectivity;

namespace PortiLog.WindowsPhone.Test
{
    [TestClass]
    public class UnitTest
    {
        [TestMethod]
        public async Task Phone_WriteSingleLogEntryAndDump()
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
        public async Task Phone_Write1000LogEntryAndDump()
        {
            for (int i = 0; i < 1000; i++)
            {
                Logger.Info("Phone_Write1000LogEntryAndDump " + i.ToString());
            }
            Logger.Engine.Flush();
            await Logger.Engine.DumpAsync();
        }

        [TestMethod]
        public void Phone_CategoryFilterTest()
        {
            var engine = new WindowsPhone.Engine();
            var trace = new TraceListener("Phone_CategoryFilterTest");
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
        public void Phone_LevelFilterTest()
        {
            var engine = new WindowsPhone.Engine();
            var trace = new TraceListener("Phone_LevelFilterTest");
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
        public async Task Phone_LoggingDisabledTest()
        {
            var trace = new TraceListener("Phone_LoggingDisabledTest");
            Logger.Engine.RegisterListener(trace);

            var configuration = await Logger.Engine.GetConfigurationAsync();
            configuration.LoggingEnabled = false;

            Logger.WriteEntry(new Entry() { Category = "Filter1", Level = PortiLog.Level.Critical, Message = "Critical Message" });
            Logger.WriteEntry(new Entry() { Category = "Filter2", Level = PortiLog.Level.Info, Message = "Info Message" });
            Logger.Engine.Flush();

            Assert.AreEqual(0, trace.Entries.Count);
        }

        [TestMethod]
        public void Phone_GetApplicationId()
        {
            var appid = PhoneUtil.GetAppProductId();
            Assert.IsNotNull(appid);
        }

        [TestMethod]
        public void Phone_GetDeviceName()
        {
            var value = PhoneUtil.GetDeviceName();
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
