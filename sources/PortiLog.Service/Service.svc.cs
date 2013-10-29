using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Threading;
using System.Web.Configuration;

namespace PortiLog.Service
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Service1.svc or Service1.svc.cs at the Solution Explorer and start debugging.
    public class Service : IService
    {
        EntryFormatter _entryFormatter;

        public EntryFormatter EntryFormatter
        {
            get 
            {
                if (_entryFormatter == null)
                    _entryFormatter = new EntryFormatter();
                return _entryFormatter; 
            }
        }

        static ConcurrentDictionary<string, SemaphoreSlim> _semaphoreDictionary = new ConcurrentDictionary<string, SemaphoreSlim>();

        public void Dump(DumpData dumpData)
        {
            // check if, we have anything to write here.
            if (dumpData == null)
                return;

            if (dumpData.Entries.Count == 0)
                return;

            var source = string.Format("{0}_{1}.log", dumpData.ApplicationName, dumpData.UserId);

            // limit access to a file to one thread
            var semaphore = _semaphoreDictionary.GetOrAdd(source, new SemaphoreSlim(1));

            try
            {
                semaphore.Wait();
                string basePath = WebConfigurationManager.AppSettings["Path"];
                string logFilename = Path.Combine(basePath, LogUtil.RemoveInvalidPathChars(source));

                // write log data to file
                StringBuilder sb = new StringBuilder();
                foreach (var entry in dumpData.Entries)
                {
                    //var newFormatedLine = string.Format(_format, entry.Created, entry.Level, entry.Message);
                    var formatted = EntryFormatter.Format(entry);
                    sb.AppendLine(formatted);
                }
                File.AppendAllText(logFilename, sb.ToString());

                // check if log file reaches max size, if so, move the log to .bak
                int maxSize = Convert.ToInt32(WebConfigurationManager.AppSettings["MaxLogSize"]);

                FileInfo info = new FileInfo(logFilename);
                if (info.Length > maxSize)
                {
                    var bakFilename = logFilename + ".bak";
                    if(File.Exists(bakFilename))
                        File.Delete(bakFilename);
                    File.Move(logFilename, bakFilename);
                }
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
