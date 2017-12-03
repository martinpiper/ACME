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
        int m_bytesRead;
        int m_rounds; //how many times have we requested sprite data

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

                renderChar(dataPtr, CharX, CharY, m_hiresOrMulti[CharX, CharY], sprCol, m_wb);
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
            m_mainWindow.SendCommand("bank ram\n");
            m_mainWindow.GetReply();

            int index = address.SelectedIndex;
            if (index < 0) index = 0;
            m_startAddress = index * 2048;

            byte[] data = m_mainWindow.sendBinaryMemCommandAndGetData(m_startAddress, m_startAddress + (2 * 1024) - 1);
            C64RAM ram = C64RAM.getInstace();
            ram.injectBinaryData(m_startAddress, data);
            renderChars(m_startAddress);
            m_mainWindow.SendCommand("bank cpu\n");
            m_mainWindow.GetReply();
        }

        public void renderChars(int dataPtr)
        {
            m_wb = new WriteableBitmap(8 * 16, 8 * 16, 96, 96, PixelFormats.Bgra32, BitmapPalettes.WebPalette);

            int charCol = spriteCol.SelectedIndex;

            for (int y = 0; y < 16; ++y)
            {
                for (int x = 0; x < 16; ++x)
                {
                    renderChar(dataPtr, x, y, m_hiresOrMulti[x, y], charCol, m_wb);
                    dataPtr += 8;
                } //x
            }// y

            canvas.Source = m_wb;
        }

        void renderChar(int addr, int charX, int charY, bool multicolour, int charColour, WriteableBitmap wb)
        {

            C64RAM ramObjc = C64RAM.getInstace();
            byte[] ram = ramObjc.getRAM();

            Int32Rect rect = new Int32Rect();
            if (multicolour)
            {
                rect.Width = 2;
            }
            else
            {
                rect.Width = 1;
            }
            rect.Height = 1;
            int spriteTLX = charX * 8;
            int spriteTLY = charY * 8;


            for (int sy = 0; sy < 8; ++sy)
            {
                rect.Y = spriteTLY + sy;
                for (int sx = 0; sx < 1; ++sx)
                {

                    byte r = ram[addr];
                    //int bitmapIndex = ((spriteTLY + sy) * (24 * 4)) + ((spriteTLX + (sx*8)) * 4);
                    if (multicolour)
                    {
                        for (int p = 0; p < 8; p += 2)
                        {
                            rect.X = spriteTLX + (sx * 8) + p;
                            switch (r & 192)
                            {
                                default:
                                case 0:  //%00
                                    wb.WritePixels(rect, VICPallete.palBGR32[m_backgroundColour], 8, 0);
                                    break;
                                case 64: //%01
                                    wb.WritePixels(rect, VICPallete.palBGR32[m_mulCol0], 8, 0);
                                    break;
                                case 128: //%10
                                    wb.WritePixels(rect, VICPallete.palBGR32[m_mulCol1], 8, 0);
                                    break;
                                case 192: //%11
                                    wb.WritePixels(rect, VICPallete.palBGR32[charColour], 8, 0);
                                    break;
                            }
                            r = (byte)(r << 2); //get next pixel
                        }
                    }
                    else
                    {
                        for (int p = 0; p < 8; ++p)
                        {

                            rect.X = spriteTLX + (sx * 8) + p;

                            if ((r & 128) == 128)
                            {
                                wb.WritePixels(rect, VICPallete.palBGR32[charColour], 4, 0);
                            }
                            else
                            {
                                wb.WritePixels(rect, VICPallete.palBGR32[m_backgroundColour], 4, 0);
                            }
                            r = (byte)(r << 1); //get next pixel
                        } // p
                    }
                    addr++;
                } //sx
            } //sy
        }

        private void address_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            handleDisplayChars();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            handleDisplayChars();
        }
    }
}
