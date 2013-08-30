using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PortiLog
{
    public class Util
    {
        public static bool IsFullyQualifiedName(string typeName)
        {
            return typeName.Contains(".");
        }
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// change is the way 
        /// from typeName = "FileListener"
        /// and baseType: PortiLog.WindowsStore.Test.UnitTest, PortiLog.WindowsStore.Test, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
        /// to: PortiLog.WindowsStore.Test.FileListener, PortiLog.WindowsStore.Test, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
        /// </remarks>
        public static string BuildFullyQualifiedName(Type baseType, string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentNullException("typeName cannot be null or empty!");

            // check if is already is a fully
            if (IsFullyQualifiedName(typeName))
                return typeName;

            var fully = baseType.AssemblyQualifiedName;
            var parts = fully.Split(',');
            var classNameWithNameSpace = parts[0];

            var nameSpaceName = classNameWithNameSpace.Substring(0, classNameWithNameSpace.LastIndexOf(".")+1);
            var newClassNameWithNameSpace = nameSpaceName + typeName;
            parts[0] = newClassNameWithNameSpace;

            var newFully = string.Join(",", parts);
            return newFully;
        }

        public static string RemoveInvalidPathChars(string value)
        {
            StringBuilder validPath = new StringBuilder();
            var invalidChars = Path.GetInvalidPathChars();
            var invalidChars2 = Path.GetInvalidFileNameChars();
            foreach (var c in value)
            {
                if (invalidChars.Contains(c) || invalidChars2.Contains(c) || c == '&' || c == '#')
                    validPath.Append('_');
                else
                    validPath.Append(c);
            }
            string sValidPath = validPath.ToString();

            while (sValidPath.Contains(".."))
            {
                sValidPath = sValidPath.Replace("..", ".");
            }

            return sValidPath;
        }

        public static T Copy<T>(T obj)
        {
            if (obj == null)
                return obj;

            T copy = default(T);
            XmlSerializer ser = new XmlSerializer(typeof(T));
            using (MemoryStream ms = new MemoryStream())
            {
                ser.Serialize(ms, obj);
                ms.Flush();
                ms.Position = 0;
                copy = (T)ser.Deserialize(ms);
                return copy;
            }
        }

        public static string ToXml<T>(T obj)
        {
            if (obj == null)
                return null;

            XmlSerializer ser = new XmlSerializer(typeof(T));
            using (MemoryStream ms = new MemoryStream())
            {
                ser.Serialize(ms, obj);
                ms.Flush();
                ms.Position = 0;
                using (var reader = new StreamReader(ms))
                {
                    var result = reader.ReadToEnd();
                    return result;
                }
            }
        }

        public static T FromXml<T>(string xml)
        {
            if (xml == null)
                return default(T);

            XmlSerializer ser = new XmlSerializer(typeof(T));
            
            using (StringReader r = new StringReader(xml))
            {
                var copy = (T)ser.Deserialize(r);
                return copy;
            }
        }
    }
}
