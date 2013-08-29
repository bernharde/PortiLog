using Microsoft.Phone.Info;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PortiLog.WindowsPhone
{
    public class Engine : EngineBase
    {
        DbListener _listener;

        public override ListenerBase DefaultListener
        {
            get { return _listener; }
        }

        public Engine()
        {
            _listener = new DbListener("App");
            RegisterListener(_listener);
        }

        DumpMetaData _dumpMetaData;

        public override Task<DumpMetaData> GetDumpMetaDataAsync()
        {
            var task = Task.Factory.StartNew( () =>
            {
                if (_dumpMetaData == null)
                {
                    _dumpMetaData = new DumpMetaData();
                    _dumpMetaData.ApplicationName = PhoneUtil.GetAppTitle();
                    _dumpMetaData.ApplicationVersion = PhoneUtil.GetAppVersion();
                    _dumpMetaData.DeviceId = GetDeviceId();
                    _dumpMetaData.UserId = GetUserId();
                }
                return _dumpMetaData;
            });
            return task;
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
                    var xml = await PhoneUtil.LoadFromAppFolderAsync("PortiLog.Config.xml");
                    _configuration = Util.FromXml<Configuration>(xml);
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
            var listener = (DbListener)this.DefaultListener;
            var service = await listener.CheckDumperAsync();
            var entries = new List<Entry>();
            entries.Add(new Entry() { Category = "DumpServiceTest", Level = Level.Verbose, Message = "Dump Service Test Entry"});
            await listener.DumpEntriesToServiceAsync(service, entries);
        }

        public override async Task DumpAsync()
        {
            var listener = (DbListener)this.DefaultListener;
            await listener.DumpAsync();
        }


        bool _deviceIdRead;
        string _deviceId;

        public string GetDeviceId()
        {
            if (_deviceIdRead)
                return _deviceId;

            try
            {
                object deviceId;
                if (DeviceExtendedProperties.TryGetValue("DeviceUniqueId", out deviceId))
                {
                    _deviceId = deviceId.ToString();
                }
                else
                {
                    InternalTrace(Entry.CreateError("Retrieve user id failed. The capability ID_CAP_IDENTITY_DEVICE is required for this."));
                }
            }
            catch (Exception ex)
            {
                InternalTrace(Entry.CreateError("GetDeviceId failed! " + ex.Message));
            }

            _deviceIdRead = true;
            return _deviceId;
        }

        const int IdentifierLength = 32;
        const int IdentifierOffset = 2;

        string _userId;
        bool _userIdRead;

        public string GetUserId()
        {
            if (_userIdRead)
                return _userId;

            object anid;
            if (UserExtendedProperties.TryGetValue("ANID2", out anid))
            {
                if (anid != null && anid.ToString().Length >= (IdentifierLength + IdentifierOffset))
                {
                    _userId = anid.ToString().Substring(IdentifierOffset, IdentifierLength);
                }
            }
            else
            {
                InternalTrace(Entry.CreateInfo("Retrieve user id failed. The capability ID_CAP_IDENTITY_USER is required for this."));
            }

            _userIdRead = true;
            return _userId;
        }
    }
}
