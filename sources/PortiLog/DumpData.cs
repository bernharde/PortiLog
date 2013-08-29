using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PortiLog
{
    /// <summary>
    /// Defines logging data to send to the logging service
    /// </summary>
    [DataContract]
    public class DumpData
    {
        [DataMember]
        public string ApplicationName { get; set; }
        [DataMember]
        public string ApplicationVersion { get; set; }
        [DataMember]
        public string UserId { get; set; }
        [DataMember]
        public string DeviceId { get; set; }

        List<Entry> _entries;
        [DataMember]
        public List<Entry> Entries
        {
            get
            {
                if (_entries == null)
                    _entries = new List<Entry>();
                return _entries;
            }
            set { _entries = value; }
        }
    }
}
