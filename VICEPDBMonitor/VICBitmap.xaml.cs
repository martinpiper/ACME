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
    /// Interaction logic for VICBitmap.xaml
    /// </summary>
    public partial class VICBitmap : Window
    {
        MainWindow m_mainWindow;
        int m_startAddress;

        int m_backgroundColour;
        int m_mulCol0;
        int m_mulCol1;

        WriteableBitmap m_wb;

        public VICBitmap(MainWindow mainWindow)
        {
            InitializeComponent();
            DataContext = new C128ViewModel();
            m_mainWindow = mainWindow;
            address.Items.Clear();
            for (int i = 0; i < 0xFFFF; i += 0x2000)
            {
                address.Items.Add(string.Format("{0:X4}", i));
            }
            m_startAddress = 0;
            address.SelectedIndex = 0;
            m_backgroundColour = 0;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            getStartAddress();
            handleDisplayChars();
        }

        private void handleDisplayChars()
        {
            getStartAddress();
            VICECOMManager vcom = VICECOMManager.getVICEComManager();
            vcom.addTextCommand("bank ram", CommandStruct.eMode.DoCommandThrowAwayResults, null, null, null);
            vcom.addBinaryMemCommand(m_startAddress, m_startAddress + 0x2000, new CommandStruct.CS_BinaryDelegate(got_ram), null, this.Dispatcher);
            vcom.addTextCommand("bank cpu", CommandStruct.eMode.DoCommandThrowAwayResults, null, null, null);
        }

        private void getStartAddress()
        {
            int index = address.SelectedIndex;
            if (index < 0) index = 0;
            m_startAddress = index * 0x2000;
        }

        private void got_ram(byte[] data, object none)
        {
            C64RAM ram = C64RAM.getInstace();
            ram.injectBinaryData(m_startAddress, data);
            renderChars(m_startAddress);
        }

        public void renderChars(int dataPtr)
        {
            m_wb = new WriteableBitmap(8 * 40, 8 * 25, 96, 96, PixelFormats.Bgra32, BitmapPalettes.WebPalette);

            int charCol = 1; // spriteCol.SelectedIndex;

            for (int y = 0; y < 25; ++y)
            {
                for (int x = 0; x < 40; ++x)
                {
                    VICIIRenderer.renderChar(dataPtr, x, y, false, charCol, m_backgroundColour, m_mulCol0, m_mulCol1, m_wb);
                    dataPtr += 8;
                } //x
            }// y

            canvas.Source = m_wb;
        }
    }
}
