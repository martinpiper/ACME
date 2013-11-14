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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Sockets;

namespace VICEPDBMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0 , 0 , 0 , 0 , 100);
            dispatcherTimer.Start();

            // Remember to use: "C:\Downloads\WinVICE-2.4-x64\WinVICE-2.4-x64\x64.exe" -remotemonitor
            // Connect to port 6510

            sock = new Socket(AddressFamily.InterNetwork , SocketType.Stream , ProtocolType.Tcp);
            sock.Blocking = false;
            try
            {
                sock.Connect("localhost", 6510);
            }
            catch (System.Exception ex)
            {
            	
            }
        }

        Socket sock;
        bool sentMem = false;
        bool sentExit = false;

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            sock.Poll(0, SelectMode.SelectRead);
            if (!sock.Connected)
            {
                return;
            }
            if (!sentMem)
            {
                sentMem = true;
                byte[] msg = Encoding.ASCII.GetBytes("m 0 ffff\n");
                int ret = sock.Send(msg);
            }
//            textBox.Text += "Hello\n";

            byte[] bytes = new byte[500000];
            try
            {
                int got = sock.Receive(bytes);
                if (got > 0)
                {
                    textBox.Text += Encoding.ASCII.GetString(bytes, 0, got);
                }
            }
            catch (System.Exception ex)
            {
                int foundPos = textBox.Text.IndexOf("(C:$0000)");
                if (sentMem && !sentExit && (foundPos > 0) && (foundPos > (textBox.Text.Length - 20)))
                {
                    textBox.Text += "Sent EXIT\n";
                    sentExit = true;
                    byte[] msg = Encoding.ASCII.GetBytes("x\n");
                    int ret = sock.Send(msg);
                }
            }
        }
    }

}
