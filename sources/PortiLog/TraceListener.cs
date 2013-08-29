using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortiLog
{
    public class TraceListener : ListenerBase
    {
        public TraceListener(string name) : base(name)
        {
        }

        List<Entry> _entries;

        public List<Entry> Entries
        {
            get 
            {
                if (_entries == null)
                    _entries = new List<Entry>();
                return _entries; 
            }
        }

        public override async Task WriteEntriesAsync(List<Entry> entries)
        {
            await Task.Factory.StartNew(() => _entries = entries);
        }
    }
}
