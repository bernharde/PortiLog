using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Windows.Foundation;
using Windows.Storage;
using Windows.System.Threading;
using System.Text;
using System.Reflection;
using System.Windows;

namespace PortiLog.WindowsPhone81
{
    public class DbListener : ListenerBase, IFileListener
    {
        int _maxEntryCount = 1000;

        public int MaxEntryCount
        {
            get { return _maxEntryCount; }
            set { _maxEntryCount = value; }
        }
        
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
                var title = PhoneUtil.GetAppName();
                var filename = LogUtil.RemoveInvalidPathChars(title);
                return filename + ".log.txt";
            }
        }

        public async Task PrepareFileAsync()
        {
            var dc = await LogDbContext.CreateAsync();
            StorageFile _storageFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(FileName, CreationCollisionOption.ReplaceExisting);

            using (var stream = await _storageFile.OpenStreamForWriteAsync())
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    foreach (var entry in await dc.Table<DbEntry>().ToListAsync())
                    {
                        var newFormatedLine = string.Format(_format, entry.Created, entry.Level, entry.Category, entry.Message);
                        writer.WriteLine(newFormatedLine);
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the listener
        /// </summary>
        /// <param name="name">the name of the listener. The file name of the log will be "(name).log"</param>
        public DbListener(string name) : base(name)
        {
        }

        public override Task WriteEntriesAsync(List<Entry> entries)
        {
            var task = Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        var db = await LogDbContext.CreateAsync();
                        List<DbEntry> dbEntries = new List<DbEntry>();
                        foreach (var entry in entries)
                        {
                            var dbEntry = new DbEntry()
                            {
                                Id = entry.Id,
                                Created = entry.Created,
                                Category = entry.Category,
                                Level = entry.Level,
                                Message = EntryFormatter.FormatMessage(entry)
                            };

                            //db.InsertAsync(dbEntry);
                            dbEntries.Add(dbEntry);
                        }
                        await db.InsertAllAsync(dbEntries);
                        //db.SubmitChanges();

                        var entryCount = await db.Table<DbEntry>().CountAsync();
                        if (entryCount > this.MaxEntryCount)
                        {
                            var deleteCount = entryCount - this.MaxEntryCount;
                            var deleteThem = await db.Table<DbEntry>().OrderBy(e => e.DbEntryId).Take(deleteCount).ToListAsync();
                            foreach(var deleteOne in deleteThem)
                            {
                                await db.DeleteAsync(deleteOne);
                            }

                            //try
                            //{
                            //    db.(System.Data.Linq.ConflictMode.ContinueOnConflict);
                            //}
                            //catch (Exception ccex)
                            //{
                            //    InternalTrace(Entry.CreateError(("WriteEntriesAsync conflict ignore: " + ccex.Message)));

                                //// ignore conflicts
                                //foreach (var conflict in db.ChangeConflicts)
                                //{
                                //    if (conflict.IsDeleted)
                                //    {
                                //        conflict.Resolve(RefreshMode.KeepCurrentValues);
                                //    }
                                //    else
                                //    {
                                //        conflict.Resolve(RefreshMode.OverwriteCurrentValues);
                                //    }
                                //}
                            //}
                        }

                        if (DumpEnabled && !NeverDump && _dumpTask == null)
                            _dumpTask = Task.Run(() => DumpAsync());
                    }
                    catch (Exception ex)
                    {
                        InternalTrace(Entry.CreateError("AddLine failed: " + ex.Message));
                    }
                });
            return task;
        }

        Task _dumpTask;

        public override bool DumpSupported
        {
            get
            {
                return true;
            }
        }

        static SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        //[DebuggerStepThrough]
        public async override Task DumpAsync()
        {
            try
            {
                await _semaphore.WaitAsync();

                var service = GetService();
                if (service == null)
                    return;

                // prepare for further dumps
                _dumpTask = null;

                var db = await LogDbContext.CreateAsync();
                while (true)
                {
                    // only take 30 at one time to limit the onetime transfer volume
                    var notDumped = await (from e in db.Table<DbEntry>()
                                           where e.Dumped == false
                                           orderby e.DbEntryId
                                           select e).Take(30).ToListAsync();

                    int count = notDumped.Count();

                    if (count == 0)
                        return;

                    var entries = new List<Entry>();
                    foreach (var entry in notDumped)
                    {
                        entries.Add(new Entry()
                        {
                            Id = entry.Id
                             ,
                            Created = entry.Created
                             ,
                            Category = entry.Category
                             ,
                            Level = entry.Level
                             ,
                            Message = entry.Message
                        });
                    }

                    await DumpEntriesToServiceAsync(service, entries);

                    foreach (var entry in notDumped)
                    {
                        entry.Dumped = true;
                    }

                    try
                    {
                        await db.UpdateAllAsync(notDumped);
                    }
                    catch (Exception ccex)
                    {
                        InternalTrace(Entry.CreateError(("StartDumpAsync conflict ignore: " + ccex.Message)));

                        //// ignore conflicts
                        //foreach (var conflict in db.ChangeConflicts)
                        //{
                        //    if (conflict.IsDeleted)
                        //    {
                        //        conflict.Resolve(RefreshMode.KeepCurrentValues);
                        //    }
                        //    else
                        //    {
                        //        conflict.Resolve(RefreshMode.OverwriteCurrentValues);
                        //    }
                        //}
                    }

                    await Task.Delay(500);
                }
            }
            catch (Exception ex)
            {
                InternalTrace(Entry.CreateError("StartDumpAsync failed: " + ex.Message));
                // do nothing here;
            }
            finally
            {
                _dumpTask = null;
                _semaphore.Release();
            }
        }

        public async Task DumpEntriesToServiceAsync(ServiceClient service, List<Entry> entries)
        {
            var engine = this.Engine;
            if (engine != null)
            {
                var dumpData = await engine.CreateDumpDataAsync();
                dumpData.Entries = entries;

                await service.PostDumpData(dumpData);
            }
        }

    }
}