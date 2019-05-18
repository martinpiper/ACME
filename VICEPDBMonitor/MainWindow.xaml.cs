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
using System.Globalization;
using System.Threading;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace VICEPDBMonitor
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        IRegisterSet m_registerSet;
        IPDBReaderAndDisplay m_readerAndDispaly;
        IMemDump m_memDump;

        bool mDump = false;
        bool mUsedLabels = false;
        bool mAccessUsed = false;
        bool mExecUsed = false;

        //Regex mBreakPointResultRegex;
        //Regex mBreakPointHitRegex;
        List<BreakPointDataSource> mBreakPoints;
        ObservableCollection<AssertDataSource> mAssertList;


        private delegate void NoArgDelegate();
        private delegate void OneArgDelegate(String arg);
        private delegate void TwoArgDelegate(String arg, Brush brush);

        public MainWindow()
        {
            InitializeComponent();
            //install the error callback before we do anything in case something connects to VICE
            VICECOMManager vcom = VICECOMManager.getVICEComManager();
            vcom.setErrorCallback(new VICECOMManager.OneArgDelegate(SetSourceView), this.Dispatcher);
            vcom.setVICEmsgCallback(new VICECOMManager.OneArgDelegate(GotMsgFromVice));
            m_registerSet = new RegisterSet6510();
            m_memDump = new C64MemDump();
            m_memDump.SetRegisterSet(m_registerSet);

            //this must be BEFORE we parse the PDB Data
            mAssertList = new ObservableCollection<AssertDataSource>();
            string[] commandLineArgs = Environment.GetCommandLineArgs();
            if(commandLineArgs.Length == 1)
            {
                m_readerAndDispaly = new AcmePDBRandD();
                //System.Environment.Exit(1);
            }
            else if (commandLineArgs[1].EndsWith(".json"))
            {
                m_readerAndDispaly = new FunctionJSONRAndD();
            }
            else
            {
                m_readerAndDispaly = new AcmePDBRandD();
            }
            m_readerAndDispaly.SetCodeWindowControl(mTextBox);
            m_readerAndDispaly.SetLabelsWindowControl(mLabelsBox);
            m_readerAndDispaly.SetRegisterSet(m_registerSet);
            m_readerAndDispaly.SetMemDump(m_memDump);

            mBreakPoints = new List<BreakPointDataSource>();
            mBreakPointDisplay.ItemsSource = mBreakPoints;

            VICIIRenderer.initRenderer(); //load charsets


            m_readerAndDispaly.CreatePDBFromARGS(commandLineArgs, this);

            //			mCommands.Add("r");
            //			mCommands.Add("m 0000 ffff");
            //			mCommands.Add("x");
            //			mCommands.Add("!s");
            //			mCommands.Add("!sm");

            dispatchCommand("!breaklist");

            HandleCodeView();

            /*AssertDataSource AD = new AssertDataSource();
            AD.Enable = true;
            AD.Address = 0x810;
            AD.Label = "Test";
            AD.Condition = "@io:$d020 != $00";
            AD.Msg = "This is a test";
            AD.Number = 1;
            mAssertList.Add(AD);*/

            AssertDataGrid.ItemsSource = mAssertList;
        }

        public void canStep(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        public void stepExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Button_Click_StepOver(sender, null);
        }

        public void stepInExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Button_Click_StepIn(sender, null);
        }

        public void stepOutExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Button_Click_StepOut(sender, null);
        }

        public void runExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            Button_Click_Go(sender, null);
        }

        TextPointer GetTextPositionAtOffset(TextPointer position, int characterCount)
        {
            while (position != null)
            {
                if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    int count = position.GetTextRunLength(LogicalDirection.Forward);
                    if (characterCount <= count)
                    {
                        return position.GetPositionAtOffset(characterCount);
                    }

                    characterCount -= count;
                }

                TextPointer nextContextPosition = position.GetNextContextPosition(LogicalDirection.Forward);
                if (nextContextPosition == null)
                    return position;

                position = nextContextPosition;
            }

            return position;
        }

        public int getSafeC64Memory(int addr)
        {
            return (int)m_memDump.GetMemory(addr & 0xffff);
        }

        string getMicroDump(int addr)
        {
            if (mDump == false)
            {
                return "";
            }
            return m_memDump.GetMicroDump(addr);
        }

        string EnrichDumpWithMemory(string text)
        {
            if (mDump == false)
            {
                return text;
            }
            int pos = 0;
            int RegY = m_registerSet.GetRegister(e6510Registers.Y);
            int RegX = m_registerSet.GetRegister(e6510Registers.X);

            while (pos < text.Length)
            {
                try
                {
                    int pos2 = text.IndexOf("$", pos);
                    if (pos2 < 1)
                    {
                        break;
                    }

                    // Skip #$xx
                    if (text[pos2 - 1] == '#')
                    {
                        //text = text.Insert(pos + 4, "\t\t\t\t");
                        pos = pos2 + 3;
                        continue;
                    }


                    // Enrich ($xx),Y
                    if (text[pos2 + 3] == ')' && text[pos2 + 4] == ',')
                    {
                        string tAddr = text.Substring(pos2 + 1, 2);
                        int addr = int.Parse(tAddr, NumberStyles.HexNumber);
                        int indirect = m_memDump.GetMemory(addr) + (((int)m_memDump.GetMemory(addr + 1)) << 8);
                        text = text.Insert(pos2 + 5 + 3, getMicroDump(indirect + RegY));

                        pos = pos2 + 5 + 3 + m_memDump.GetMicroDumpStringLenght();
                        continue;
                    }

                    // Enrich ($xx,X)
                    if (text[pos2 + 3] == ',' && text[pos2 + 5] == ')')
                    {
                        string tAddr = text.Substring(pos2 + 1, 2);
                        int addr = int.Parse(tAddr, NumberStyles.HexNumber);
                        int indirect = m_memDump.GetMemory((addr + RegX) & 0xff) + (((int)m_memDump.GetMemory((addr + RegX + 1) & 0xff)) << 8);
                        text = text.Insert(pos2 + 5 + 3, getMicroDump(indirect));

                        pos = pos2 + 5 + 3 + m_memDump.GetMicroDumpStringLenght();
                        continue;
                    }

                    // Enrich $xxxx,x/y
                    if (text[pos2 + 5] == ',' && (text[pos2 + 5 + 1] == 'X' || text[pos2 + 5 + 1] == 'x' || text[pos2 + 5 + 1] == 'Y' || text[pos2 + 5 + 1] == 'y'))
                    {
                        string tAddr = text.Substring(pos2 + 1, 4);
                        int addr = int.Parse(tAddr, NumberStyles.HexNumber);
                        int offset = RegX;
                        if (text[pos2 + 5 + 1] == 'Y' || text[pos2 + 5 + 1] == 'y')
                        {
                            offset = RegY;
                        }
                        text = text.Insert(pos2 + 5 + 3, getMicroDump(addr + offset));

                        pos = pos2 + 5 + 3 + m_memDump.GetMicroDumpStringLenght();
                        continue;
                    }

                    // Enrich $xx,
                    if (text[pos2 + 3] == ',' && (text[pos2 + 3 + 1] == 'X' || text[pos2 + 3 + 1] == 'x' || text[pos2 + 3 + 1] == 'Y' || text[pos2 + 3 + 1] == 'y'))
                    {
                        string tAddr = text.Substring(pos2 + 1, 2);
                        int addr = int.Parse(tAddr, NumberStyles.HexNumber);
                        int offset = RegX;
                        if (text[pos2 + 3 + 1] == 'Y' || text[pos2 + 3 + 1] == 'y')
                        {
                            offset = RegY;
                        }
                        text = text.Insert(pos2 + 3 + 3, getMicroDump(addr + offset));

                        pos = pos2 + 3 + 3 + m_memDump.GetMicroDumpStringLenght();
                        continue;
                    }

                    // Enrich $xxxx
                    int pos3 = text.IndexOfAny(new[] { ' ', '\n', '\r' }, pos2);
                    if (pos3 >= 0 && (pos3 - pos2) == 5)
                    {
                        string tAddr = text.Substring(pos2 + 1, 4);
                        int addr = int.Parse(tAddr, NumberStyles.HexNumber);
                        text = text.Insert(pos3, getMicroDump(addr));

                        pos = pos3 + m_memDump.GetMicroDumpStringLenght();
                        continue;
                    }

                    // Enrich $xx
                    if (pos3 >= 0 && (pos3 - pos2) == 3)
                    {
                        string tAddr = text.Substring(pos2 + 1, 2);
                        int addr = int.Parse(tAddr, NumberStyles.HexNumber);
                        text = text.Insert(pos3, getMicroDump(addr));

                        pos = pos3 + m_memDump.GetMicroDumpStringLenght();
                        continue;
                    }

                    pos = pos2 + 1;
                }
                catch (System.Exception)
                {
                    // Skip whatever was giving problems and try again
                    pos += 4;
                }
            }
            return text;
        }

        public void SetSourceView(String text)
        {
            m_readerAndDispaly.SetSouceView(EnrichDumpWithMemory(text));
        }

        private void AppendTextSourceView(String text, Brush brush)
        {
            m_readerAndDispaly.AppendTextSouceView(text, brush);
        }

        private void UpdateLabelView(String text)
        {
            mLabelsBox.Text = text;
        }
        private void UpdateRegsView(String text)
        {
            mRegsBox.Text = text;
        }


        Dictionary<int, int> mAccessedCount = new Dictionary<int, int>();
        Dictionary<int, int> mExecutedCount = new Dictionary<int, int>();

        //byte[] mMemoryC64 = new byte[65536];
        bool mNeedNewMemoryDump = true;

        public void TestForMemoryDump(bool force = false)
        {
            if (!force && mDump == false)
            {
                return;
            }
            if (force || mNeedNewMemoryDump)
            {
                mNeedNewMemoryDump = false;
                //dispatchCommand("!domem");
                m_memDump.RefreshDump(Dispatcher);
            }
        }

        public void AddAssert(AssertDataSource ads)
        {
            mAssertList.Add(ads);
        }

        private void ParseProfileInformation(string theReply)
        {
            //			addr: IO ROM RAM
            //			0000: -- --- rw-
            //			0001: -- --- rw-
            string[] split = theReply.Split('\r');
            int index = 0;
            bool gotProfileInfo = false;
            while (index < split.Length)
            {
                string line = split[index++];
                if (line.Length < 7)
                {
                    continue;
                }
                if (line.IndexOf("addr:") == 0)
                {
                    gotProfileInfo = true;
                    continue;
                }
                if (!gotProfileInfo)
                {
                    continue;
                }
                line = line.ToLower();
                string tAddr = line.Substring(0, 4);
                int theAddr = int.Parse(tAddr, NumberStyles.HexNumber);
                int count;
                if (line.IndexOf('x') != -1)
                {
                    count = 0;
                    mExecutedCount.TryGetValue(theAddr, out count);
                    count++;
                    mExecutedCount[theAddr] = count;
                }

                count = 0;
                mAccessedCount.TryGetValue(theAddr, out count);
                count++;
                mAccessedCount[theAddr] = count;
            }

        }

        private void ParseRegisters(string theReply)
        {
            if (m_registerSet.SetFromString(theReply))
            {
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new OneArgDelegate(UpdateRegsView), theReply);
            }
        }

        private void UpdateLabels()
        {
            string labels = m_readerAndDispaly.UpdateLabels(mUsedLabels, mExecUsed, mAccessUsed, mExecutedCount, mAccessedCount); ;

            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new OneArgDelegate(UpdateLabelView), labels);
        }

        private void dispatchCommand(string command)
        {
            VICECOMManager vcom = VICECOMManager.getVICEComManager();

            if (command.IndexOf("!memmapzap") == 0)
            {
                vcom.addTextCommand("memmapzap", CommandStruct.eMode.DoCommandThenExit, null, null, this.Dispatcher);
            }
            else if (command.IndexOf("!memmapshow") == 0)
            {
                vcom.addTextCommand("memmapshow", CommandStruct.eMode.DoCommandReturnResults, new CommandStruct.CS_TextDelegate(memmapshow_callback), null, this.Dispatcher);
            }
            else if (command.IndexOf("!domem") == 0)
            {
                //vcom.addBinaryMemCommand(0, 0xFFFF, full_mem_dump, null, this.Dispatcher);
            }
            else if (command.IndexOf("!sm") == 0)
            {
                get_registers_callmeback(new NoArgDelegate(show_src_diss_post_registers));
            }
            else if (command.IndexOf("!s") == 0)
            {
                get_registers_callmeback(new NoArgDelegate(show_src_post_registers));
            }
            else if (command.IndexOf("!d") == 0)
            {
                get_registers_callmeback(new NoArgDelegate(show_diss_post_registers));
            }
            else if (command.IndexOf("!d") == 0)
            {
                get_registers_callmeback(new NoArgDelegate(show_diss_post_registers));
            }
            else if (command.IndexOf("!breaklist") == 0)
            {
                vcom.addTextCommand("break", CommandStruct.eMode.DoCommandReturnResults, new CommandStruct.CS_TextDelegate(breaklist_callback), null, this.Dispatcher);
            }
            else if (command.IndexOf("break") == 0 || command.IndexOf("watch") == 0)
            {
                vcom.addTextCommand(command, CommandStruct.eMode.DoCommandReturnResults, new CommandStruct.CS_TextDelegate(break_callback), null, this.Dispatcher);
            }
            else if (command.StartsWith("quit"))
            {
                vcom.addTextCommand("quit", CommandStruct.eMode.DoCommandFireCallback, new CommandStruct.CS_TextDelegate(quit_callback), null, this.Dispatcher);
            }
            else
            {
                // Any other commands get here
                //bool silent = false;
                if (command.IndexOf('!') == 0)
                {
                    command = command.Substring(1);
                    //silent = true;
                }

                if (command.IndexOf("x") != 0)
                {
                    vcom.addTextCommand(command, CommandStruct.eMode.DoCommandReturnResults, new CommandStruct.CS_TextDelegate(command_just_show_reply), null, this.Dispatcher);
                }
                else
                {
                    vcom.addTextCommand(command, CommandStruct.eMode.DoCommandOnly, null, null, this.Dispatcher);
                }
            }
        }

        private void memmapshow_callback(string reply, object userData)
        {
            ParseProfileInformation(reply);
            UpdateLabels();
            dispatchCommand("!memmapzap");
        }

        private void get_registers_callmeback(NoArgDelegate callme)
        {
            VICECOMManager vcom = VICECOMManager.getVICEComManager();
            vcom.addTextCommand("r", CommandStruct.eMode.DoCommandReturnResults, new CommandStruct.CS_TextDelegate(get_registers_callback), callme, this.Dispatcher);
        }

        private void get_registers_callback(string reply, object userData)
        {
            if (reply.Length > 0)
            {
                ParseRegisters(reply);
                UpdateLabels();
                NoArgDelegate del = userData as NoArgDelegate;
                del(); //call back
            }
        }

        private ShowSrcDissStruct show_diss_common()
        {
            return m_readerAndDispaly.show_diss_common();
        }

        private void show_src_diss_post_registers()
        {
            ShowSrcDissStruct sms = show_diss_common();
            if (sms == null) { SetSourceView("Source not found for this address "); return; }

            sms.displayDissCallback = show_src_diss_get_post_dissasem;
            string command = "d " + sms.startPrev.ToString("X") + " " + m_registerSet.GetPC().ToString("X");
            VICECOMManager vcom = VICECOMManager.getVICEComManager();
            CommandStruct.CS_TextDelegate callback = new CommandStruct.CS_TextDelegate(show_src_diss_get_pre_dissasem);
            vcom.addTextCommand(command, CommandStruct.eMode.DoCommandReturnResults, callback, sms, this.Dispatcher);
        }

        private void show_src_diss_get_pre_dissasem(string reply, object userData)
        {
            ShowSrcDissStruct sms = userData as ShowSrcDissStruct;
            sms.disassemblyBefore = reply;
            string command = "d " + m_registerSet.GetPC().ToString("X") + " " + sms.endNext.ToString("X");
            VICECOMManager vcom = VICECOMManager.getVICEComManager();
            vcom.addTextCommand(command, CommandStruct.eMode.DoCommandReturnResults, sms.displayDissCallback, sms, this.Dispatcher);
        }

        private void show_src_diss_get_post_dissasem(string reply, object userData)
        {
            string displayText = m_readerAndDispaly.ShowSrcDissGetPostDissaem(reply, userData);
            SetSourceView(displayText);
        }

        private void show_diss_post_registers()
        {
            ShowSrcDissStruct sms = show_diss_common();
            if (sms == null) {
                SetSourceView("Source not found for this address ");
                sms = new ShowSrcDissStruct()
                {
                    endNext = m_registerSet.GetPC() + 0x20
                    , startPrev = m_registerSet.GetPC()
                };
            }
            sms.displayDissCallback = new CommandStruct.CS_TextDelegate(show_diss_get_post_dissasem);
            string command = "d " + sms.startPrev.ToString("X") + " " + m_registerSet.GetPC().ToString("X");
            VICECOMManager vcom = VICECOMManager.getVICEComManager();
            vcom.addTextCommand(command, CommandStruct.eMode.DoCommandReturnResults, new CommandStruct.CS_TextDelegate(show_src_diss_get_pre_dissasem), sms, this.Dispatcher);
        }

        private void show_diss_get_post_dissasem(string reply, object userData)
        {
            string displayText = String.Empty;
            try
            {
                ShowSrcDissStruct sms = userData as ShowSrcDissStruct;
                string[] split = sms.disassemblyBefore.Split('\r');
                bool doingBefore = true;
                int index = 0;
                while (index < split.Length)
                {
                    string line = split[index++];
                    if (line.Length < 7)
                    {
                        continue;
                    }
                    string tAddr = line.Substring(3);
                    tAddr = tAddr.Substring(0, 4);
                    int theAddr = int.Parse(tAddr, NumberStyles.HexNumber);
                    if (doingBefore && theAddr >= m_registerSet.GetPC())
                    {
                        split = reply.Split('\r');
                        index = 0;
                        doingBefore = false;
                        continue;
                    }

                    Brush brush = null;
                    if (theAddr == m_registerSet.GetPC())
                    {
                        line = ">>>> " + line;
                        brush = Brushes.LightBlue;
                    }
                    else
                    {
                        line = "     " + line;
                    }
                    line += "\r";

                    displayText += line;
                }
            }
            catch (System.Exception)
            {
            }

            SetSourceView(displayText);
        }

        private void show_src_post_registers()
        {
            string displayText = m_readerAndDispaly.ShowSrcPostRegisters();
            SetSourceView(displayText);
        }

        private void command_just_show_reply(string reply, object userData)
        {
            SetSourceView(reply);
        }

        private void quit_callback(string reply, object userData)
        {
            Close();
        }

        private void breaklist_callback(string reply, object userData)
        {
            mBreakPoints.Clear();
            break_callback(reply, userData);

        }

        private void break_callback(string reply, object userData)
        {
            string[] lines = reply.Split('\r');

            mBreakPointDisplay.ItemsSource = null;
            foreach (string line in lines)
            {
                try
                {
                    Match match = RegexMan.BreakPointResult.Match(line);
                    if (match.Success)
                    {
                        int Number = Int32.Parse(match.Groups[(int)RegexMan.eBreakPointResult.number].Value);
                        //we don't want to have Asserts mixed in with breakpoints so make sure the number is high enough
                        if (Number > mAssertList.Count)
                        {
                            BreakPointDataSource test = new BreakPointDataSource();
                            test.setFromMatch(match);
                            mBreakPoints.Add(test);
                        }
                    }

                    mBreakPointDisplay.ItemsSource = mBreakPoints;
                    mBreakPointDisplay.Items.Refresh(); // make sure the other thread knows we changed stuff
                }
                catch
                {

                }
            }
        }

        private void commandBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string command = mCommandBox.Text.Trim();

                //trap and evelvate commands 
                if (command.StartsWith("z"))
                {
                    command = "!" + command;
                }
                else if (command.StartsWith("n"))
                {
                    command = "!" + command;
                }
                else if (command.StartsWith("ret"))
                {
                    command = "!" + command;
                }

                command = m_readerAndDispaly.PostEnterKeyForCommand(command);

                dispatchCommand(command);
                mCommandBox.Clear();
            }
        }

        private void HandleCheckBoxes()
        {
            mDump = (mDoDump.IsChecked == true);
            mUsedLabels = (mCheckUsedLabels.IsChecked == true);
            mAccessUsed = (mCheckAccessUse.IsChecked == true);
            mExecUsed = (mCheckExecUse.IsChecked == true);
        }
        private void GotMsgFromVice(string text)
        {
            Match match = RegexMan.BreakPointHit.Match(text);
            if (match.Success)
            {
                //we have a break point hit, but does anybody want it?
                if (BreakPointDispatcher.getBreakPointDispatcher().checkBreakPointAndDisptach(match) == false)
                {
                    HandleCodeView();
                }
            }
            else
            {
                HandleCodeView(); //for now
            }
        }
        private void HandleCodeView()
        {
            HandleCheckBoxes();
            TestForMemoryDump();
            //mCommands.Add("!r"); //all the sub commands do this anyway
            if (mDoSource.IsChecked == true && mDoDisassembly.IsChecked == true)
            {
                dispatchCommand("!sm");
            }
            else if (mDoSource.IsChecked == true && mDoDisassembly.IsChecked == false)
            {
                dispatchCommand("!s");
            }
            else if (mDoSource.IsChecked == false && mDoDisassembly.IsChecked == true)
            {
                dispatchCommand("!d");
            }
            else if (mDoSource.IsChecked == false && mDoDisassembly.IsChecked == false)
            {
                //mCommands.Add("!cls");
                SetSourceView(String.Empty);
            }
        }

        private void Button_Click_Break(object sender, RoutedEventArgs e)
        {
            mNeedNewMemoryDump = true;
            HandleCodeView();
        }

        private void Button_Click_Go(object sender, RoutedEventArgs e)
        {
            // mNeedNewMemoryDump = true; //do we really need a new mem dump on an X won't that cause the data backup when you quit?
            dispatchCommand("x");
        }

        private void Button_Click_StepIn(object sender, RoutedEventArgs e)
        {
            mNeedNewMemoryDump = true;
            dispatchCommand("!z");
            HandleCodeView();
        }

        private void mDoSource_Click(object sender, RoutedEventArgs e)
        {
            HandleCodeView();
        }

        private void mDoDisassembly_Click(object sender, RoutedEventArgs e)
        {
            HandleCodeView();
        }

        private void mCheckUsedLabels_Click(object sender, RoutedEventArgs e)
        {
            HandleCodeView();
        }

        private void Button_Click_StepOver(object sender, RoutedEventArgs e)
        {
            mNeedNewMemoryDump = true;
            dispatchCommand("!n");
            HandleCodeView();
        }

        private void Button_Click_StepOut(object sender, RoutedEventArgs e)
        {
            mNeedNewMemoryDump = true;
            dispatchCommand("!ret");
            HandleCodeView();
        }

        private void Button_Click_ProfileClear(object sender, RoutedEventArgs e)
        {
            mAccessedCount.Clear();
            mExecutedCount.Clear();
            dispatchCommand("!memmapzap");
        }

        private void Button_Click_ProfileAdd(object sender, RoutedEventArgs e)
        {
            dispatchCommand("!memmapshow");
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            HandleCheckBoxes();
            UpdateLabels();
        }

        private void CheckBox_Checked_1(object sender, RoutedEventArgs e)
        {
            HandleCheckBoxes();
            UpdateLabels();
        }

        private void viewSprites_Click(object sender, RoutedEventArgs e)
        {
            SpriteView SV = new SpriteView(this);
            //set up here
            SV.Show();
        }

        private void viewChars_Click(object sender, RoutedEventArgs e)
        {
            CharView CV = new CharView(this);
            CV.Show();
        }

        private void viewVICBitmap_Click(object sender, RoutedEventArgs e)
        {
            VICBitmap VBV = new VICBitmap(this);
            VBV.Show();
        }

        private void viewVDCBitmap_Click(object sender, RoutedEventArgs e)
        {
            VDCBitmap vdcb = new VDCBitmap(this);
            vdcb.Show();
        }

        private void mDoDump_Checked(object sender, RoutedEventArgs e)
        {
            HandleCheckBoxes();
            UpdateLabels();
        }

        private void mScreenButton_Click(object sender, RoutedEventArgs e)
        {
            ScreenView SV = new ScreenView(this);
            SV.Show();
        }

        private void mBreakpointEnabledCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            BreakPointDataSource ds = cb.DataContext as BreakPointDataSource;
            int breakNum = ds.Number;
            dispatchCommand("enable " + breakNum);
        }

        private void mBreakpointEnabledCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            BreakPointDataSource ds = cb.DataContext as BreakPointDataSource;
            int breakNum = ds.Number;
            dispatchCommand("disable " + breakNum);
        }

        private void OnAssertChecked(object sender, RoutedEventArgs e)
        {
            DataGridCell dgc = sender as DataGridCell;
            AssertDataSource ADS = dgc.DataContext as AssertDataSource;
            int breakNum = ADS.Number;
            dispatchCommand("enable " + breakNum);
        }

        private void OnAssertUnchecked(object sender, RoutedEventArgs e)
        {
            DataGridCell dgc = sender as DataGridCell;
            AssertDataSource ADS = dgc.DataContext as AssertDataSource;
            int breakNum = ADS.Number;
            dispatchCommand("disable " + breakNum);
        }

        private void removeButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            BreakPointDataSource ds = button.DataContext as BreakPointDataSource;
            int breakNum = ds.Number;
            dispatchCommand("del " + breakNum);
            mBreakPoints.Remove(ds);
            mBreakPointDisplay.ItemsSource = null;
            mBreakPointDisplay.ItemsSource = mBreakPoints;
        }

        private void calculator_Click(object sender, RoutedEventArgs e)
        {
            MemCalc MC = new MemCalc();
            MC.Show();
        }

        private void mDoChis_Click(object sender, RoutedEventArgs e)
        {
            ChisViewer CV = new ChisViewer();
            CV.Show();
        }

        private void Button_Click_Tweak(object sender, RoutedEventArgs e)
        {
            TweakPanel TP = new TweakPanel(this);
            TP.Show();
        }

        private void mShowScriptPanel_Click(object sender, RoutedEventArgs e)
        {
            ScriptPanel SP = new ScriptPanel();
            SP.Show();
        }

        private void Button_Click_WatchWindow(object sender, RoutedEventArgs e)
        {
            LiveWatch LW = new LiveWatch();
            LW.Show();
        }
    }

    public static class CustomCommands
    {
        public static readonly RoutedUICommand StepOver = new RoutedUICommand
        (
            "StepOver",
            "StepOver",
            typeof(CustomCommands)
        );

        public static readonly RoutedUICommand StepIn = new RoutedUICommand
        (
            "StepIn",
            "StepIn",
            typeof(CustomCommands)
        );

        public static readonly RoutedUICommand StepOut = new RoutedUICommand
        (
            "StepOut",
            "StepOut",
            typeof(CustomCommands)
        );

        public static readonly RoutedUICommand Run = new RoutedUICommand
        (
            "Run",
            "Run",
            typeof(CustomCommands)
        );
        
    }
}
