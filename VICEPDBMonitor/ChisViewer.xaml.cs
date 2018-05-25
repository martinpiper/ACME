using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace VICEPDBMonitor
{
    /// <summary>
    /// Interaction logic for ChisViewer.xaml
    /// </summary>
    public partial class ChisViewer : Window
    {

        private List<Match> m_currentMatchData = null;
        private int m_maxSP = 0;

        public ChisViewer()
        {
            InitializeComponent();
        }

        private void getButton_Click(object sender, RoutedEventArgs e)
        {
            VICECOMManager vcom = VICECOMManager.getVICEComManager();
            int count;
            if(!Int32.TryParse(mCount.Text,out count))
            {
                count = 500;
            }
            vcom.addTextCommand("chis " + count.ToString(), CommandStruct.eMode.DoCommandReturnResults, chis_gotData, null, this.Dispatcher);
        }

        private void chis_gotData(string reply, object userData)
        {
            string[] lines = reply.Split('\r');            
            m_currentMatchData = new List<Match>(lines.Length);
            foreach (string line in lines)
            {
                if (line.Length <= 16) continue; //if it is less than 16 chars its not a chis line 
                Match m = RegexMan.CPUHistoryLine.Match(line);
                m_currentMatchData.Add(m);
                string StackString = m.Groups[(int)RegexMan.eCPUHistoryLine.sp_reg].ToString();
                int stack = Int32.Parse(StackString, System.Globalization.NumberStyles.HexNumber);
                if (stack > m_maxSP)
                {
                    m_maxSP = stack;
                }
            }
            fillListBox();
        }

        private void fillListBox()
        {
            int colour = 0;

            string oldA = string.Empty;
            string oldX = string.Empty;
            string oldY = string.Empty;

            Brush[] backs = { new SolidColorBrush(Color.FromRgb(255, 255, 255)), new SolidColorBrush(Color.FromRgb(225, 225, 225)),new SolidColorBrush(Color.FromRgb(255,0,0)),new SolidColorBrush(Color.FromRgb(255,255,0)) };

            Brush[] paramsSet = { new SolidColorBrush(Color.FromRgb(0, 0, 0)), new SolidColorBrush(Color.FromRgb(0, 128, 0)) };

            Brush[] newSet = { new SolidColorBrush(Color.FromRgb(0, 0, 0)), new SolidColorBrush(Color.FromRgb(255, 0, 0)) };

            dataList.Items.Clear();

            int aligment = 24;

            Int32.TryParse(mStatusAligment.Text, out aligment);

            Stack<Tuple<ListRowItem, int>> stackStack = new Stack<Tuple<ListRowItem, int>>(64);
            ListRowItem root = new ListRowItem();
            root.address = "root";
            stackStack.Push(new Tuple<ListRowItem, int>(root, 255));
            dataList.BeginInit();
            foreach ( Match m in m_currentMatchData)
            {
                string StackString = m.Groups[(int)RegexMan.eCPUHistoryLine.sp_reg].ToString();
                int stack = Int32.Parse(StackString, System.Globalization.NumberStyles.HexNumber);
                int extra = m_maxSP - stack;
                string address = m.Groups[(int)RegexMan.eCPUHistoryLine.address].ToString();
                string op_hex = m.Groups[(int)RegexMan.eCPUHistoryLine.opcode_hex].ToString();
                while (op_hex.Length < 9) op_hex += " "; //pad it out
                string opcode = m.Groups[(int)RegexMan.eCPUHistoryLine.opcode_string].ToString();
                for (int i = 0; i < extra; ++i)
                {
                    opcode = " " + opcode;
                }
                string opcodeParams = m.Groups[(int)RegexMan.eCPUHistoryLine.opcode_params].ToString();
                while ((opcode.Length + opcodeParams.Length) < aligment) opcodeParams += " "; //pad it out
                string a_reg = m.Groups[(int)RegexMan.eCPUHistoryLine.a_reg].ToString();
                string x_reg = m.Groups[(int)RegexMan.eCPUHistoryLine.x_reg].ToString();
                string y_reg = m.Groups[(int)RegexMan.eCPUHistoryLine.y_reg].ToString();
                string stN = m.Groups[(int)RegexMan.eCPUHistoryLine.status_N].ToString();
                string stV = m.Groups[(int)RegexMan.eCPUHistoryLine.status_V].ToString();
                string stB = m.Groups[(int)RegexMan.eCPUHistoryLine.status_B].ToString();
                string stD = m.Groups[(int)RegexMan.eCPUHistoryLine.status_D].ToString();
                string stI = m.Groups[(int)RegexMan.eCPUHistoryLine.status_I].ToString();
                string stZ = m.Groups[(int)RegexMan.eCPUHistoryLine.status_Z].ToString();
                string stC = m.Groups[(int)RegexMan.eCPUHistoryLine.status_C].ToString();


                if( stI == "I")
                {
                    if( mHideInterupt.IsChecked == false )
                    {
                        // Console.WriteLine("discarded " + m.ToString());
                        continue;
                    }
                }
                // Console.WriteLine("Kept " + stI +" : " + m.ToString());
                int aColourIndex = 0;
                int xColourIndex = 0;
                int yColourIndex = 0;
                if(oldA.Length > 0)
                {
                    if( oldA != a_reg)
                    {
                        aColourIndex = 1;
                    }
                }
                oldA = a_reg;

                if (oldX.Length > 0)
                {
                    if (oldX != x_reg)
                    {   
                        xColourIndex = 1;
                    }
                }
                oldX = x_reg;

                if (oldY.Length > 0)
                {
                    if (oldY != y_reg)
                    {                    
                        yColourIndex = 1;
                    }
                }
                oldY = y_reg;

                ListRowItem row = new ListRowItem();
                row.address = address + " " + op_hex + " ";
                row.opcode = opcode + " ";
                row.opcode_params = opcodeParams;
                int type = 0;
                if(opcodeParams.StartsWith("#"))
                {
                    type = 1;
                }
                row.params_colour = paramsSet[type];
                row.a = " A:" + a_reg;
                row.x = " X:" + x_reg;
                row.y = " Y:" + y_reg;
                row.sp = " SP:" + StackString;
                row.status = " " + stN + stV + "-" + stB + stD + stI + stZ + stC;
                if (opcode.Contains("BRK"))
                {
                    row.background = backs[2];
                }
                else if (address.StartsWith("00"))
                {
                    row.background = backs[3];
                }
                else
                {
                    row.background = backs[colour];
                }
                row.A_colour = newSet[aColourIndex];
                row.X_colour = newSet[xColourIndex];
                row.Y_colour = newSet[yColourIndex];
                colour ^= 1;
                // string line_string =  + " " + op_hex + " ";
                //line_string += opcode + " " + opcodeParams + " A:" + a_reg + " X:" + x_reg + " Y:" + y_reg + " SP:" + StackString + " " + stN + stV + "-" + stB + stD + stI + stZ + stC;
                stackStack.Peek().Item1.Items.Add(row);

                if(opcode.Contains("JSR"))
                {
                    stackStack.Push(new Tuple<ListRowItem, int>(row, stack));
                    aligment -= 2; //shift it down 2 to keep alignment
                }
                if(opcode.Contains("RTS"))
                {
                    if( stackStack.Count > 1)
                    {
                        stackStack.Pop();
                        aligment += 2; //shift it up 2 to keep alignment
                    }
                } 
                
            }
            dataList.Items.Add(root);
            dataList.EndInit();
        }

        private void mHideInterupt_Click(object sender, RoutedEventArgs e)
        {
            fillListBox();
            //dataList.SelectedIndex = 0;
        }

        private void mStatusAligment_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (m_currentMatchData != null)
            {
                fillListBox();
            }
        }

        private void mFlagBRK_Click(object sender, RoutedEventArgs e)
        {
            fillListBox();
        }

        private void mShowZP_Click(object sender, RoutedEventArgs e)
        {
            fillListBox();
        }
    }

    public class ListRowItem
    {
        public ListRowItem()
        {
            this.Items = new ObservableCollection<ListRowItem>();
        }
        public string address { get; set; }
        public string opcode { get; set; }
        public string opcode_params { get; set; }
        public string a { get; set; }
        public string x { get; set; }
        public string y { get; set; }
        public string sp { get; set; }
        public string status { get; set; }
        public Brush background { get; set; }
        public Brush params_colour { get; set; }
        public Brush A_colour { get; set; }
        public Brush X_colour { get; set; }
        public Brush Y_colour { get; set; }

        public ObservableCollection<ListRowItem> Items { get; set; }
    }
}
