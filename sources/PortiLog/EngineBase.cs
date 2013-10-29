using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PortiLog
{
    /// <summary>
    /// Defines the logging engine
    /// </summary>
    public abstract class EngineBase
    {
        public EngineBase()
        {
        }

        public void Verbose(string message)
        {
            WriteEntry(new Entry() { Level = Level.Verbose, Message = message });
        }

        public void Info(string message)
        {
            WriteEntry(new Entry() { Level = Level.Info, Message = message });
        }

        public void Error(string message)
        {
            Error(message, null);
        }

        public void Error(Exception exception)
        {
            Error(null, exception);
        }

        public void Error(string message, Exception exception)
        {
            WriteEntry(new Entry() { Level = Level.Error, Message = message, Exception = exception });
        }

        public void Critial(string message)
        {
            WriteEntry(new Entry() { Level = Level.Critical, Message = message });
        }

        public virtual T FindListener<T>() where T: class
        {
            foreach (var listener in _listeners)
            {
                var found = listener as T;
                if (found != null)
                    return found;
            }
            return default(T);
        }

        List<ListenerBase> _listeners = new List<ListenerBase>();

        System.Threading.SemaphoreSlim semaphoreSlim = new System.Threading.SemaphoreSlim(1);

        public virtual async Task DumpAsync()
        {
            foreach (var listener in _listeners)
            {
                if(listener.DumpSupported)
                {
                    await listener.DumpAsync();
                }
            }
        }

        public virtual async Task DumpServiceTestAsync()
        {
            foreach (var listener in RegisteredListeners)
            {
                if (listener.DumpSupported)
                {
                    var entries = new List<Entry>();
                    entries.Add(new Entry() { Category = "DumpServiceTest", Level = Level.Verbose, Message = "Dump Service Test Entry" });
                    await listener.DumpEntriesAsync(entries);
                }
            }
        }
        
        public ListenerBase[] RegisteredListeners
        {
            get
            {
                return _listeners.ToArray();
            }
        }

        public virtual void RegisterListener(ListenerBase listener)
        {
            semaphoreSlim.Wait();
            if (!_listeners.Contains(listener))
            {
                listener.Engine = this;
                _listeners.Add(listener);

                listener.DumpEnabled = DumpEnabled;

                InternalTrace(Entry.CreateInfo(string.Format("Listener '{0}' registered", listener.Name)));
            }
            semaphoreSlim.Release();
        }

        public virtual void UnregisterListener(ListenerBase listener)
        {
            semaphoreSlim.Wait();
            if (!_listeners.Contains(listener))
            {
                _listeners.Remove(listener);
                listener.Engine = null;
                InternalTrace(Entry.CreateInfo(string.Format("Listener '{0}' unregistered", listener.Name)));
            }
            semaphoreSlim.Release();
        }

        List<Entry> _entriesCache = new List<Entry>();

        public object _syncRoot = new object();

        public SemaphoreSlim _entriesWritingSlim = new SemaphoreSlim(1);

        Task _entryWritingTask;

        int _entriesToWrite = 0;

        public virtual void WriteEntry(Entry entry)
        {
            InternalTrace(Entry.CreateVerbose("WriteEntry start: "));

            lock (_syncRoot)
            {
                InternalTrace(Entry.CreateVerbose("WriteEntry start in lock "));
                _entriesCache.Add(entry);
                _entriesToWrite++;

                if (_entryWritingTask == null)
                {
                    _entryWritingTask = Task.Factory.StartNew( async delegate { await UpdateListenersAsync(); });
                }
            }
            InternalTrace(Entry.CreateVerbose("WriteEntry release lock "));
        }

        public async Task UpdateListenersAsync()
        {
            InternalTrace(Entry.CreateVerbose("StartEntryWriting: slim wait"));
            _entriesWritingSlim.Wait();
            InternalTrace(Entry.CreateVerbose("StartEntryWriting: slim start"));

            // delay the worker to give the cache time to grow and to reduce listener calls
            await Task.Delay(100);

            List<Entry> entries = null;
            try
            {
                InternalTrace(Entry.CreateVerbose("StartEntryWriting: start lock"));
                lock (_syncRoot)
                {
                    InternalTrace(Entry.CreateVerbose("StartEntryWriting: in lock"));
                    _entryWritingTask = null;

                    // reset the cache
                    entries = _entriesCache;
                    _entriesCache = new List<Entry>();

                    if (entries.Count == 0)
                        return;
                }
                InternalTrace(Entry.CreateVerbose("StartEntryWriting: release lock"));

                // check config
                var configuration = await GetConfigurationAsync();

                // check, if logging is enabled
                if (configuration.LoggingEnabled)
                {
                     foreach (var listener in _listeners)
                    {
                        await listener.UpdateAsync(entries);
                    }
                }
            }
            finally
            {
                lock (_syncRoot)
                {
                    if(entries != null)
                        _entriesToWrite -= entries.Count;
                }
                _entriesWritingSlim.Release();
                InternalTrace(Entry.CreateVerbose("StartEntryWriting: release slim"));
            }
        }

        public virtual void ResetConfiguration()
        {
            ConfigurationRead = false;
        }

        bool _configurationRead;

        protected bool ConfigurationRead
        {
            get { return _configurationRead; }
            set { _configurationRead = value; }
        }

        Configuration _configuration;

        SemaphoreSlim _configurationSlim = new SemaphoreSlim(1);

        public async Task<Configuration> GetConfigurationAsync()
        {
            if (!ConfigurationRead)
                await ConfigureAsync();
            return _configuration;
        }

        public async Task ConfigureAsync()
        {
            _configurationSlim.Wait();

            if (!ConfigurationRead)
            {
                string xml = null;

                Configuration configuration = null;

                try
                {
                    xml = await LoadConfigurationFromFileAsync();
                }
                catch (Exception ex)
                {
                    InternalTrace(Entry.CreateError("LoadConfigurationFromFileAsync failed: " + ex.Message));
                }

                try
                {
                    configuration = LogUtil.FromXml<Configuration>(xml);
                }
                catch (Exception ex)
                {
                    InternalTrace(Entry.CreateError("PortiLog config file has an invalid format: " + ex.Message));
                }

                if (configuration == null)
                {
                    InternalTrace(Entry.CreateInfo("PortiLog config file not available! Default configuration is created"));
                    configuration = CreateDefaultConfiguration();
                }

                this.Configure(configuration);
            }

            _configurationSlim.Release();
        }

        public virtual void Configure(Configuration configuration)
        {
            _configuration = configuration;
            ConfigurationRead = true;

            try
            {

                // unregister all listeners configured through this method
                var listeners = _listeners.ToArray();
                foreach (var listener in listeners)
                {
                    if (listener.Configured)
                        UnregisterListener(listener);
                }

                foreach (var listenerConfiguration in configuration.Listeners)
                {
                    var listener = CreateListenerInstance(listenerConfiguration);
                    listener.Configuration = listenerConfiguration;
                    listener.EntryFormatter = CreateEntryFormatterInstance(listenerConfiguration);
                    listener.Configured = true;

                    RegisterListener(listener);
                }
            }
            catch (Exception ex)
            {
                InternalTrace(Entry.CreateError("Configure failed: " + ex.Message));
            }
        }

        ListenerBase CreateListenerInstance(ListenerConfiguration listenerConfiguration)
        {
            var baseType = this.GetType();
            string typeName = listenerConfiguration.Type;
            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentNullException("Configuration is invalid. Listener Type property cannot be null or emtpy");

            // check if the type name contains the full qualified name
            var fullyTypeName = LogUtil.BuildFullyQualifiedName(baseType, typeName);
            var listenerType = Type.GetType(fullyTypeName, false);
            if (listenerType == null)
                throw new ArgumentException("Listener type cannot be found! Type: " + fullyTypeName);

            var listener = (ListenerBase)Activator.CreateInstance(listenerType, listenerConfiguration.Name);
            return listener;
        }

        EntryFormatter CreateEntryFormatterInstance(ListenerConfiguration listenerConfiguration)
        {
            var baseType = this.GetType();
            string typeName = listenerConfiguration.EntryFormatterType;
            EntryFormatter formatter;
            if (string.IsNullOrEmpty(typeName))
            {
                formatter = new EntryFormatter();
            }
            else
            {
                // check if the type name contains the full qualified name
                var fullyTypeName = LogUtil.BuildFullyQualifiedName(baseType, typeName);
                var entryFormatterType = Type.GetType(fullyTypeName, false);
                if (entryFormatterType == null)
                    throw new ArgumentException("EntryFormatter type cannot be found! Type: " + fullyTypeName);

                formatter = (EntryFormatter)Activator.CreateInstance(entryFormatterType);
            }

            formatter.IncludeExceptionDetails = listenerConfiguration.IncludeExceptionDetails;
            return formatter;
        }

        bool _dumpEnabled = true;

        public bool DumpEnabled
        {
            get { return _dumpEnabled; }
            set 
            { 
                _dumpEnabled = value;
                foreach (var listener in _listeners)
                {
                    listener.DumpEnabled = value;
                }
            }
        }

        public abstract Configuration CreateDefaultConfiguration();

        public abstract Task<string> LoadConfigurationFromFileAsync();

        public abstract Task<DumpMetaData> GetDumpMetaDataAsync();

        public virtual async Task<DumpData> CreateDumpDataAsync()
        {
            var dumpMetaData = await GetDumpMetaDataAsync();
            var configuration = await GetConfigurationAsync();

            var dumpData = new DumpData();
            dumpData.ApplicationName =string.IsNullOrEmpty(configuration.ApplicationName)
                ? dumpMetaData.ApplicationName
                : configuration.ApplicationName;
            dumpData.ApplicationVersion = dumpMetaData.ApplicationVersion;
            dumpData.DeviceId = dumpMetaData.DeviceId;
            dumpData.UserId = dumpMetaData.UserId;
            return dumpData;
        }

        /// <summary>
        /// Wait if there are logging entries out there to write
        /// </summary>
        public void Flush()
        {
            DateTime begin = DateTime.Now;

            try
            {
                while (_entriesToWrite > 0)
                {
                    Task.WaitAll(Task.Delay(50));
                }
            }
            catch (Exception ex)
            {
                InternalTrace(Entry.CreateError("Flush failed: " + ex.Message));
            }
        }

        /// <summary>
        /// Wait if there are logging entries out there to write
        /// </summary>
        public void Flush(int timeout)
        {
            DateTime begin = DateTime.Now;

            while (_entriesToWrite > 0)
            {
                Task.WaitAll(Task.Delay(50));

                if (begin < DateTime.Now.AddSeconds(timeout))
                    break;
            }
        }

        List<Entry> _internalTrace = new List<Entry>();

        public object _internalTraceSyncRoot = new object();

        string _internalTraceFormat = "{0:yyyy-MM-dd HH\\:mm\\:ss\\:ffff}\tLevel: {1}\tCategory: {2}\tMessage: '{3}'";
        
        public void InternalTrace(Entry entry)
        {
            lock (_internalTraceSyncRoot)
            {
                _internalTrace.Add(entry);

                if (_internalTrace.Count > 1000)
                    _internalTrace.RemoveRange(0, _internalTrace.Count - 1000);
            }
        }

        public string GetInternalTraceLog()
        {
            StringBuilder sb = new StringBuilder();
            var entries = _internalTrace;
            foreach (var entry in entries)
            {
                var line = string.Format(_internalTraceFormat, entry.Created, entry.Level, entry.Category, entry.Message);
                sb.AppendLine(line);
            }
            return sb.ToString();
        }

        public string GetInternalTraceLog(Level startLevel)
        {
            StringBuilder sb = new StringBuilder();
            var entries = _internalTrace;
            foreach (var entry in entries)
            {
                if (entry.Level >= startLevel)
                {
                    var line = string.Format(_internalTraceFormat, entry.Created, entry.Level, entry.Category, entry.Message);
                    sb.AppendLine(line);
                }
            }
            return sb.ToString();
        }
    }
}
