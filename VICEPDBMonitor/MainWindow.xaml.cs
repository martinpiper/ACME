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
        bool sentReg = false;
        bool gotReg = false;
        bool sentMem = false;
        bool sentExit = false;
        string gotText = "";
        string gotTextWork = "";

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            sock.Poll(0, SelectMode.SelectRead);
            if (!sock.Connected)
            {
                return;
            }
            if (!sentReg)
            {
                gotText = "";
                gotTextWork = "";
                sentReg = true;
                // NOTE: Adding spaces for monitor bug workaround
                byte[] msg = Encoding.ASCII.GetBytes("r                                         \n");
                int ret = sock.Send(msg);
            }
            if (!sentMem && gotReg)
            {
                sentMem = true;
                // NOTE: Adding spaces for monitor bug workaround
                byte[] msg = Encoding.ASCII.GetBytes("m 0000 ffff                               \n");
                int ret = sock.Send(msg);
            }

            byte[] bytes = new byte[500000];
            try
            {
                int got = sock.Receive(bytes);
                if (got > 0)
                {
                    gotText += Encoding.ASCII.GetString(bytes, 0, got);
                }
            }
            catch (System.Exception ex)
            {
                if (gotText.Length > 10)
                {
                    // Look for the second line starting with this, which signifies the command was done.
                    int foundPos = gotText.IndexOf("(C:$", 8);
                    if ((foundPos > 0) && (foundPos > (gotText.Length - 20)))
                    {
                        if (sentReg && !gotReg)
                        {
                            gotTextWork = gotText.Substring(10 , foundPos-10);
                            gotReg = true;
                            gotText = gotText.Substring(foundPos);
                        }
                        if (sentMem)
                        {
                            gotTextWork += gotText.Substring(10 , foundPos-10);

                            textBox.Text = gotTextWork;
                            sentExit = true;
                            byte[] msg = Encoding.ASCII.GetBytes("x                                         \n");
                            int ret = sock.Send(msg);
                            gotText = "";
                            gotTextWork = "";
                            sentReg = false;
                            gotReg = false;
                            sentMem = false;
                            sentExit = false;
                        }
                    }
                }
            }
        }
    }

}
