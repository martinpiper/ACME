using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace VICEPDBMonitor
{
    interface IBreakpointReturn
    {
        void yourBreakpointNumberIs(int number);
    }

    class BreakPointDispatcher
    {
        static BreakPointDispatcher gBreakPointDispatcher;
        static public BreakPointDispatcher getBreakPointDispatcher()
        {
            if (gBreakPointDispatcher == null)
            {
                gBreakPointDispatcher = new BreakPointDispatcher();
            }
            return gBreakPointDispatcher;
        }

        public delegate void BreakPointEventDelegate(String eventType,int number, int address);

        private Dictionary<int, BreakPointEventDelegate> mBreakPointToDispatch;

        private class CallBackStruct
        {
            public BreakPointEventDelegate me;
            public IBreakpointReturn owner;
        }


        private BreakPointDispatcher()
        {
            mBreakPointToDispatch = new Dictionary<int, BreakPointEventDelegate>();
        }

        public void addBreakPointAndNotifyMe(int address, BreakPointEventDelegate me, Dispatcher dispatcher,IBreakpointReturn owner)
        {
            VICECOMManager vc = VICECOMManager.getVICEComManager();
            string command = string.Format("break {0:X4}", address);
            vc.addTextCommand(command, CommandStruct.eMode.DoCommandReturnResults, bpd_addBreak, new CallBackStruct { me = me, owner = owner}, dispatcher);
        }

        public void addBreakPointAndNotifyMe(int startAddress, int endAddress, BreakPointEventDelegate me, Dispatcher dispatcher, IBreakpointReturn owner)
        {
            VICECOMManager vc = VICECOMManager.getVICEComManager();            
            string command = string.Format("break {0:X4} {1:X4}", startAddress, endAddress);
            vc.addTextCommand(command, CommandStruct.eMode.DoCommandReturnResults, bpd_addBreak, new CallBackStruct { me = me, owner = owner }, dispatcher);
        }

        public void addWatchPointAndNotifyMe(int startAddress, int endAddress, BreakPointEventDelegate me, Dispatcher dispatcher,bool load, bool store, IBreakpointReturn owner)
        {
            VICECOMManager vc = VICECOMManager.getVICEComManager();
            string pre = String.Empty;
            if(load)
            {
                pre += "load ";
            }
            if(store)
            {
                pre += "store ";
            }
            string command = string.Format("watch {0:s} {1:X4} {2:X4}",pre, startAddress, endAddress);
            vc.addTextCommand(command, CommandStruct.eMode.DoCommandReturnResults, bpd_addBreak, new CallBackStruct { me = me, owner = owner }, dispatcher);
        }

        private void bpd_addBreak(string reply, object userData)
        {
            Match match = RegexMan.BreakPointResult.Match(reply);
            if( match.Success )
            {
                int number = Int32.Parse(match.Groups[(int)RegexMan.eBreakPointResult.number].Value);
                CallBackStruct cbs = userData as CallBackStruct;
                mBreakPointToDispatch[number] = cbs.me;
                cbs.owner.yourBreakpointNumberIs(number);
            }
        }        

        public bool checkBreakPointAndDisptach(Match match)
        {
            int number = Int32.Parse(match.Groups[(int)RegexMan.eBreakPointHit.number].Value);
            if( mBreakPointToDispatch.ContainsKey(number))
            {
                string eventType = match.Groups[(int)RegexMan.eBreakPointHit.mode].Value;
                int address = Int32.Parse(match.Groups[(int)RegexMan.eBreakPointHit.address].Value, System.Globalization.NumberStyles.HexNumber);
                mBreakPointToDispatch[number](eventType, number, address);
                return true;
            }
            return false;
        }

        public void removeBreakpoint(int number)
        {
            VICECOMManager vc = VICECOMManager.getVICEComManager();
            string command = "del " + number;
            vc.addTextCommand(command, CommandStruct.eMode.DoCommandThrowAwayResults, null, null, null);
            mBreakPointToDispatch.Remove(number);
        }
    }
}
