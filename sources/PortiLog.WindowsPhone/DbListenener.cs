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
using System.Data.Linq;
using System.Reflection;
using System.Windows;

namespace PortiLog.WindowsPhone
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
                var title = PhoneUtil.GetAppTitle();
                var filename = LogUtil.RemoveInvalidPathChars(title);
                return filename + ".log.txt";
            }
        }

        public async Task PrepareFileAsync()
        {
            using (var dc = CreateLogDbContext())
            {
                StorageFile _storageFile = await ApplicationData.Current.LocalFolder.CreateFileAsync(FileName, CreationCollisionOption.ReplaceExisting);

                using (var stream = await _storageFile.OpenStreamForWriteAsync())
                {
                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        foreach (var entry in dc.Entries)
                        {
                            var newFormatedLine = string.Format(_format, entry.Created, entry.Level, entry.Category, entry.Message);
                            writer.WriteLine(newFormatedLine);
                        }
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
            var task = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        using (var db = CreateLogDbContext())
                        {
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

                                db.Entries.InsertOnSubmit(dbEntry);
                            }

                            db.SubmitChanges();

                            var entryCount = db.Entries.Count();
                            if (entryCount > this.MaxEntryCount)
                            {
                                var deleteCount = entryCount - this.MaxEntryCount;
                                var deleteThem = db.Entries.OrderBy(e => e.DbEntryId).Take(deleteCount).ToList();
                                db.Entries.DeleteAllOnSubmit(deleteThem);

                                try
                                {
                                    db.SubmitChanges(System.Data.Linq.ConflictMode.ContinueOnConflict);
                                }
                                catch (ChangeConflictException ccex)
                                {
                                    InternalTrace(Entry.CreateError(("WriteEntriesAsync conflict ignore: " + ccex.Message)));

                                    // ignore conflicts
                                    foreach (var conflict in db.ChangeConflicts)
                                    {
                                        if (conflict.IsDeleted)
                                        {
                                            conflict.Resolve(RefreshMode.KeepCurrentValues);
                                        }
                                        else
                                        {
                                            conflict.Resolve(RefreshMode.OverwriteCurrentValues);
                                        }
                                    }
                                }
                            }

                            
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

        LogDbContext CreateLogDbContext()
        {
            var db = new LogDbContext("Data Source=isostore:/PortiLogDb.sdf");
            if (db.DatabaseExists() == false)
            {
                db.CreateDatabase();
            }
            return db;
        }

        Task _dumpTask;

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

                using (var db = CreateLogDbContext())
                {
                    while (true)
                    {
                        // only take 30 at one time to limit the onetime transfer volume
                        var notDumped = (from e in db.Entries
                                         where !e.Dumped
                                         orderby e.DbEntryId
                                         select e).Take(30).ToList();

                        int count = notDumped.Count();

                        if (count == 0)
                            return;

                        var entries = new List<Entry>();
                        foreach (var entry in notDumped)
                        {
                            entries.Add(new Entry()
                            {
                                 Id = entry.Id
                                 , Created = entry.Created
                                 , Category = entry.Category
                                 , Level = entry.Level
                                 , Message = entry.Message
                            });
                        }

                        await DumpEntriesToServiceAsync(service, entries);

                        foreach (var entry in notDumped)
                        {
                            entry.Dumped = true;
                        }

                        try
                        {
                            db.SubmitChanges(System.Data.Linq.ConflictMode.ContinueOnConflict);
                        }
                        catch (ChangeConflictException ccex)
                        {
                            InternalTrace(Entry.CreateError(("StartDumpAsync conflict ignore: " + ccex.Message)));

                            // ignore conflicts
                            foreach (var conflict in db.ChangeConflicts)
                            {
                                if (conflict.IsDeleted)
                                {
                                    conflict.Resolve(RefreshMode.KeepCurrentValues);
                                }
                                else
                                {
                                    conflict.Resolve(RefreshMode.OverwriteCurrentValues);
                                }
                            }
                        }

                        await Task.Delay(500);
                    }
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