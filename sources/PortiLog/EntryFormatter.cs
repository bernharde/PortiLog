using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortiLog
{
    public class EntryFormatter
    {
        public bool IncludeExceptionDetails { get; set; }

        public virtual string FormatMessage(Entry entry)
        {
            if (entry == null)
                return null;
            if (entry.Exception == null)
                return entry.Message;

            string formatted;
            if (IncludeExceptionDetails)
            {
                formatted = string.Format("{0} - {1}", entry.Message, entry.Exception.ToString());
            }
            else
            {
                formatted = string.Format("{0} - {1}: {2}", entry.Message, entry.Exception.GetType().Name, entry.Exception.Message);
            }
            
            return formatted;
        }
    }
}
