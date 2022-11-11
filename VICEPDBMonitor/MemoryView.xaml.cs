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
    /// Interaction logic for MemoryView.xaml
    /// </summary>
    public partial class MemoryView : Window
    {
        public MemoryView()
        {
            InitializeComponent();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            mRefresh.IsEnabled = false;
            VICECOMManager vcom = VICECOMManager.getVICEComManager();
            vcom.addBinaryMemCommand(0, 0xffff, new CommandStruct.CS_BinaryDelegate(gotMemory), null, this.Dispatcher);
        }

        private void gotMemory(byte[] data, object none)
        {
            StringBuilder result = new StringBuilder((7 + (16 * 3) + 1) * 4096);

            for (int i = 0; i < data.Length; i += 0x10)
            {
                result.Append(i.ToString("X4") + " : ");

                for (int c = 0; c < 0x10; c++)
                {
                    if (i + c < data.Length)
                    {
                        result.Append(" " + data[i+c].ToString("X2"));
                    }
                }
                result.Append("\r");
            }

            mTextBox.Text = result.ToString();

            mRefresh.IsEnabled = true;
        }
    }
}
