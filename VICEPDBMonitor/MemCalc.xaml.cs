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
    /// Interaction logic for MemCalc.xaml
    /// </summary>
    public partial class MemCalc : Window
    {
        public MemCalc()
        {
            InitializeComponent();
        }

        private void mCharBase_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                try
                {
                    int baseAddr = Int32.Parse(mCharBase.Text, System.Globalization.NumberStyles.HexNumber);
                    int charAddr = Int32.Parse(mCharAddress.Text, System.Globalization.NumberStyles.HexNumber);
                    charAddr -= baseAddr;
                    int charNum = charAddr / 8;
                    int line = charAddr % 8;
                    mCharNum.Text = charNum.ToString();
                    mCharLine.Text = line.ToString();
                }
                catch
                {
                }
            }
        }

        private void charChangeCharLine(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                try
                {
                    int baseAddr = Int32.Parse(mCharBase.Text, System.Globalization.NumberStyles.HexNumber);
                    int charNum = Int32.Parse(mCharNum.Text);
                    int charLine = Int32.Parse(mCharLine.Text);
                    int charAddr = baseAddr + (charNum * 8) + charLine;
                    mCharAddress.Text = String.Format("{0:X4}", charAddr);
                }
                catch
                {
                }
            }
        }

        private void ScreenAddr_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                try
                {
                    int baseAddr = Int32.Parse(mScreenrBase.Text, System.Globalization.NumberStyles.HexNumber);
                    int screenAddr = Int32.Parse(mScreenAddress.Text, System.Globalization.NumberStyles.HexNumber);
                    screenAddr -= baseAddr;
                    int line = screenAddr / 40;
                    int charNum = screenAddr % 40;
                    mScreenCharNum.Text = charNum.ToString();
                    mScreenLine.Text = line.ToString();
                }
                catch
                {
                }
            }
        }

        private void ScreenLine_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                try
                {
                    int baseAddr = Int32.Parse(mScreenrBase.Text, System.Globalization.NumberStyles.HexNumber);
                    int line = Int32.Parse(mScreenLine.Text);
                    int charNum = Int32.Parse(mScreenCharNum.Text);
                    int screenAddr = baseAddr + (line * 40) + charNum;
                    mScreenAddress.Text = String.Format("{0:X4}", screenAddr);
                }
                catch
                {
                }
            }
        }
    }

}
