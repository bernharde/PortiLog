using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PortiLog
{
    [DataContract]
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

        [DataMember]
        public DateTime Created { get; set; }
        [DataMember]
        public Level Level { get; set; }
        [DataMember]
        public string Category { get; set; }
        [DataMember]
        public string Message { get; set; }
        [DataMember]
        public int Id { get; set; }

        public Exception Exception { get; set; }
    }
}
