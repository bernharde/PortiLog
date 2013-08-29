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
        FileListener _defaultListener;

        public override ListenerBase DefaultListener
        {
            get 
            {
                return _defaultListener; 
            }
        }

        DumpFileListener _dumpListener;

        public DumpFileListener DumpListener
        {
            get { return _dumpListener; }
        }

        public Engine()
        {
            _defaultListener = new FileListener("App");
            RegisterListener(_defaultListener);

            _dumpListener = new DumpFileListener("DumpListener");
            RegisterListener(_dumpListener);
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

        Configuration _configuration;

        SemaphoreSlim _configurationSlim = new SemaphoreSlim(1);

        public override async Task<Configuration> GetConfigurationAsync()
        {
            if (!ConfigurationRead)
            {
                _configurationSlim.Wait();

                if (!ConfigurationRead)
                {
                    var xml = await StoreUtil.LoadFromAppFolderAsync("PortiLog.Config.xml");
                    try
                    {
                        _configuration = Util.FromXml<Configuration>(xml);
                    }
                    catch (Exception ex)
                    {
                        InternalTrace(Entry.CreateError("PortiLog.Config.xml has an invalid format: " + ex.Message));
                    }
                    if (_configuration == null)
                    {
                        InternalTrace(Entry.CreateInfo("PortiLog.Config.xml not available! Default configuration used"));
                        _configuration = new Configuration();
                    }
                    ConfigurationRead = true;
                }

                _configurationSlim.Release();
            }
            return _configuration;
        }

        public override async Task DumpServiceTestAsync()
        {
            var listener = this.DumpListener;
            var service = await listener.CheckDumperAsync();
            var entries = new List<Entry>();
            entries.Add(new Entry() { Category = "DumpServiceTest", Level = Level.Verbose, Message = "Dump Service Test Entry" });
            await listener.DumpEntriesAsync(service, entries);
        }

        public override async Task DumpAsync()
        {
            await this.DumpListener.DumpAsync();
        }

    }
}
