using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace VICEPDBMonitor
{
    public class ShowSrcDissStruct
    {
        public int startPrev;
        public int endNext;
        public string disassemblyBefore;
        public string disassemblyAfter;
        public CommandStruct.CS_TextDelegate displayDissCallback;
    }

    interface IPDBReaderAndDisplay
    {
        //Init functions
        void CreatePDBFromARGS(string[] commandLineArgs,MainWindow window);
        void SetRegisterSet(IRegisterSet registerSet);
        void SetCodeWindowControl(Control textControl);
        void SetLabelsWindowControl(Control textControl);
        void SetLabelView(string text);
        void SetMemDump(IMemDump memDump);

        //Global update text functions
        void SetSouceView(string text);
        void AppendTextSouceView(string text, Brush brush);

        //Top Level Update Windows functions
        string UpdateLabels(bool usedLabels, bool execUsed, bool accessUsed, Dictionary<int, int> executedCount, Dictionary<int, int> accessedCount);
        string ShowSrcDissGetPostDissaem(string reply, object userData);
        string ShowSrcPostRegisters(); //this needs access to the registers
        string PostEnterKeyForCommand(string command); //this is called after the command has been "entered"

        //Child Update Functions
        ShowSrcDissStruct show_diss_common();
    }
}
