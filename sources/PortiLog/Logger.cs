using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortiLog
{
    /// <summary>
    /// Defines the logger to write log data
    /// </summary>
    public static class Logger
    {
        static EngineBase _engine;

        public static EngineBase Engine
        {
            get 
            {
                if (_engine == null)
                    _engine = CreateEngine();
                return _engine; 
            }
        }

        public static void Use(EngineBase engine)
        {
            _engine = engine;
        }

        static EngineBase CreateEngine()
        {
            EngineBase engine;

            Type windowsPhone = Type.GetType("PortiLog.WindowsPhone.Engine, PortiLog.WindowsPhone", false);
            Type windowsPhone81 = Type.GetType("PortiLog.WindowsPhone81.Engine, PortiLog.WindowsPhone81", false);
            Type windowsStore = Type.GetType("PortiLog.WindowsStore.Engine, PortiLog.WindowsStore", false);

            if (windowsPhone != null)
                engine = (EngineBase)Activator.CreateInstance(windowsPhone);
            else if (windowsStore != null)
                engine = (EngineBase)Activator.CreateInstance(windowsStore);
            else if (windowsPhone81 != null)
                engine = (EngineBase)Activator.CreateInstance(windowsPhone81);
            else
                throw new Exception("no engine avaiable");

            return engine;
        }

        public static void Verbose(string message)
        {
            Engine.Verbose(message);
        }

        public static void Info(string message)
        {
            Engine.Info(message);
        }

        public static void Error(string message)
        {
            Engine.Error(message);
        }

        public static void Error(string message, Exception exception)
        {
            Engine.Error(message, exception);
        }

        public static void Error(Exception exception)
        {
            Engine.Error(exception);
        }

        public static void Critical(string message)
        {
            Engine.Critial(message);
        }

        public static void WriteEntry(Entry entry)
        {
            Engine.WriteEntry(entry);
        }


    }
}
