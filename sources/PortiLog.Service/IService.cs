using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace PortiLog.Service
{
    [ServiceContract]
    public interface IService
    {
        [OperationContract]
        void Dump(DumpData dumpData);
    }
}
