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
    /// Interaction logic for VDCBitmap.xaml
    /// </summary>
    public partial class VDCBitmap : Window
    {
        WriteableBitmap m_wb;
        MainWindow m_mainWindow;
        int m_startAddress;

        public VDCBitmap(MainWindow mainWindow)
        {
            InitializeComponent();
            m_mainWindow = mainWindow;
        }

        private void handleDisplayBitmap()
        {

            m_startAddress = Convert.ToInt32(startBox.Text, 16);
            // m_startBank = bank;

            VICECOMManager vcom = VICECOMManager.getVICEComManager();
            if (ram0Rad.IsChecked ?? true)
            {
                vcom.addTextCommand("bank ram", CommandStruct.eMode.DoCommandThrowAwayResults, null, null, null);
            }
            else if (ram1Rad.IsChecked ?? true)
            {
                vcom.addTextCommand("bank ram1", CommandStruct.eMode.DoCommandThrowAwayResults, null, null, null);
            }
            else
            {
                vcom.addTextCommand("bank vdc", CommandStruct.eMode.DoCommandThrowAwayResults, null, null, null);
            }
            vcom.addBinaryMemCommand(m_startAddress, m_startAddress + 0x2000, new CommandStruct.CS_BinaryDelegate(got_ram), null, this.Dispatcher);
            vcom.addTextCommand("bank cpu", CommandStruct.eMode.DoCommandThrowAwayResults, null, null, null);
        }

        private void got_ram(byte[] data, object none)
        {
            C64RAM ram = C64RAM.getInstace();
            ram.injectBinaryData(m_startAddress, data);
            renderBitmap();
        }

        public void renderBitmap()
        {
            //byte[] imageArray = new byte[(16 * 24) * (16 * 21) * 4]; //16x16 with 4 bits per pixel

            m_wb = new WriteableBitmap(640, 200, 96, 96, PixelFormats.Bgra32, BitmapPalettes.WebPalette);

            int width = Convert.ToInt32(widthBox.Text);
            int stride = Convert.ToInt32(strideBox.Text);
            int height = Convert.ToInt32(heightBox.Text);

            int bytesPerLines = width + stride;

            Int32Rect rect = new Int32Rect();
            if (doubleCheck.IsChecked ?? true)
            {
                rect.Width = 2;
            }
            else
            {
                rect.Width = 1;
            }
            rect.Height = 1;

            C64RAM ramObjc = C64RAM.getInstace();
            byte[] ram = ramObjc.getRAM();

            for (int y = 0; y < height*8; ++y)
            {
                int startAddress = m_startAddress + (y * bytesPerLines);
                rect.Y = y;
                for (int x = 0; x < width ; ++x)
                {
                    byte r = ram[startAddress + x];
                    for (int p = 0; p < 8; p+=1)
                    {
                        rect.X = x * 8 * rect.Width + p* rect.Width;

                        if ((r & 128) == 128)
                        {
                            m_wb.WritePixels(rect, VICPallete.getSafePalBGR32(1), 8/*4*rect.Width*/, 0);
                        }
                        else
                        {
                            m_wb.WritePixels(rect, VICPallete.getSafePalBGR32(0), 8/*4*rect.Width*/, 0);
                        }
                        r = (byte)(r << 1); //get next pixel
                    } // p
                }
            }

            canvas.Source = m_wb;
        }

        private void updateButton_Click(object sender, RoutedEventArgs e)
        {
            handleDisplayBitmap();
        }
    }
}
