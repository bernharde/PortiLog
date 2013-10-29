using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.System.Threading;

namespace PortiLog.WindowsStore
{
    public class FileListener : ListenerBase, IFileListener
    {
        /// <summary>
        /// Storage file to be used to write logs
        /// </summary>
        StorageFile _storageFile = null;
        /// <summary>
        /// The format to be used by logging.
        /// </summary>
        string _format = "{0:yyyy-MM-dd HH\\:mm\\:ss\\:ffff}\tLevel: {1}\tCategory: {2}\tMessage: '{3}'";

        /// <summary>
        /// Gets the log file name (without path)
        /// </summary>
        public string FileName
        {
            get
            {
                return Name + ".log";
            }
        }

        public async Task PrepareFileAsync()
        {
            await Task.Delay(0);
        }

        /// <summary>
        /// Gets the backup log file name (without path)
        /// </summary>
        public string BackupLogFilename
        {
            get
            {
                return Name.Replace(" ", "_") + ".log.bak";
            }
        }

        /// <summary>
        /// Gets the full log file name (with path)
        /// </summary>
        public string FullLogFilename
        {
            get
            {
                return System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, FileName);
            }
        }

        /// <summary>
        /// Gets the full backup log file name (with path)
        /// </summary>
        public string FullBackupLogFilename
        {
            get
            {
                return System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, BackupLogFilename);
            }
        }

        /// <summary>
        /// Initializes a new instance of the listener
        /// </summary>
        /// <param name="name">the name of the listener. The file name of the log will be "(name).log"</param>
        public FileListener(string name) : base(name)
        {
        }

        public override async Task WriteEntriesAsync(List<Entry> entries)
        {
            // make five tries, if background workers are accessing the file also - so access could denied sometimes
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    // for the first call - create the storage file
                    if (_storageFile == null)
                    {
                        _storageFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(
                            FileName, CreationCollisionOption.OpenIfExists);
                    }

                    List<string> lines = new List<string>();
                    foreach (var entry in entries)
                    {
                        var message = EntryFormatter.FormatMessage(entry);
                        var line = string.Format(_format, entry.Created, entry.Level, entry.Category, message);
                        lines.Add(line);
                    }

                    // check the size of the log file
                    await CheckLogFileSize();

                    // write the buffered log entries to the file
                    await FileIO.AppendLinesAsync(_storageFile, lines);

                    // everything is ok to return here...
                    return;
                }
                catch (Exception ex)
                {
                    InternalTrace(Entry.CreateError("DelayedWorker exception: " + ex.Message + DateTime.Now.ToString()));
                }

                // Writing to the log file was not successful. Wait for a short period and do a retry
                await Task.Delay(300);
            }
        }


        /// <summary>
        /// Check, that the log file size will not explode
        /// </summary>
        /// <remarks>
        /// When the log file size is more than 1 MB, the current log file is copied to the BackupLogFileName.
        /// </remarks>
        async Task CheckLogFileSize()
        {
            try
            {
                var buffer = await FileIO.ReadBufferAsync(_storageFile);
                // when the log file reaches 1 mb file size, copy the log to bak file and empty the log file.
                int mb = 1024 * 1000;
                if (buffer.Length > mb)
                {
                    var bakFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(BackupLogFilename, CreationCollisionOption.ReplaceExisting);
                    //copy and replace the old backup log
                    await _storageFile.CopyAndReplaceAsync(bakFile);
                    
                    // empty the existing log file
                    await FileIO.WriteTextAsync(_storageFile, string.Empty);
                }
            }
            catch (Exception ex)
            {
                // Do nothing here. It will be handled the next time
                InternalTrace(Entry.CreateError("CheckLogFileSize failed: " + ex.Message));
            }
        }
    }
}