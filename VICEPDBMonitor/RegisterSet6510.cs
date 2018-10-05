using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VICEPDBMonitor
{
    enum e6510Registers
    {
        A,
        X,
        Y,
        SP,
        ST_N,
        ST_V,
        ST_B,
        ST_D,
        ST_I,
        ST_C,
        ST_Z,
        r00,
        r01,
        Line,
        Cycle,
        StopWatch
    }

    class RegisterSet6510 : IRegisterSet
    {
        int m_PC;
        int m_SP;
        int m_A;
        int m_X;
        int m_Y;
        int m_N; //negative
        int m_V; //overflow
        int m_B; //Break
        int m_D; //Decimal
        int m_I; //Interupt
        int m_C; //Carry
        int m_Z; //Zero
        int m_r00; //$00
        int m_r01; //$01
        int m_Line; //VIC
        int m_Cycle; //VIC
        int m_Stopwatch;

        public int GetPC()
        {
            return m_PC;
        }

        public int GetRegister(Enum registerID)
        {
            if((registerID is e6510Registers) == false)
            {
                throw new Exception("This requires a 6510 register enum set");
            }
            e6510Registers id = (e6510Registers)registerID;
            switch (id)
            {
                case e6510Registers.A:
                    return m_A;
                case e6510Registers.X:
                    return m_X;
                case e6510Registers.Y:
                    return m_Y;
                case e6510Registers.SP:
                    return m_SP;
                case e6510Registers.ST_N:
                    return m_N;
                case e6510Registers.ST_V:
                    return m_V;
                case e6510Registers.ST_B:
                    return m_B;
                case e6510Registers.ST_D:
                    return m_D;
                case e6510Registers.ST_I:
                    return m_I;
                case e6510Registers.ST_C:
                    return m_C;
                case e6510Registers.ST_Z:
                    return m_Z;
                case e6510Registers.r00:
                    return m_r00;
                case e6510Registers.r01:
                    return m_r01;
                case e6510Registers.Line:
                    return m_Line;
                case e6510Registers.Cycle:
                    return m_Cycle;
                case e6510Registers.StopWatch:
                    return m_Stopwatch;
            }
            return -1;
        }

        public bool SetFromString(string emulator)
        {
            //  ADDR AC XR YR SP 00 01 NV-BDIZC LIN CYC  STOPWATCH
            //.;0427 ad 00 00 f4 2f 37 10100100 000 000   87547824
            int index = emulator.IndexOf(".;"); //seems when you have break point this can get messed up
            if (index >= 0)
            {
                string parse = emulator.Substring(index);
                string[] parse2 = parse.Split(new char[2] { ' ','\r' },StringSplitOptions.RemoveEmptyEntries);
                m_PC = int.Parse(parse2[0].Substring(2), NumberStyles.HexNumber);
                m_A = int.Parse(parse2[1], NumberStyles.HexNumber);
                m_X = int.Parse(parse2[2], NumberStyles.HexNumber);
                m_Y = int.Parse(parse2[3], NumberStyles.HexNumber);
                m_SP = int.Parse(parse2[4], NumberStyles.HexNumber);
                m_r00 = int.Parse(parse2[5], NumberStyles.HexNumber);
                m_r01 = int.Parse(parse2[6], NumberStyles.HexNumber);
                m_N = parse2[7][0] - '0';
                m_V = parse2[7][1] - '0';
                m_B = parse2[7][3] - '0';
                m_D = parse2[7][4] - '0';
                m_I = parse2[7][5] - '0';
                m_Z = parse2[7][6] - '0';
                m_C = parse2[7][7] - '0';
                m_Line = int.Parse(parse2[8], NumberStyles.HexNumber);
                m_Cycle = int.Parse(parse2[9], NumberStyles.HexNumber);
                m_Stopwatch = int.Parse(parse2[10], NumberStyles.Integer);
                return true;
            }
            return false;
        }

        public void SetRegister(Enum registerID, int value)
        {
            if ((registerID is e6510Registers) == false)
            {
                throw new Exception("This requires a 6510 register enum set");
            }
            e6510Registers id = (e6510Registers)registerID;
            switch (id)
            {
                case e6510Registers.A:
                    m_A = value;
                    break;
                case e6510Registers.X:
                    m_X = value;
                    break;
                case e6510Registers.Y:
                    m_Y = value;
                    break;
                case e6510Registers.SP:
                    m_SP = value;
                    break;
                case e6510Registers.ST_N:
                    m_N = value;
                    break;
                case e6510Registers.ST_V:
                    m_V = value;
                    break;
                case e6510Registers.ST_B:
                    m_B = value;
                    break;
                case e6510Registers.ST_D:
                    m_D = value;
                    break;
                case e6510Registers.ST_I:
                    m_I = value;
                    break;
                case e6510Registers.ST_C:
                    m_C = value;
                    break;
                case e6510Registers.ST_Z:
                    m_Z = value;
                    break;
                case e6510Registers.r00:
                    m_r00 = value;
                    break;
                case e6510Registers.r01:
                    m_r01 = value;
                    break;
                case e6510Registers.Line:
                    m_Line = value;
                    break;
                case e6510Registers.Cycle:
                    m_Cycle = value;
                    break;
                case e6510Registers.StopWatch:
                    m_Stopwatch = value;
                    break;
            }
        }
    }
}
