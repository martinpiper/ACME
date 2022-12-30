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
    /// Interaction logic for ProfileView.xaml
    /// </summary>
    public partial class ProfileView : Window
    {
        public ProfileView()
        {
            InitializeComponent();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            mRefresh.IsEnabled = false;
            VICECOMManager vcom = VICECOMManager.getVICEComManager();
            vcom.addTextCommand("profile print", CommandStruct.eMode.DoCommandReturnResults, chis_gotData, null, this.Dispatcher);
        }

        private void chis_gotData(string reply, object userData)
        {
            mTextBox.Text = reply;

            mRefresh.IsEnabled = true;
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            VICECOMManager vcom = VICECOMManager.getVICEComManager();
            vcom.addTextCommand("profile start", CommandStruct.eMode.DoCommandThrowAwayResults, null, null, null);
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            VICECOMManager vcom = VICECOMManager.getVICEComManager();
            vcom.addTextCommand("profile stop", CommandStruct.eMode.DoCommandThrowAwayResults, null, null, null);
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            VICECOMManager vcom = VICECOMManager.getVICEComManager();
            vcom.addTextCommand("profile clear", CommandStruct.eMode.DoCommandThrowAwayResults, null, null, null);
        }
    }
}
