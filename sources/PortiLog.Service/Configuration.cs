using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace PortiLog.Service
{
    /// <summary>
    /// Defines all configuration settings configured in web.config
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// Contains a reference to the current configuration
        /// </summary>
        private static Configuration _current;
        /// <summary>
        /// Gets a reference to the current configuration
        /// </summary>
        public static Configuration Current
        {
            get {
                if (_current == null)
                    _current = new Configuration();
                return _current; 
            }
        }

        public string DateTimeFormat
        {
            get
            {
                return GetValue<string>("DateTimeFormat", "{0:yyyy-MM-dd HH\\:mm\\:ss}");
            }
        }

        public string EntryFormat
        {
            get
            {
                return GetValue<string>("EntryFormat", "${Created}\tLevel: ${Level}\tCategory: ${Category}\tMessage: '${Message}'");
            }
        }
              
        /// <summary>
        /// Generic method to retrieve app setting from app.config / web.config
        /// </summary>
        /// <typeparam name="T">The type to retrieve</typeparam>
        /// <param name="name">the name of the app setting</param>
        /// <returns>the value</returns>
        T GetValue<T>(string name)
        {
            return GetValue<T>(name, default(T));
        }

        /// <summary>
        /// Generic method to retrieve app setting from app.config / web.config
        /// </summary>
        /// <typeparam name="T">The type to retrieve</typeparam>
        /// <param name="name">the name of the app setting</param>
        /// /// <param name="defaultValue">the default value, that should be returned if no configuration value is available</param>
        /// <returns>the value</returns>
        T GetValue<T>(string name, T defaultValue)
        {
            var value = ConfigurationManager.AppSettings[name];
            if (value != null)
                return (T)Convert.ChangeType(value, typeof(T));
            return defaultValue;
        }
    }
}