using Microsoft.Phone.Info;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;

namespace PortiLog.WindowsPhone
{
    public static class PhoneUtil
    {
        

        public static Guid GetAppProductId()
        {
            using (var strm = TitleContainer.OpenStream("WMAppManifest.xml"))
            {
                var xml = XElement.Load(strm);
                var prodId = (from app in xml.Descendants("App")
                              select app.Attribute("ProductID").Value).FirstOrDefault();
                if (string.IsNullOrEmpty(prodId)) return Guid.Empty;
                return new Guid(prodId);
            }
        }

        public static string GetAppTitle()
        {
            using (var strm = TitleContainer.OpenStream("WMAppManifest.xml"))
            {
                var xml = XElement.Load(strm);
                var value = (from app in xml.Descendants("App")
                              select app.Attribute("Title").Value).FirstOrDefault();
                return value;
            }
        }

        public static string GetAppVersion()
        {
            string version = XDocument.Load("WMAppManifest.xml").Root.Element("App").Attribute("Version").Value;
            return version;
        }

        public static async Task<string> LoadFromAppFolderAsync(string strFileName)
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

 



        public static string GetDeviceName()
        {
            
            //object anid;
            //if (DeviceExtendedProperties.TryGetValue("DeviceName", out anid))
            //{
            //    if (anid != null && anid.ToString().Length >= (IdentifierLength + IdentifierOffset))
            //    {
            //        return anid.ToString().Substring(IdentifierOffset, IdentifierLength);
            //    }
            //}

            //return null;

            return DeviceStatus.DeviceName;
        }
    }
}
