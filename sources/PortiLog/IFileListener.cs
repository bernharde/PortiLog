using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortiLog
{
    public interface IFileListener
    {
        string FileName { get; }
        Task PrepareFileAsync();
    }
}
