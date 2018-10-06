using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VICEPDBMonitor
{
    interface IRegisterSet
    {
        bool SetFromString(string emulator);
        int GetPC();
        int GetRegister(Enum registerID);
        void SetRegister(Enum registerID, int value);
    }
}
