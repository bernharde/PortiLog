using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortiLog
{
    public class ListenerEventArgs
    {
        public Entry Entry { get; private set; }

        public ListenerEventArgs(Entry entry)
        {
            this.Entry = entry;
        }
    }
}
