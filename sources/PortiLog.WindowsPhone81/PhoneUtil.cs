using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.System.Profile;
using Windows.System.UserProfile;

namespace PortiLog.WindowsPhone81
{
    public static class PhoneUtil
    {
        public static string GetAppName()
        {
            return Package.Current.Id.FullName;
        }

        static bool _hardwareIdRead;
        static string _hardwareId;

        public static string GetHardwareId()
        {
            if (!_hardwareIdRead)
            {
                var token = HardwareIdentification.GetPackageSpecificToken(null);
                var hardwareId = token.Id;
                var dataReader = Windows.Storage.Streams.DataReader.FromBuffer(hardwareId);

                byte[] bytes = new byte[hardwareId.Length];
                dataReader.ReadBytes(bytes);

                _hardwareId = BitConverter.ToString(bytes);
                _hardwareIdRead = true;
            }
            return _hardwareId;
        }

        public static async Task<string> LoadFromAppFolderAsync(string filename)
        {
            try
            {
                var file = await Package.Current.InstalledLocation.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);
                var content = await FileIO.ReadTextAsync(file);
                return content;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string GetComputerName()
        {
            var list = NetworkInformation.GetHostNames();
            var computerName = list.FirstOrDefault(l => l.Type == Windows.Networking.HostNameType.DomainName);
            if (computerName != null)
                return computerName.DisplayName;
            else
                return null;
        }

        public static string GetAppVersion()
        {
            var appVersion = ConvertToString(Package.Current.Id.Version);
            return appVersion;
        }

        public static string ConvertToString(PackageVersion packageVersion)
        {
            var result = string.Format("{0}.{1}.{2}.{3}", packageVersion.Major, packageVersion.Minor
                , packageVersion.Build, packageVersion.Revision);
            return result;
        }

        [System.Diagnostics.DebuggerStepThrough]
        public static async Task<string> GetUserNameAsync()
        {
            try
            {
                string userName = await UserInformation.GetPrincipalNameAsync();
                if (string.IsNullOrEmpty(userName))
                    userName = await UserInformation.GetDisplayNameAsync();
                return userName;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
