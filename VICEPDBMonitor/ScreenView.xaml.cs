using System;
using System.Collections.Generic;
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
    /// <summary>
    /// Interaction logic for ScreenView.xaml
    /// </summary>
    public partial class ScreenView : Window, IBreakpointReturn
    {
        MainWindow m_mainWindow;
        int m_screenAddress;
        int m_charAddress;
        int m_breakpointNumber;

        int m_backgroundColour;
        int m_mulCol0;
        int m_mulCol1;
        int m_charColour;

        bool[,] m_hiresOrMulti;

        WriteableBitmap m_wb;

        const int kNumRAMCharsets = 0x10000 / 0x800;

        public ScreenView(MainWindow mainWindow)
        {
            InitializeComponent();
            DataContext = new C128ViewModel();
            m_mainWindow = mainWindow;
            charMem.Items.Clear();
            for (int i = 0; i < 0xFFFF; i += 0x800)
            {
                charMem.Items.Add(string.Format("{0:X4}", i));
            }
            charMem.Items.Add("hex 1");
            charMem.Items.Add("hex 2");
            charMem.Items.Add("hex 3");
            charMem.Items.Add("CharROM lo");
            charMem.Items.Add("CharROM hi");
            for (int i = 0; i < 0xFFFF; i += 0x400)
            {
                screenMem.Items.Add(string.Format("{0:X4}", i));
            }
            m_hiresOrMulti = new bool[40, 25];
            // setAllHires();
            m_screenAddress = 0;
            m_charAddress = 0;
            m_breakpointNumber = -1;
        }

        private void backgroundColour_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            m_backgroundColour = backgroundColour.SelectedIndex;
            m_mulCol0 = mcol0.SelectedIndex;
            m_mulCol1 = mcol1.SelectedIndex;
            m_charColour = charCol.SelectedIndex;
            renderScreen();
        }

        private void canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && e.ClickCount == 2)
            {
                Point clickPoint = e.GetPosition(canvas);
                double x = clickPoint.X / 8.0;
                double y = clickPoint.Y / 8.0;

                int CharX = (int)Math.Floor(x);
                int CharY = (int)Math.Floor(y);

                m_hiresOrMulti[CharX, CharY] = !m_hiresOrMulti[CharX, CharY];

                int CharPtr = m_screenAddress + (CharY*40) + CharX;
                C64RAM ram = C64RAM.getInstace();
                byte[] RAM = ram.getRAM();
                int dataPtr = m_charAddress + (8 * RAM[CharPtr]);

                VICIIRenderer.renderChar(dataPtr, CharX, CharY, m_hiresOrMulti[CharX, CharY], m_charColour, m_backgroundColour, m_mulCol0, m_mulCol1, m_wb);
            }
        }

        private void screenMem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            m_screenAddress = screenMem.SelectedIndex * 0x400;
            drawScreen();
        }

        private void charMem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (charMem.SelectedIndex < kNumRAMCharsets)
            {
                m_charAddress = charMem.SelectedIndex * 0x800;
            }
            else
            {
                switch(charMem.SelectedIndex)
                {
                    default:
                    case kNumRAMCharsets:
                        m_charAddress = (int)VICIIRenderer.eExtraCharsets.hexCharset1;
                        break;
                    case kNumRAMCharsets+1:
                        m_charAddress = (int)VICIIRenderer.eExtraCharsets.hexCharset2;
                        break;
                    case kNumRAMCharsets+2:
                        m_charAddress = (int)VICIIRenderer.eExtraCharsets.hexCharset3;
                        break;
                    case kNumRAMCharsets + 3:
                        m_charAddress = (int)VICIIRenderer.eExtraCharsets.charrom_lo;
                        break;
                    case kNumRAMCharsets + 4:
                        m_charAddress = (int)VICIIRenderer.eExtraCharsets.charrom_hi;
                        break;
                }
            }
            drawScreen();
        }

        private void drawScreen()
        {
            //first we need to get the RAM upto date
            VICECOMManager vcom = VICECOMManager.getVICEComManager();
            vcom.addTextCommand("bank ram", CommandStruct.eMode.DoCommandThrowAwayResults, null, null, null);
            vcom.addBinaryMemCommand(m_screenAddress, m_screenAddress + 0x400, new CommandStruct.CS_BinaryDelegate(got_screen), null, this.Dispatcher);
        }

        private void got_screen(byte[] data, object none)
        {
            C64RAM ram = C64RAM.getInstace();
            ram.injectBinaryData(m_screenAddress, data);
            VICECOMManager vcom = VICECOMManager.getVICEComManager();
            vcom.addBinaryMemCommand(m_screenAddress, m_charAddress + 0x800, new CommandStruct.CS_BinaryDelegate(got_char), null, this.Dispatcher);
        }

        private void got_char(byte[] data, object none)
        {
            C64RAM ram = C64RAM.getInstace();
            ram.injectBinaryData(m_screenAddress, data);
            VICECOMManager vcom = VICECOMManager.getVICEComManager();
            vcom.addTextCommand("bank cpu", CommandStruct.eMode.DoCommandThrowAwayResults, null, null, null);
            renderScreen();
        }
       
        private void renderScreen()
        {
            C64RAM ram = C64RAM.getInstace();
            byte[] RAM = ram.getRAM();
            m_wb = new WriteableBitmap(320, 200, 96, 96, PixelFormats.Bgra32, BitmapPalettes.WebPalette);
            for(int y = 0; y<25; ++y)
            {
                int rowPtr = m_screenAddress + (y * 40);
                for(int x = 0; x < 40; ++x)
                {
                    byte c = RAM[rowPtr + x];
                    int charAddress = m_charAddress + ((int)c * 8);
                    VICIIRenderer.renderChar(charAddress, x, y, m_hiresOrMulti[x, y], m_charColour, m_backgroundColour, m_mulCol0, m_mulCol1, m_wb);
                } //x
            } //y
            canvas.Source = m_wb;
        }

        private void refreshButton_Click(object sender, RoutedEventArgs e)
        {
            drawScreen();
        }

        private void allHiresButton_Click(object sender, RoutedEventArgs e)
        {
            for (int y = 0; y < 25; ++y)
            {
                for (int x = 0; x < 40; ++x)
                {
                    m_hiresOrMulti[x, y] = false;
                }
            }
            renderScreen();
        }

        private void allMultiButton_Click(object sender, RoutedEventArgs e)
        {
            for (int y = 0; y < 25; ++y)
            {
                for (int x = 0; x < 40; ++x)
                {
                    m_hiresOrMulti[x, y] = true;
                }
            }
            renderScreen();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            BreakPointDispatcher bpd = BreakPointDispatcher.getBreakPointDispatcher();
            if (m_breakpointNumber >= 0)
            {
                Button butt = sender as Button;
                butt.Content = "Trace Reads";
                bpd.removeBreakpoint(m_breakpointNumber);
                m_breakpointNumber = -1;
            }
            else
            {
                Button butt = sender as Button;
                butt.Content = "Stop Trace";
                int end = m_screenAddress + 1000;
                bpd.addWatchPointAndNotifyMe(m_screenAddress, end, new BreakPointDispatcher.BreakPointEventDelegate(hitBreakpoint), this.Dispatcher, true, false, this);
            }
        }

        private void hitBreakpoint(String eventType, int number, int address)
        {
            int relAddr = address - m_screenAddress;
            int y = relAddr / 40;
            int x = relAddr % 40;
            int[] pixels = new int[8 * 8];
            Int32Rect rect = new Int32Rect(x * 8, y * 8, 8, 8);
            m_wb.CopyPixels(rect, pixels, 8*4, 0);
            for( int dy = 0; dy < 8; dy++)
            {
                for(int dx = 0; dx < 8; dx++)
                {
                    int value = pixels[dy * 8 + dx];
                    value = (int)(value ^ 0xFFFFFFFF);
                    pixels[dy * 8 + dx] = value;
                }
            }
            m_wb.WritePixels(rect, pixels, 8*4, 0);
            canvas.Source = null;
            canvas.Source = m_wb;

            if(autorunCB.IsChecked ?? true)
            {
                VICECOMManager vcom = VICECOMManager.getVICEComManager();
                vcom.addTextCommand("x", CommandStruct.eMode.DoCommandOnly, null, null, null);
            }
        }

        public void yourBreakpointNumberIs(int number)
        {
            m_breakpointNumber = number;
        }

        private void canvas_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            Point mouse = Mouse.GetPosition(canvas);
            double x = mouse.X / 8.0;
            double y = mouse.Y / 8.0;

            int charX = (int)Math.Floor(x);
            int charY = (int)Math.Floor(y);

            C64RAM ram = C64RAM.getInstace();
            byte[] RAM = ram.getRAM();
            int rowPtr = m_screenAddress + (charY * 40);
            byte c = RAM[rowPtr + charX];
            int charAddress = m_charAddress + ((int)c * 8);
            
            hoverTip.Content = string.Format("{0},{1}@{2:X04}({3:X02})", charX,charY,rowPtr+charX, c);
        }
    }
}
