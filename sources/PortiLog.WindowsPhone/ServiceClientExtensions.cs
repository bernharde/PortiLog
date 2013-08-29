using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortiLog.WindowsPhone
{
    public partial class ServiceClient
    {
        public Task SetDataTask(string source, string data)
        {
            var tcs = new TaskCompletionSource<string>();

            EventHandler<System.ComponentModel.AsyncCompletedEventArgs> handler = null;
            handler = (sender, e) =>
            {
                this.SetDataCompleted -= handler;

                if (e.Error != null)
                {
                    tcs.SetException(e.Error);
                }
                else
                {
                    tcs.SetResult(string.Empty);
                }
            };

            this.SetDataCompleted += handler;
            this.SetDataAsync(source, data);

            return tcs.Task;
        }
    }
}
