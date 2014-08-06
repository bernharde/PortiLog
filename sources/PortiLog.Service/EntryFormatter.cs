using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortiLog.Service
{
    public class EntryFormatter
    {
        public const string CreatedPlaceHolder = "${Created}";
        public const string IdPlaceHolder = "${Id}";
        public const string LevelPlaceHolder = "${Level}";
        public const string CategoryPlaceHolder = "${Category}";
        public const string MessagePlaceHolder = "${Message}";

        public string Format(Entry entry)
        {
            string formatted = Configuration.Current.EntryFormat;
            Replace(ref formatted, CreatedPlaceHolder, entry.Created, Configuration.Current.DateTimeFormat);
            Replace(ref formatted, IdPlaceHolder, entry.Id);
            Replace(ref formatted, LevelPlaceHolder, entry.Level);
            Replace(ref formatted, CategoryPlaceHolder, entry.Category);
            Replace(ref formatted, MessagePlaceHolder, entry.Message);
            return formatted;
        }

        void Replace<T>(ref string formatted, string placeHolder, T value, string format)
        {
            if (formatted.IndexOf(placeHolder) > -1)
            {
                formatted = formatted.Replace(placeHolder, string.Format(format, value));
            }
        }

        void Replace<T>(ref string formatted, string placeHolder, T value)
        {
            Replace(ref formatted, placeHolder, value, "{0}");
        }
    }
}
