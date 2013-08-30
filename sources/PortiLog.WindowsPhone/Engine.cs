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
        public Engine()
        {
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

        public override async Task<string> LoadConfigurationFromFileAsync()
        {
            var xml = await PhoneUtil.LoadFromAppFolderAsync("PortiLog.Config.xml");
            return xml;
        }

        public override Configuration CreateDefaultConfiguration()
        {
            var configuration = new Configuration();
            configuration.LoggingEnabled = true;
            configuration.ApplicationName = PhoneUtil.GetAppTitle();
            var dbListener = new ListenerConfiguration();
            dbListener.Name = "App";
            dbListener.Type = typeof(DbListener).AssemblyQualifiedName;
            configuration.Listeners.Add(dbListener);
            return configuration;
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
