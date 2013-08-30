using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PortiLog.WindowsStore
{
    public class Engine : EngineBase
    {
        public Engine()
        {
        }

        DumpMetaData _dumpMetaData;

        public override async Task<DumpMetaData> GetDumpMetaDataAsync()
        {
            if (_dumpMetaData == null)
            {
                _dumpMetaData = new DumpMetaData();
                _dumpMetaData.ApplicationName = StoreUtil.GetAppName();
                _dumpMetaData.ApplicationVersion = StoreUtil.GetAppVersion();
                _dumpMetaData.DeviceId = StoreUtil.GetComputerName();
                _dumpMetaData.UserId = await StoreUtil.GetUserNameAsync();
            }
            return _dumpMetaData;
        }

        public override async Task<string> LoadConfigurationFromFileAsync()
        {
            var xml = await StoreUtil.LoadFromAppFolderAsync("PortiLog.Config.xml");
            return xml;
        }

        public override Configuration CreateDefaultConfiguration()
        {
            var configuration = new Configuration();
            configuration.LoggingEnabled = true;
            configuration.ApplicationName = StoreUtil.GetAppName();
            var fileListener = new ListenerConfiguration();
            fileListener.Name = "App";
            configuration.Listeners.Add(fileListener);
            return configuration;
        }

        //public override async Task DumpServiceTestAsync()
        //{
        //    var listener = this.DumpListener;
        //    var service = listener.CheckDumper();
        //    var entries = new List<Entry>();
        //    entries.Add(new Entry() { Category = "DumpServiceTest", Level = Level.Verbose, Message = "Dump Service Test Entry" });
        //    await listener.DumpEntriesAsync(service, entries);
        //}
    }
}
