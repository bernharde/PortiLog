using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Linq;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Storage;
using Windows.System.Threading;
using Windows.ApplicationModel;
using Windows.System.UserProfile;

namespace PortiLog.WindowsStore
{
    public class DumpFileListener : ListenerBase
    {
        public class DumpFile
        {
            public string Name { get; set; }
            public long Ticks { get; set; }
            public int EntryCount { get; set; }
            public StorageFile File { get; set; }
            public DateTime Created { get { return new DateTime(Ticks); } }
        }

        int _maxDumpEntryCountPerFile = 30;

        public int MaxDumpEntryCountPerFile
        {
            get { return _maxDumpEntryCountPerFile; }
            set { _maxDumpEntryCountPerFile = value; }
        }

        int _maxDumpEntryCount = 1000;

        public int MaxDumpEntryCount
        {
            get { return _maxDumpEntryCount; }
            set { _maxDumpEntryCount = value; }
        }

        
  
        /// <summary>
        /// Initializes a new instance of the listener
        /// </summary>
        /// <param name="name">the name of the listener. The file name of the log will be "(name).log"</param>
        public DumpFileListener(string name) : base(name)
        {
        }

        public override async Task WriteEntriesAsync(List<Entry> entries)
        {
            // make five tries, if background workers are accessing the file also - so access could denied sometimes
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    await WriteEntriesWorkflowAsync(entries);
                    return;
                }
                catch (Exception ex)
                {
                    string msg = string.Format("DelayedWorker exception: {0}, {1} -> {2}", ex.Message, DateTime.Now, i);
                    InternalTrace(Entry.CreateError(msg));
                }

                // Writing to the log file was not successful. Wait for a short period and do a retry
                await Task.Delay(300);
            }
        }

        public async Task WriteEntriesWorkflowAsync(List<Entry> entries)
        {
            var folder = await GetFolderAsync();

            await CleanupDumpFilesAsync(folder);

            await WriteNewDumpFilesAsync(entries, folder);

            EnsureDumperIsRunning();
        }

        Task _dumperTask;

        void EnsureDumperIsRunning()
        {
            if (DumpEnabled && _dumperTask == null)
                _dumperTask = Task.Factory.StartNew(async delegate { await DumpAsync(); });
        }

        public async Task WriteNewDumpFilesAsync(List<Entry> entries, StorageFolder folder)
        {
            // create the new file name in the format <DateTime ticks>.<Guid>.<EntryCount>
            int totalEntries = entries.Count;
            int entriesPending = totalEntries;
            int index = 0;
            while (true)
            {
                var newFileEntryCount = Math.Min(entriesPending, MaxDumpEntryCountPerFile);
                var newFileEntries = entries.GetRange(index, newFileEntryCount);
                var newFileName = string.Format("{0}.{1}.{2}", DateTime.Now.Ticks, Guid.NewGuid().ToString(), newFileEntryCount);
                var newFile = await folder.CreateFileAsync(newFileName, CreationCollisionOption.FailIfExists);
                string content = ConvertEntriesToJsonArray(newFileEntries);
                await FileIO.WriteTextAsync(newFile, content);

                entriesPending -= newFileEntryCount;
                if (entriesPending < 1)
                    break;

                index += newFileEntryCount;
            }
        }

        public async Task CleanupDumpFilesAsync(StorageFolder folder)
        {
            var files = await folder.GetFilesAsync();

            var dumpFiles = CreateDumpFileList(files);

            int totalEntryCount = dumpFiles.Sum(d => d.EntryCount);
            int entriesToDelete = totalEntryCount - this.MaxDumpEntryCount;

            if (totalEntryCount > this.MaxDumpEntryCount)
            {
                foreach (var dumpFile in dumpFiles)
                {
                    await dumpFile.File.DeleteAsync();
                    entriesToDelete -= dumpFile.EntryCount;
                    if (entriesToDelete < 1)
                        break;
                }
            }
        }

        List<DumpFile> CreateDumpFileList(IReadOnlyList<StorageFile> files)
        {
            var dumpFiles = new List<DumpFile>();
            foreach (var file in files)
            {
                DumpFile dumpFile = new DumpFile();
                dumpFile.File = file;
                dumpFile.Name = file.Name;

                var values = dumpFile.Name.Split('.');

                dumpFile.Ticks = Convert.ToInt64(values[0]);
                dumpFile.EntryCount = Convert.ToInt32(values[2]);
                dumpFiles.Add(dumpFile);
            }
            return dumpFiles.OrderBy(d => d.Ticks).ToList();
        }

        public async Task<StorageFolder> GetFolderAsync()
        {
            var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(this.Name, CreationCollisionOption.OpenIfExists);
            return folder;
        }

        public string ConvertEntriesToJsonArray(List<Entry> entries)
        {
            if (entries == null || entries.Count == 0)
                return null;

            JsonArray arr = new JsonArray();
            foreach (var entry in entries)
            {
                JsonObject obj = new JsonObject();
                obj["Created"] = JsonValue.CreateNumberValue(entry.Created.Ticks);
                obj["Id"] = JsonValue.CreateNumberValue(entry.Id);
                obj["Level"] = JsonValue.CreateNumberValue((int)entry.Level);
                if(entry.Category != null)
                    obj["Category"] = JsonValue.CreateStringValue(entry.Category);
                if(entry.Message != null)
                    obj["Message"] = JsonValue.CreateStringValue(entry.Message);
                arr.Add(obj);
            }

            return arr.Stringify();
        }

        List<Entry> ConvertJsonToEntries(string content)
        {
            List<Entry> entries = new List<Entry>();

            if (!string.IsNullOrEmpty(content))
            {
                JsonArray arr = JsonArray.Parse(content);
                foreach (var jEntry in arr)
                {
                    var obj = jEntry.GetObject();
                    var entry = new Entry();
                    entry.Created = new DateTime((long)obj.GetNamedNumber("Created"));
                    entry.Id = (int)obj.GetNamedNumber("Id");
                    entry.Level = (Level)obj.GetNamedNumber("Level");
                    if(obj.ContainsKey("Category"))
                        entry.Category = obj.GetNamedString("Category");
                    if (obj.ContainsKey("Message"))
                        entry.Message = obj.GetNamedString("Message");
                    entries.Add(entry);
                }
            }

            return entries;
        }

        SemaphoreSlim _dumpWorkerSlim = new SemaphoreSlim(1);

        public override async Task DumpAsync()
        {
            _dumpWorkerSlim.Wait();
            try
            {
                var service = GetService();
                if (service == null)
                    return;

                var folder = await GetFolderAsync();

                await CleanupDumpFilesAsync(folder);

                var files = await folder.GetFilesAsync();
                var dumpFiles = CreateDumpFileList(files);

                foreach (var dumpFile in dumpFiles)
                {
                    InternalTrace(Entry.CreateVerbose("DumpAsync dumpFile: " + dumpFile.Name));
                    string content = null;
                    try
                    {
                        content = await FileIO.ReadTextAsync(dumpFile.File);
                    }
                    catch (Exception ex)
                    {
                        InternalTrace(Entry.CreateError("DumpAsync ReadTextAsync failed: " + ex.Message));
                    }

                    try
                    {
                        await dumpFile.File.DeleteAsync();
                    }
                    catch (Exception ex)
                    {
                        InternalTrace(Entry.CreateError("DumpAsync DeleteAsync failed: " + ex.Message));
                    }

                    // nothing todo here?
                    if (content == null)
                        continue;

                    bool success = false;
                    try
                    {
                        var entries = ConvertJsonToEntries(content);

                        await DumpEntriesAsync(service, entries);
                        success = true;
                    }
                    catch (UnauthorizedAccessException unauthex)
                    {
                        // application is configured in a wrong way
                        throw new UnauthorizedAccessException(
                            "Log-Dumping requires the following capabilities: Internet (Client) and Private Networks. Please add the capabilities or disable the dump service by setting the ServiceUrl to null in the PortiLog.Config.xml",
                            unauthex);
                    }
                    catch (Exception ex)
                    {
                        // dump service is not available -> a normal state
                        InternalTrace(Entry.CreateInfo("Dump Service is not available. Details: " + ex.Message));
                    }

                    if (!success)
                    {
                        // restore dump file because dump failed
                        var restoreFile = await folder.CreateFileAsync(dumpFile.Name);
                        await FileIO.WriteTextAsync(restoreFile, content);
                        // dump service is not working correctly, so exit here
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                InternalTrace(Entry.CreateError("DumpAsync failed: " + ex.Message));
            }
            finally
            {
                _dumperTask = null;
                _dumpWorkerSlim.Release();
            }
        }


    }
}