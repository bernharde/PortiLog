using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace PortiLog.WindowsStore.SampleApp.BackgroundTasks
{
    public sealed class SampleBackgroundTask : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            Logger.Info("SampleBackgroundTask started");
            Task.Delay(1000).Wait();
            Logger.Info("SampleBackgroundTask finished");
            // call flush to ensure that all entries are written to the log file
            Logger.Engine.Flush();
        }
    }
}
