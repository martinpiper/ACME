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
    public partial class ScreenView : Window
    {
        MainWindow m_mainWindow;
        int m_screenAddress;
        int m_charAddress;

        int m_backgroundColour;
        int m_mulCol0;
        int m_mulCol1;
        int m_charColour;

        bool[,] m_hiresOrMulti;

        WriteableBitmap m_wb;

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
            for (int i = 0; i < 0xFFFF; i += 0x400)
            {
                screenMem.Items.Add(string.Format("{0:X4}", i));
            }
            m_hiresOrMulti = new bool[40, 25];
            // setAllHires();
            m_screenAddress = 0;
            m_charAddress = 0;
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
            m_charAddress = charMem.SelectedIndex * 0x800;
            drawScreen();
        }

        private void drawScreen()
        {
            //first we need to get the RAM upto date
//             VICECOMManager vcom = VICECOMManager.getVICEComManager();
//             vcom.addTextCommand("bank ram", CommandStruct.eMode.DoCommandThrowAwayResults, null, null, null);
//             vcom.addBinaryMemCommand(m_screenAddress, m_screenAddress + 0x400, new CommandStruct.CS_BinaryDelegate(got_screen), null, this.Dispatcher);
            
        }

        private void got_screen(byte[] data, object none)
        {
            C64RAM ram = C64RAM.getInstace();
            ram.injectBinaryData(m_screenAddress, data);
//             VICECOMManager vcom = VICECOMManager.getVICEComManager();
//             vcom.addBinaryMemCommand(m_screenAddress, m_charAddress + 0x800, new CommandStruct.CS_BinaryDelegate(got_char), null, this.Dispatcher);

        }

        private void got_char(byte[] data, object none)
        {
            C64RAM ram = C64RAM.getInstace();
            ram.injectBinaryData(m_screenAddress, data);
//             VICECOMManager vcom = VICECOMManager.getVICEComManager();
//             vcom.addTextCommand("bank cpu", CommandStruct.eMode.DoCommandThrowAwayResults, null, null, null);
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
    }
}
