using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
    /// Interaction logic for ScriptPanel.xaml
    /// </summary>
    public partial class ScriptPanel : Window
    {
        private string[] currentCommandList;
        private int currentIndex;

        public ScriptPanel()
        {
            InitializeComponent();
        }

        private void mRunButton_Click(object sender, RoutedEventArgs e)
        {
            mResults.Text = "";
            string[] split = { "\n" };
            currentCommandList = mScriptText.Text.Split(split, StringSplitOptions.RemoveEmptyEntries);
            currentIndex = 0;
            DispatchNextCommand();
        }
        private void DispatchNextCommand(string command)
        {
            if (command.StartsWith("vice:"))
            {
                string viceCommand = null;
                string destString = null;
                if (command.Contains(">>"))
                {
                    int pipeIndex = command.IndexOf(">>");
                    viceCommand = command.Substring(5, pipeIndex - 5).Trim();
                    destString = command.Substring(pipeIndex+2).Trim();
                }
                else
                {
                    viceCommand = command.Substring(5);
                }
                mResults.Text += "executing vice command " + viceCommand +"\n";
                VICECOMManager vcom = VICECOMManager.getVICEComManager();
                vcom.addTextCommand(viceCommand, CommandStruct.eMode.DoCommandReturnResults, sp_gotData, destString, this.Dispatcher);
            }
            else if (command.StartsWith("host:"))
            {
                string hostCommand = command.Substring(5).Trim();
                int fileNameEnd = hostCommand.IndexOf(' ');

                string filename = hostCommand.Substring(0, fileNameEnd);
                string param = hostCommand.Substring(fileNameEnd);

                mResults.Text += "executing host command " + filename + "with params " + param + "\n";

                var proc = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = filename,
                        Arguments = param,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                proc.Start();
                while (!proc.StandardOutput.EndOfStream)
                {
                    mResults.Text += proc.StandardOutput.ReadLine() + "\n";

                }
                while (!proc.StandardError.EndOfStream)
                {
                    mResults.Text += proc.StandardOutput.ReadLine() + "\n";
                }
                DispatchNextCommand();
            }
        }
        private void DispatchNextCommand()
        {
            if (currentCommandList.Length > 0)
            {
                if (currentIndex < currentCommandList.Length)
                {
                    string command = currentCommandList[currentIndex];
                    currentIndex++;
                    DispatchNextCommand(command);
                }
            }
        }
        private void sp_gotData(string reply, object userData)
        {
            if (userData == null)
            {
                mResults.Text += reply;
            }
            else
            {
                var sw = new StreamWriter(userData as string);
                reply = reply.Replace("\r", "\r\n");
                sw.Write(reply);
                sw.Close();
            }
            DispatchNextCommand();
        }

        private void mCopyButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(mResults.Text);
        }
    }
}
