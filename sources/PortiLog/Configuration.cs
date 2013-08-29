using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PortiLog
{
    [XmlRoot("portiLog")]
    public class Configuration
    {
        public Configuration()
        {
            LoggingEnabled = true;
        }

        [XmlAttribute("applicationName")]
        public string ApplicationName { get; set; }

        [XmlAttribute("loggingName")]
        public bool LoggingEnabled { get; set; }

        List<ListenerConfiguration> _listeners;

        [XmlArray("listeners")]
        [XmlArrayItem("add")]
        public List<ListenerConfiguration> Listeners
        {
            get 
            {
                if (_listeners == null)
                    _listeners = new List<ListenerConfiguration>();
                return _listeners; 
            }
            set { _listeners = value; }
        }
    }
}
