using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortiLog
{
    public class Entry
    {
        public Entry()
        {
            Created = DateTime.Now;
        }

        public static Entry CreateVerbose(string message)
        {
            return new Entry() { Level = Level.Verbose, Message = message };
        }

        public static Entry CreateInfo(string message)
        {
            return new Entry() { Level = Level.Info, Message = message };
        }

        public static Entry CreateError(string message)
        {
            return new Entry() { Level = Level.Error, Message = message };
        }

        public static Entry CreateCritical(string message)
        {
            return new Entry() { Level = Level.Critical, Message = message };
        }

        public DateTime Created { get; set; }
        public Level Level { get; set; }
        public string Category { get; set; }
        public string Message { get; set; }
        public int Id { get; set; }
    }
}
