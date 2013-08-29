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
        bool _configurationRead;

        public async Task<ListenerConfiguration> GetConfigurationAsync()
        {
            if (!_configurationRead)
            {
                var engine = Engine;
                if (engine != null)
                {
                    var config = await Engine.GetConfigurationAsync();
                    _configuration = config.Listeners.FirstOrDefault(l => l.Name == Name);
                }

                // if config is not there use default
                if (_configuration == null)
                    _configuration = new ListenerConfiguration();

                _configurationRead = true;
            }
            return _configuration;
        }

        public virtual void UpdateConfiguration()
        {
            
        }

        public virtual async Task UpdateAsync(List<Entry> entries)
        {
            var configuration = await GetConfigurationAsync();

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
    }
}
