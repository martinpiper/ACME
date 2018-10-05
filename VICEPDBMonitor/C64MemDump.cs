using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace VICEPDBMonitor
{
    class C64MemDump : IMemDump
    {
        byte[] mMemoryC64 = new byte[65536];
        RegisterSet6510 mRegisterSet;

        public byte GetMemory(int addr)
        {
            return mMemoryC64[addr & 0xFFFF];
        }

        public byte GetMemory(int addr, Enum bank)
        {
            return mMemoryC64[addr & 0xFFFF]; //not handled yet
        }

        public int GetMicroDumpStringLenght()
        {
            return 16;
        }

        public string GetMicroDump(int addr)
        {
            string ret;
            ret = " >(" + GetMemory(addr).ToString("X2") + " " + GetMemory(addr + 1).ToString("X2") + " " + GetMemory(addr + 2).ToString("X2") + " " + GetMemory(addr + 3).ToString("X2") + ")<";
            return ret;
        }

        public string GetRichMicroDump(int addr)
        {
            string ret;
            ret = " >(" + GetMemory(addr).ToString("X2") + " " + GetMemory(addr + 1).ToString("X2") + " " + GetMemory(addr + 2).ToString("X2") + " " + GetMemory(addr + 3).ToString("X2") + ")<";
            return ret;
        }

        public void RefreshDump(Dispatcher dispatch)
        {
            VICECOMManager vcom = VICECOMManager.getVICEComManager();
            vcom.addBinaryMemCommand(0, 0xFFFF, full_mem_dump, null, dispatch);
        }

        public void SetRegisterSet(IRegisterSet set)
        {
            mRegisterSet = set as RegisterSet6510;
            if(mRegisterSet == null)
            {
                throw new Exception("Register set needs to be a RegisterSet6510");
            }
        }

        void full_mem_dump(byte[] reply, object userData)
        {
            Buffer.BlockCopy(reply, 0, mMemoryC64, 0, reply.Length);
        }
    }
}
