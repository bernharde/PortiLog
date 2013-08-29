using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.System;
using Windows.UI.Core;

namespace PortiLog.WindowsPhone.SampleApp
{
    class ListenerViewModel : ListenerBase, INotifyPropertyChanged
    {
        static ListenerViewModel _current;

        public static ListenerViewModel Current
        {
            get 
            { 
                if(_current == null)
                {
                    _current = new ListenerViewModel();
                    Logger.Engine.RegisterListener(_current);
                }
                return _current; 
            }
        }

        public ListenerViewModel() : base("ListenerViewModel")
        {
            StartInitialize();
        }

        public async void StartInitialize()
        {
            var engine = (WindowsPhone.Engine)Logger.Engine;
            var dbListener = (WindowsPhone.DbListener)engine.DefaultListener;
            var config = await dbListener.GetConfigurationAsync();
            DumpServiceUrl = config.ServiceUrl;
        }

        object _syncRoot = new object();

        string _logData;

        public string LogData
        {
            get { return _logData; }
            set
            {
                if (_logData == value)
                    return;
                _logData = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("LogData"));
            }
        }

        string _dumpServiceUrl;

        public string DumpServiceUrl
        {
            get { return _dumpServiceUrl; }
            set
            {
                if (_dumpServiceUrl == value)
                    return;
                _dumpServiceUrl = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("DumpServiceUrl"));
            }
        }

        string _dumpTestMessage;

        public string DumpTestMessage
        {
            get { return _dumpTestMessage; }
            set
            {
                if (_dumpTestMessage == value)
                    return;
                _dumpTestMessage = value;
                if (PropertyChanged != null)
                    PropertyChanged(this, new PropertyChangedEventArgs("DumpTestMessage"));
            }
        }

        string _format = "{0:HH\\:mm\\:ss}\tLevel: {1}\tCat: {2}\tMsg: '{3}'";

        /// <summary>
        /// Contains max 100 entries and are ordered in reverve with the latest on top
        /// </summary>
        List<Entry> _uiEntries = new List<Entry>();

        public override Task WriteEntriesAsync(List<Entry> entries)
        {
            var task = Task.Factory.StartNew(() =>
                {
                    lock (_syncRoot)
                    {
                        foreach (var entry in entries)
                            _uiEntries.Insert(0, entry);

                        if (_uiEntries.Count > 100)
                            _uiEntries.RemoveRange(100, _uiEntries.Count - 100);
                    }


                    var dispatcher = Deployment.Current.Dispatcher;
                    
                    var subTask = dispatcher.BeginInvoke(() =>
                        {
                            lock (_syncRoot)
                            {
                                StringBuilder sb = new StringBuilder();
                                foreach (var entry in _uiEntries)
                                {
                                    var newFormatedLine = string.Format(_format, entry.Created, entry.Level, entry.Category, entry.Message);
                                    sb.AppendLine(newFormatedLine);
                                }

                                LogData = sb.ToString();
                            }
                        });
                });
            return task;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        int _offSet = 0;

        public void LogWrite150Entries()
        {
            int tick = Environment.TickCount;
            for (int i = 0; i < 150; i++)
            {
                _offSet++;
                Logger.Info("Info " + _offSet.ToString());
            }
            int duration = Environment.TickCount - tick;
            System.Diagnostics.Debug.WriteLine("LogWrite150Entries: " + duration.ToString());
            return;
        }

        public void LogSingleEntryWithCategory()
        {
            _offSet++;
            Logger.WriteEntry(new Entry() { Category = "mycat1", Message = "Entry with Category " + _offSet.ToString() });
        }

        public async void StartNavigateToDumpServiceUrl()
        {
            await Launcher.LaunchUriAsync(new Uri(this.DumpServiceUrl));
        }

        public async void StartTestDumpService()
        {
            try
            {
                DumpTestMessage = "Test started: "  + DateTime.Now.ToString("HH:mm:ss");
                await Logger.Engine.DumpServiceTestAsync();
                DumpTestMessage = "Ok " + DateTime.Now.ToString("HH:mm:ss");
            }
            catch (Exception ex)
            {
                DumpTestMessage = ex.Message;
            }
        }

        
    }
}
