using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VICEPDBMonitor
{
    class C64RAM
    {
        static C64RAM g_64RAM = null;

        byte[] m_RAM;

        public static C64RAM getInstace()
        {
            if (g_64RAM == null)
            {
                g_64RAM = new C64RAM();
            }
            return g_64RAM;
        }

        public C64RAM()
        {
            m_RAM = new byte[64 * 1024];
        }

        public byte[] getRAM()
        {
            return m_RAM;
        }
/*
        public int injectRAMFromVICE_m(string cmd)
        {
            string[] lines = cmd.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            string[] trippleSpace = new string[] { "   " };
            string[] doubleSpace = new string[] { "  " };
            char[] singleSpace = new char[] { ' ' };
            int bytesExtracted = 0;
            foreach (string line in lines)
            {
                string stippedLine = VICETextConverter.stipAnyMsgBlocksAtStart(line);
                //.WriteLine("looking at \"" + stippedLine + "\"");
                if (stippedLine.Length > 0)
                {
                    if (stippedLine[0] == '>') //make sure it is a m result line
                    {
                        string[] dataText = stippedLine.Split(trippleSpace, StringSplitOptions.None);
                        string[] parts = dataText[0].Split(doubleSpace, StringSplitOptions.None);
                        //Console.WriteLine("> found, " + parts.Length + " parts found");
                        if (parts.Length > 1)
                        {
                            int oldBytesExtracted = bytesExtracted;
                            int addr = VICETextConverter.extractAddress(parts[0]);
                            for (int part = 1; part < parts.Length; ++part)
                            {
                                string[] bytes = parts[part].Split(singleSpace, StringSplitOptions.RemoveEmptyEntries);
                                foreach (string b in bytes)
                                {
                                    byte value = VICETextConverter.convertToByte(b);
                                    m_RAM[addr] = value;
                                    addr++;
                                    bytesExtracted++; //count the bytes read
                                }
                            }
                            //Console.WriteLine((bytesExtracted - oldBytesExtracted) + " bytes found : total = " + bytesExtracted);
                        }
                    }
                }
            }
            return bytesExtracted;
        }*/

        public void injectBinaryData(int address, byte[] data)
        {
            Buffer.BlockCopy(data, 0, m_RAM, address, data.Length);
        }
    }
}
