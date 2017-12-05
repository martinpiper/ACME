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
            // renderScreen(m_startAddress);
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

                int dataPtr = m_screenAddress;
                dataPtr += CharY * (16 * 8);
                dataPtr += CharX * 8;
                int sprCol = spriteCol.SelectedIndex;

                VICIIRenderer.renderChar(dataPtr, CharX, CharY, m_hiresOrMulti[CharX, CharY], sprCol, m_backgroundColour, m_mulCol0, m_mulCol1, m_wb);
            }
        }
    }
}
