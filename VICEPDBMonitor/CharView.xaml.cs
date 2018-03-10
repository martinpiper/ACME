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
    /// Interaction logic for CharView.xaml
    /// </summary>
    public partial class CharView : Window
    {
        MainWindow m_mainWindow;
        int m_startAddress;

        int m_backgroundColour;
        int m_mulCol0;
        int m_mulCol1;

        bool[,] m_hiresOrMulti;

        WriteableBitmap m_wb;

        public CharView(MainWindow mainWindow)
        {
            InitializeComponent();
            DataContext = new C128ViewModel();
            m_mainWindow = mainWindow;
            address.Items.Clear();
            for (int i = 0; i < 0xFFFF; i += 0x800)
            {
                address.Items.Add(string.Format("{0:X4}", i));
            }
            m_hiresOrMulti = new bool[16, 16];
            setAllHires();
            m_startAddress = 0;
        }

        private void backgroundColour_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            m_backgroundColour = backgroundColour.SelectedIndex;
            m_mulCol0 = mcol0.SelectedIndex;
            m_mulCol1 = mcol1.SelectedIndex;
            renderChars(m_startAddress);
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

                int dataPtr = m_startAddress;
                dataPtr += CharY * (16 * 8);
                dataPtr += CharX * 8;
                int sprCol = spriteCol.SelectedIndex;

                VICIIRenderer.renderChar(dataPtr, CharX, CharY, m_hiresOrMulti[CharX, CharY], sprCol, m_backgroundColour, m_mulCol0, m_mulCol1, m_wb);
            }
        }

        private void allHiresButton_Click(object sender, RoutedEventArgs e)
        {
            setAllHires();
            renderChars(m_startAddress);
        }

        private void allMultiButton_Click(object sender, RoutedEventArgs e)
        {
            setAllMulti();
            renderChars(m_startAddress);
        }

        public void setAllHires()
        {
            for (int x = 0; x < 16; ++x)
            {
                for (int y = 0; y < 16; ++y)
                {
                    m_hiresOrMulti[x, y] = false;
                }
            }
        }

        public void setAllMulti()
        {
            for (int x = 0; x < 16; ++x)
            {
                for (int y = 0; y < 16; ++y)
                {
                    m_hiresOrMulti[x, y] = true;
                }
            }
        }

        private void handleDisplayChars()
        {
            getStartAddress();
            VICECOMManager vcom = VICECOMManager.getVICEComManager();
            vcom.addTextCommand("bank ram", CommandStruct.eMode.DoCommandThrowAwayResults, null, null, null);
            vcom.addBinaryMemCommand(m_startAddress, m_startAddress + 0x0800, new CommandStruct.CS_BinaryDelegate(got_ram), null, this.Dispatcher);
            vcom.addTextCommand("bank cpu", CommandStruct.eMode.DoCommandThrowAwayResults, null, null, null);
        }

        private void getStartAddress()
        {
            int index = address.SelectedIndex;
            if (index < 0) index = 0;
            m_startAddress = index * 2048;
        }

        private void got_ram(byte[] data, object none)
        {
            C64RAM ram = C64RAM.getInstace();
            ram.injectBinaryData(m_startAddress, data);
            renderChars(m_startAddress);
        }
        /*m_mainWindow.SendCommand("bank ram\n");
        m_mainWindow.GetReply();

        int index = address.SelectedIndex;
        if (index < 0) index = 0;
        m_startAddress = index * 2048;

        byte[] data = m_mainWindow.sendBinaryMemCommandAndGetData(m_startAddress, m_startAddress + (2 * 1024) - 1);
        C64RAM ram = C64RAM.getInstace();
        ram.injectBinaryData(m_startAddress, data);
        renderChars(m_startAddress);
        m_mainWindow.SendCommand("bank cpu\n");
        m_mainWindow.GetReply();*/

        public void renderChars(int dataPtr)
        {
            m_wb = new WriteableBitmap(8 * 16, 8 * 16, 96, 96, PixelFormats.Bgra32, BitmapPalettes.WebPalette);

            int charCol = spriteCol.SelectedIndex;

            for (int y = 0; y < 16; ++y)
            {
                for (int x = 0; x < 16; ++x)
                {
                    VICIIRenderer.renderChar(dataPtr, x, y, m_hiresOrMulti[x, y], charCol, m_backgroundColour, m_mulCol0, m_mulCol1, m_wb);
                    dataPtr += 8;
                } //x
            }// y

            canvas.Source = m_wb;
        }

        

        private void address_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            handleDisplayChars();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            handleDisplayChars();
        }

        private void canvas_ToolTipOpening(object sender, ToolTipEventArgs e)
        {            
            Point mouse = Mouse.GetPosition(canvas);
            double x = mouse.X / 8.0;
            double y = mouse.Y / 8.0;

            int charX = (int)Math.Floor(x);
            int charY = (int)Math.Floor(y);

            int charNum = charY * 16 + charX;
            getStartAddress();
            int charMem = m_startAddress + (charNum * 8);
            hoverTip.Content = string.Format("{0:X02}@{1:X04}", charNum, charMem);
        }
    }
}
