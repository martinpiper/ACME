using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace VICEPDBMonitor
{
    interface IMemDump
    {
        void SetRegisterSet(IRegisterSet set);
        void RefreshDump(Dispatcher dispatch);
        byte GetMemory(int addr);
        byte GetMemory(int addr, Enum bank);
        string GetMicroDump(int addr);
        string GetRichMicroDump(int addr);
        int GetMicroDumpStringLenght();
    }
}
