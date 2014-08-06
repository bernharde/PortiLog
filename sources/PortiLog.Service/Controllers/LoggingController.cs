using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Web.Configuration;
using System.Web.Http;

namespace PortiLog.Service.Controllers
{
    public class LoggingController : ApiController
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

        public HttpResponseMessage PostDumpData(DumpData dumpData)
        {
            var response = Request.CreateResponse(HttpStatusCode.Created);

            //string uri = Url.Link("DefaultApi", new { id = dumpData.DeviceId });
            //response.Headers.Location = new Uri(uri);
            //return response;

            // check if, we have anything to write here.
            if (dumpData == null)
                return response;

            if (dumpData.Entries.Count == 0)
                return response;

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
                    if (File.Exists(bakFilename))
                        File.Delete(bakFilename);
                    File.Move(logFilename, bakFilename);
                }
            }
            finally
            {
                semaphore.Release();
            }

            return response;
        }
    }
}
