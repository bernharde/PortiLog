using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace PortiLog
{
    /// <summary>
    /// Defines the logging receiver service
    /// </summary>
    [ServiceContract]
    public interface IService
    {
        [OperationContract(AsyncPattern = true)]
        IAsyncResult BeginDump(DumpData dumpData, AsyncCallback callback, Object state);
        void EndDump(IAsyncResult result);
    }
}
