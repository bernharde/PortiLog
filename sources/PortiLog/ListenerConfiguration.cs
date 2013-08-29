using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PortiLog
{
    public class ListenerConfiguration
    {
        public ListenerConfiguration()
        {
            EndLevel = Level.Critical;
        }

        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("startLevel")]
        public Level StartLevel { get; set; }

        [XmlAttribute("endLevel")]
        public Level EndLevel { get; set; }

        [XmlAttribute("serviceUrl")]
        public string ServiceUrl { get; set; }

        List<string> _categories;

        [XmlArray("categories")]
        [XmlArrayItem("add")]
        public List<string> Categories
        {
            get
            {
                if (_categories == null)
                    _categories = new List<string>();
                return _categories;
            }
            set { _categories = value; }
        }
    }
}
