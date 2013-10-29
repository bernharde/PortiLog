using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortiLog
{
    public abstract class ListenerBase
    {
        /// <summary>
        /// Name of the current event listener
        /// </summary>
        string _name;

        public string Name
        {
            get { return _name; }
        }

        internal bool Configured { get; set; }

        /// <summary>
        /// Initializes a new instance of the listener
        /// </summary>
        /// <param name="engine">the engine</param>
        /// <param name="name">the name of the listener. The file name of the log will be "(name).log"</param>
        public ListenerBase(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name cannot be null or empty in listener constructor");
            this._name = name;
        }

        EngineBase _engine;

        public EngineBase Engine
        {
            get
            {
                return _engine;
            }
            internal set
            {
                _engine = value;
            }
        }

        ListenerConfiguration _configuration;

        public ListenerConfiguration Configuration
        {
            get 
            { 
                return _configuration; 
            }
            set 
            { 
                _configuration = value;
                Reset();
            }
        }

        ListenerConfiguration _defaultConfiguration;

        protected virtual ListenerConfiguration GetConfiguration()
        {
            if (Configuration == null)
            {
                if (_defaultConfiguration == null)
                    _defaultConfiguration = CreateDefaultConfiguration();
                return _defaultConfiguration;
            }
            else
                return Configuration;
        }


        public virtual ListenerConfiguration CreateDefaultConfiguration()
        {
            _defaultConfiguration = new ListenerConfiguration();
            return _defaultConfiguration;
        }

        public virtual void Reset()
        {
            _neverDump = false;
            _service = null;
        }

        public virtual bool DumpSupported
        {
            get
            {
                return false;
            }
        }

        public virtual async Task DumpAsync()
        {
            await Task.Delay(0);
            throw new NotSupportedException("DumpAsync is not supported");
        }

        public virtual async Task UpdateAsync(List<Entry> entries)
        {
            var configuration = GetConfiguration();

            List<Entry> writingEntries = new List<Entry>();
            // check and write all entries
            foreach (var entry in entries)
            {
                // check, if we need to write this category
                if (configuration.Categories.Count == 0 || configuration.Categories.Contains(entry.Category))
                {
                    // check, if we need to write this level
                    if (configuration.StartLevel <= entry.Level && configuration.EndLevel >= entry.Level)
                    {
                        writingEntries.Add(entry);
                    }
                }
            }

            if (writingEntries.Count > 0)
            {
                await WriteEntriesAsync(writingEntries);
            }
        }

        public abstract Task WriteEntriesAsync(List<Entry> entries);


        protected virtual void InternalTrace(Entry entry)
        {
            var engine = this.Engine;
            if (engine != null)
            {
                engine.InternalTrace(entry);
            }
        }

        public virtual async Task DumpEntriesAsync(List<Entry> entries)
        {
            if(!DumpSupported)
                throw new NotSupportedException("listener cannot dump");
            var service = GetService();
            await DumpEntriesAsync(service, entries);
        }

        protected virtual async Task DumpEntriesAsync(IService service, List<Entry> entries)
        {
            var engine = this.Engine;
            if (engine != null)
            {
                var dumpData = await engine.CreateDumpDataAsync();
                dumpData.Entries = entries;

                await Task.Factory.FromAsync(service.BeginDump,
                                                   service.EndDump,
                                                   dumpData, null);
            }
        }

        bool _neverDump;

        protected bool NeverDump
        {
            get { return _neverDump; }
            set { _neverDump = value; }
        }

        bool _dumpEnabled = true;

        public bool DumpEnabled
        {
            get { return _dumpEnabled; }
            set { _dumpEnabled = value; }
        }

        IService _service;

        public IService GetService()
        {
            if (_service != null)
                return _service;

            if (_neverDump)
                return null;

            var configuration = GetConfiguration();

            if (string.IsNullOrEmpty(configuration.ServiceUrl))
            {
                _neverDump = true;
                return null;
            }

            _service = ServiceClient.CreateChannel(configuration.ServiceUrl);
            return _service;
        }

        public EntryFormatter EntryFormatter { get; set; }
    }
}
