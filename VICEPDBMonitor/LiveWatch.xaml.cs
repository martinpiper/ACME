using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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
    public enum EWatchObjectMode
    {
         normal
        ,zeroNotZero
        ,plusMinus
    }

    public class WatchObject
    {
        static Brush grey = new SolidColorBrush(Color.FromArgb(255, 0x30, 0x30, 0x30));
        static Brush set = new SolidColorBrush(Colors.DarkRed);
        static Brush notSet = new SolidColorBrush(Colors.DarkCyan);

        public int m_address { get; set; }
        public string addressString
        {
            get
            {
                return String.Format("${0:X4}", m_address);
            }
            set
            {
                string v = value.Trim();
                if( v.StartsWith("$") )
                {
                    v = v.Substring(1); //strip $
                }
                int temp = m_address;
                if( Int32.TryParse(v, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out temp))
                {
                    m_address = temp;
                }                
            }
        }

        public int m_value { get; set; }
        public string valueString
        {
            get
            {
                return String.Format("${0:X2}", m_value);
            }
        }
        public bool enabled { get; set; }
        public EWatchObjectMode mode { get; set; }
        public int internalID { get; set; }
        public int breakpointNumber { get; set; }

        public Brush tileColour
        {
            get
            {
                switch(mode)
                {
                    case EWatchObjectMode.normal:
                    default:
                        return grey;
                    case EWatchObjectMode.plusMinus:
                        if (m_value > 127) return set;
                        return notSet;
                    case EWatchObjectMode.zeroNotZero:
                        if (m_value != 0) return set;
                        return notSet;
                }
            }
        }
        public WatchObject clone()
        {
            WatchObject n = new WatchObject()
            {
                 m_address = m_address
                ,m_value = m_value
                ,enabled = enabled
                ,mode = mode
                ,internalID = internalID
                ,breakpointNumber = breakpointNumber
            };
            return n;
        }
    }
    /// <summary>
    /// Interaction logic for LiveWatch.xaml
    /// </summary>
    public partial class LiveWatch : Window, IBreakpointReturn
    {
        private static int g_ID = 0;

        public List<WatchObject> m_watches;
        public Dictionary<int, WatchObject> m_breakpointToWatchMap;

        public List<WatchObject>  Watches { get { return m_watches; } }

        public WatchObject ActiveWatch { get; set; }

        public LiveWatch()
        {
            m_watches = new List<WatchObject>();
            m_breakpointToWatchMap = new Dictionary<int, WatchObject>();

            //m_watches.Add(new WatchObject() { m_address = 0xD020, m_value = 0, enabled = true, internalID = 0, mode = EWatchObjectMode.normal });
            //m_watches.Add(new WatchObject() { m_address = 0xDC00, m_value = 0x7f, enabled = true, internalID = 1, mode = EWatchObjectMode.plusMinus });

            WatchObject Dummy = new WatchObject() { m_address = 0x0, m_value = 0, enabled = true, mode = EWatchObjectMode.normal };
            ActiveWatch = Dummy;
            DataContext = this;
          
            InitializeComponent();

            editStackPanel.DataContext = Dummy;
        }

        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            BreakPointDispatcher bpd = BreakPointDispatcher.getBreakPointDispatcher();
            bpd.addWatchPointAndNotifyMe(ActiveWatch.m_address, ActiveWatch.m_address, new BreakPointDispatcher.BreakPointEventDelegate(hitBreakpoint), this.Dispatcher, false, true, this);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            int breakpoint = ActiveWatch.breakpointNumber;
            m_breakpointToWatchMap.Remove(breakpoint);
            m_watches.Remove(ActiveWatch);
            BreakPointDispatcher bpd = BreakPointDispatcher.getBreakPointDispatcher();
            bpd.removeBreakpoint(breakpoint);
            DataContext = null;
            DataContext = this;
        }

        public void yourBreakpointNumberIs(int number)
        {
            WatchObject n = ActiveWatch.clone();

            n.internalID = g_ID;
            g_ID++;
            n.breakpointNumber = number;

            m_watches.Add(n);
            m_breakpointToWatchMap.Add(number, n);
            DataContext = null;
            DataContext = this;
        }

        private void hitBreakpoint(String eventType, int number, int address)
        {
            try
            {
                WatchObject obj = m_breakpointToWatchMap[number];
                VICECOMManager vcom = VICECOMManager.getVICEComManager();
                vcom.addBinaryMemCommand(obj.m_address, obj.m_address + 0x0100, new CommandStruct.CS_BinaryDelegate(got_new_value), obj, this.Dispatcher);
            }
            catch(Exception )
            {
                VICECOMManager vcom = VICECOMManager.getVICEComManager();
                vcom.addTextCommand("x", CommandStruct.eMode.DoCommandOnly, null, null, null);
            }
        }

        private void got_new_value(byte[] data, object obj)
        {
            WatchObject wo = obj as WatchObject;
            wo.m_value = data[0];
            DataContext = null;
            DataContext = this;
            VICECOMManager vcom = VICECOMManager.getVICEComManager();
            vcom.addTextCommand("x", CommandStruct.eMode.DoCommandOnly, null, null, null);
        }

        private void Click_item(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            WatchObject data = b.DataContext as WatchObject;
            editStackPanel.DataContext = data;
            ActiveWatch = data;
        }
    }
}
