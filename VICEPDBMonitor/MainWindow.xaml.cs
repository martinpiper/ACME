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

namespace VICEPDBMonitor
{
	public class AddrInfo
	{
		public int mAddr = -1;
		public int mPrevAddr = -1;
		public int mNextAddr = -1;
		public int mZone = -1;
		public int mFile = -1;
		public int mLine = -1;
	}

	public class LabelInfo
	{
		public int mAddr;
		public int mZone;
		public string mLabel;
		public bool mUsed;
		public bool mMemory;
	}

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		bool mDump = false;
		bool mUsedLabels = false;
		bool mAccessUsed = false;
		bool mExecUsed = false;
		List<string> mSourceIncludes = new List<string>();
		string[] mSourceFileNames = null;
		int mSourceFileNamesLength = 0;
		List<string> mSourceFileNamesFound = new List<string>();
		List<List<string>> mSourceFiles = new List<List<string>>();
		List<LabelInfo> mAllLabels = new List<LabelInfo>();
		SortedDictionary<int, AddrInfo> mAddrInfoByAddr = new SortedDictionary<int, AddrInfo>();
		MultiMap<int, LabelInfo> mLabelInfoByAddr = new MultiMap<int, LabelInfo>();
		MultiMap<int, LabelInfo> mLabelInfoByZone = new MultiMap<int, LabelInfo>();
		MultiMap<string, LabelInfo> mLabelInfoByLabel = new MultiMap<string, LabelInfo>();
        Regex mBreakPointResultRegex;
        Regex mBreakPointHitRegex;
        List<BreakPointDataSource> mBreakPoints;

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
           
            mBreakPoints = new List<BreakPointDataSource>();
            mBreakPointDisplay.ItemsSource = mBreakPoints;

            VICIIRenderer.initRenderer(); //load charsets
                
			string line;
			string[] commandLineArgs = Environment.GetCommandLineArgs();

			int i;
			for (i = 1; i < commandLineArgs.Length; i++)
			{
				int localFileIndex = 0;

				// Read the file and parse it line by line.
				using (System.IO.StreamReader file = new System.IO.StreamReader(commandLineArgs[i]))
				{
					while ((line = file.ReadLine()) != null)
					{
						if (line.IndexOf("INCLUDES:") == 0)
						{
							int lines = int.Parse(line.Substring(9));
							mSourceIncludes.Clear();
							while (lines-- > 0)
							{
								line = file.ReadLine();
								mSourceIncludes.Add(line);
							}
						}
						else if (line.IndexOf("FILES:") == 0)
						{
							localFileIndex = mSourceFileNamesLength;
							int lines = int.Parse(line.Substring(6));
							mSourceFileNamesLength += lines;
							if (mSourceFileNames != null)
							{
								// Copy old into new
								string[] tempNames = new string[mSourceFileNamesLength];
								int j;
								for (j = 0; j < localFileIndex; j++)
								{
									tempNames[j] = mSourceFileNames[j];
								}
								mSourceFileNames = tempNames;
							}
							else
							{
								mSourceFileNames = new string[mSourceFileNamesLength];
							}
							while (lines-- > 0)
							{
								line = file.ReadLine();

                                Char[] separator = { ':' };
                                string[] tokens = line.Split(separator, 2);
                                mSourceFileNames[localFileIndex + int.Parse(tokens[0])] = tokens[1];
							}
						}
						else if (line.IndexOf("ADDRS:") == 0)
						{
							int lines = int.Parse(line.Substring(6));
							int baseZone = 0;
							if (mLabelInfoByZone.Count > 0)
							{
								baseZone = mLabelInfoByZone.Keys.Max();
							}
							while (lines-- > 0)
							{
								line = file.ReadLine();
								string[] tokens = line.Split(':');
								AddrInfo addrInfo = new AddrInfo();
								addrInfo.mAddr = int.Parse(tokens[0].Substring(1), NumberStyles.HexNumber);
								addrInfo.mZone = int.Parse(tokens[1]);
								if (addrInfo.mZone > 0)
								{
									addrInfo.mZone += baseZone;
								}
								addrInfo.mFile = localFileIndex + int.Parse(tokens[2]);
								addrInfo.mLine = int.Parse(tokens[3]) - 1;	// Files lines are 1 based in the debug file
//								mAddrInfoByAddr.Add(addrInfo.mAddr, addrInfo);
								mAddrInfoByAddr[addrInfo.mAddr] = addrInfo;
							}
						}
						else if (line.IndexOf("LABELS:") == 0)
						{
							int lines = int.Parse(line.Substring(7));
							int baseZone = 0;
							if (mLabelInfoByZone.Count > 0)
							{
								baseZone = mLabelInfoByZone.Keys.Max() + 1;
							}
							while (lines-- > 0)
							{
								line = file.ReadLine();
								string[] tokens = line.Split(':');
								LabelInfo labelInfo = new LabelInfo();
								labelInfo.mAddr = int.Parse(tokens[0].Substring(1), NumberStyles.HexNumber);
								labelInfo.mZone = int.Parse(tokens[1]);
								if (labelInfo.mZone > 0)
								{
									labelInfo.mZone += baseZone;	// Helps to distinguish zones for multiple PDB files
								}
								labelInfo.mLabel = tokens[2];
								labelInfo.mUsed = int.Parse(tokens[3]) == 1;
								labelInfo.mMemory = int.Parse(tokens[4]) == 1;
								mAllLabels.Add(labelInfo);
								mLabelInfoByAddr.Add(labelInfo.mAddr, labelInfo);
								mLabelInfoByZone.Add(labelInfo.mZone, labelInfo);
								mLabelInfoByLabel.Add(labelInfo.mLabel, labelInfo);
							}
						}
					}

					mAllLabels.Sort((a, b) => b.mLabel.Length.CompareTo(a.mLabel.Length));

					file.Close();
				}

				int l;
				// Only process new names this iteration
				// MPi: TODO: Use mSourceIncludes
				for (l = localFileIndex; l < mSourceFileNamesLength; l++ )
				{
					string name = mSourceFileNames[l];
					try
					{
						List<string> aFile = new List<string>();
						string newPath = name;
						if (!System.IO.File.Exists(newPath))
						{
							newPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(commandLineArgs[i]), name);
							if (!System.IO.File.Exists(newPath))
							{
								newPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(commandLineArgs[i]), System.IO.Path.GetFileName(name));
							}
						}
						using (System.IO.StreamReader file = new System.IO.StreamReader(newPath))
						{
							while ((line = file.ReadLine()) != null)
							{
								aFile.Add(line);
							}
							file.Close();
						}
						mSourceFiles.Add(aFile);
						mSourceFileNamesFound.Add(newPath);
					}
					catch (System.Exception)
					{
						mSourceFiles.Add(new List<string>());
						mSourceFileNamesFound.Add("");
					}
				}
			
			}

			int thePrevAddr = -1;
			foreach (KeyValuePair<int, AddrInfo> pair in mAddrInfoByAddr)
			{
				pair.Value.mPrevAddr = thePrevAddr;
				thePrevAddr = pair.Value.mAddr;
			}
			thePrevAddr = -1;
			foreach (KeyValuePair<int, AddrInfo> pair in mAddrInfoByAddr.Reverse())
			{
				pair.Value.mNextAddr = thePrevAddr;
				thePrevAddr = pair.Value.mAddr;
			}


//			mCommands.Add("r");
//			mCommands.Add("m 0000 ffff");
//			mCommands.Add("x");
//			mCommands.Add("!s");
//			mCommands.Add("!sm");

            dispatchCommand("!breaklist");

			HandleCodeView();
	
		}

		int mPC = 0;
		int mRegA = 0;
		int mRegX = 0;
		int mRegY = 0;
		int mSP = 0;
//		int mNV_BDIZC = 0;
		

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

		int getSafeC64Memory(int addr)
		{
			return (int)mMemoryC64[addr & 0xffff];
		}

		static int kMicroDumpStringLength = 16;
		string getMicroDump(int addr)
		{
			if (mDump == false)
			{
				return "";
			}
			string ret;
			ret = " >(" + getSafeC64Memory(addr).ToString("X2") + " " + getSafeC64Memory(addr+1).ToString("X2") + " " + getSafeC64Memory(addr+2).ToString("X2") + " " + getSafeC64Memory(addr+3).ToString("X2") + ")<";
			return ret;
		}

		string EnrichDumpWithMemory(string text)
		{
			if (mDump == false)
			{
				return text;
			}
			int pos = 0;
			while (pos < text.Length)
			{
				try
				{
					int pos2 = text.IndexOf("$" , pos);
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
						int indirect = mMemoryC64[addr] + (((int)mMemoryC64[addr+1]) << 8);
						text = text.Insert(pos2 + 5 + 3, getMicroDump(indirect + mRegY));

						pos = pos2 + 5 + 3 + kMicroDumpStringLength;
						continue;
					}

					// Enrich ($xx,X)
					if (text[pos2 + 3] == ',' && text[pos2 + 5] == ')')
					{
						string tAddr = text.Substring(pos2 + 1, 2);
						int addr = int.Parse(tAddr, NumberStyles.HexNumber);
						int indirect = mMemoryC64[(addr + mRegX) & 0xff] + (((int)mMemoryC64[(addr + mRegX + 1) & 0xff]) << 8);
						text = text.Insert(pos2 + 5 + 3, getMicroDump(indirect));

						pos = pos2 + 5 + 3 + kMicroDumpStringLength;
						continue;
					}

					// Enrich $xxxx,x/y
					if (text[pos2 + 5] == ',' && (text[pos2 + 5 + 1] == 'X' || text[pos2 + 5 + 1] == 'x' || text[pos2 + 5 + 1] == 'Y' || text[pos2 + 5 + 1] == 'y'))
					{
						string tAddr = text.Substring(pos2 + 1, 4);
						int addr = int.Parse(tAddr, NumberStyles.HexNumber);
						int offset = mRegX;
						if (text[pos2 + 5 + 1] == 'Y' || text[pos2 + 5 + 1] == 'y')
						{
							offset = mRegY;
						}
						text = text.Insert(pos2 + 5 + 3, getMicroDump(addr + offset));

						pos = pos2 + 5 + 3 + kMicroDumpStringLength;
						continue;
					}

					// Enrich $xx,
					if (text[pos2 + 3] == ',' && (text[pos2 + 3 + 1] == 'X' || text[pos2 + 3 + 1] == 'x' || text[pos2 + 3 + 1] == 'Y' || text[pos2 + 3 + 1] == 'y'))
					{
						string tAddr = text.Substring(pos2 + 1, 2);
						int addr = int.Parse(tAddr, NumberStyles.HexNumber);
						int offset = mRegX;
						if (text[pos2 + 3 + 1] == 'Y' || text[pos2 + 3 + 1] == 'y')
						{
							offset = mRegY;
						}
						text = text.Insert(pos2 + 3 + 3, getMicroDump(addr + offset));

						pos = pos2 + 3 + 3 + kMicroDumpStringLength;
						continue;
					}

					// Enrich $xxxx
					int pos3 = text.IndexOfAny(new[] {' ','\n','\r'} , pos2);
					if (pos3 >= 0 && (pos3 - pos2) == 5)
					{
						string tAddr = text.Substring(pos2 + 1, 4);
						int addr = int.Parse(tAddr, NumberStyles.HexNumber);
						text = text.Insert(pos3, getMicroDump(addr));

						pos = pos3 + kMicroDumpStringLength;
						continue;
					}

					// Enrich $xx
					if (pos3 >= 0 && (pos3 - pos2) == 3)
					{
						string tAddr = text.Substring(pos2 + 1, 2);
						int addr = int.Parse(tAddr, NumberStyles.HexNumber);
						text = text.Insert(pos3, getMicroDump(addr));

						pos = pos3 + kMicroDumpStringLength;
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
			text = EnrichDumpWithMemory(text);
			mTextBox.BeginChange();
			mTextBox.Document.Blocks.Clear();
			if (null == text || text.Length == 0)
			{
				mTextBox.EndChange();
				return;
			}
			try
			{
                /*Run r = new Run("", mTextBox.CaretPosition.DocumentEnd);
				r.Background = null;*/
                int split = text.IndexOf("=>");
                if(split < 0 )
                {
                    split = text.IndexOf(">>>>");
                }
                if (split < 0)
                {
                    mTextBox.AppendText(text);
                    mTextBox.EndChange();
                }
                else
                {
                    mTextBox.EndChange();
                    string before = text.Substring(0, split);
                    int endOfLine = text.IndexOf('\r', split);
                    string currLine = text.Substring(split, endOfLine - split);
                    string after = text.Substring(endOfLine);
                    Run topRun = new Run(before)
                    {
                        Background = mTextBox.Background
                    };
                    Run currRun = new Run(currLine)
                    {
                        Background = Brushes.LightGray
                    };
                    Run afterRun = new Run(after)
                    {
                        Background = mTextBox.Background
                    };
                    Paragraph para = new Paragraph();
                    para.Inlines.Add(topRun);
                    para.Inlines.Add(currRun);
                    para.Inlines.Add(afterRun);
                    FlowDocument flow = new FlowDocument(para);
                    mTextBox.Document = flow;
                   
                }
                /*
				// mTextBox.Selection.ApplyPropertyValue(TextElement.BackgroundProperty , Brushes.Red);
				TextRange searchRange = new TextRange(mTextBox.Document.ContentStart, mTextBox.Document.ContentEnd);
				int offset = searchRange.Text.IndexOf("=>");
				if (offset < 0)
				{
					offset = searchRange.Text.IndexOf(">>>>");
				}
				if (offset >= 0)
				{
					int lineLength = searchRange.Text.IndexOf("\r", offset);
					lineLength = lineLength - offset;
					TextPointer start = GetTextPositionAtOffset(searchRange.Start, offset);
					TextRange result = new TextRange(start, GetTextPositionAtOffset(start, lineLength));

					result.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.LightGray);
				}*/
			}
			catch (System.Exception)
			{
			}			
		}

		private void AppendTextSourceView(String text, Brush brush)
		{
			if (null == text || text.Length == 0)
			{
				return;
			}

			Run r = new Run("", mTextBox.CaretPosition.DocumentEnd);
			r.Background = brush;

			mTextBox.AppendText(text);

			r = new Run("", mTextBox.CaretPosition.DocumentEnd);
			r.Background = null;
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

		byte[] mMemoryC64 = new byte[65536];
		bool mNeedNewMemoryDump = true;

		private void TestForMemoryDump(bool force = false)
		{
			if (!force && mDump == false)
			{
				return;
			}
			if (force || mNeedNewMemoryDump)
			{
				mNeedNewMemoryDump = false;
                dispatchCommand("!domem");
			}
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
            //  ADDR AC XR YR SP 00 01 NV-BDIZC LIN CYC  STOPWATCH
            //.;0427 ad 00 00 f4 2f 37 10100100 000 000   87547824
            int index = theReply.IndexOf(".;"); //seems when you have break point this can get messed up
            if (index >= 0)
            {
                string parse = theReply.Substring(index);
                string[] parse2 = parse.Split(' ');
                mPC = int.Parse(parse2[0].Substring(2), NumberStyles.HexNumber);
                mRegA = int.Parse(parse2[1], NumberStyles.HexNumber);
                mRegX = int.Parse(parse2[2], NumberStyles.HexNumber);
                mRegY = int.Parse(parse2[3], NumberStyles.HexNumber);
                mSP = int.Parse(parse2[4], NumberStyles.HexNumber);
                //			mNV_BDIZC = int.Parse(parse2[7], NumberStyles.Binary); // TODO Binary

                this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new OneArgDelegate(UpdateRegsView), theReply);
            }
		}

		private void UpdateLabels()
		{
			string labels = "";
			try
			{
				List<string> allLabels = new List<string>();
				int theZone = mAddrInfoByAddr[mPC].mZone;
				while (theZone >= 0)
				{
					List<LabelInfo> theLabels = mLabelInfoByZone[theZone];

					foreach (LabelInfo aLabel in theLabels)
					{
						if ((mUsedLabels == false) || ((mUsedLabels == true) && (aLabel.mUsed == true)))
						{
							string labelText = "";
							int count;
							if (mExecUsed && mExecutedCount.TryGetValue(aLabel.mAddr, out count))
							{
								labelText += "E" + count + ":";
							}
							if (mAccessUsed && mAccessedCount.TryGetValue(aLabel.mAddr, out count))
							{
								labelText += "A" + count + ":";
							}
							if (theZone != 0)
							{
								labelText = ".";
							}

							labelText += aLabel.mLabel + " $" + aLabel.mAddr.ToString("X") + getMicroDump(aLabel.mAddr);
							allLabels.Add(labelText);
						}
					}
					// MPi: TODO: Replace with previous zone in the hierarchy when ACME saves it
					if (theZone > 0)
					{
						theZone = 0;	// For now just display the global zone
					}
					else
					{
						break;
					}
				}
				allLabels.Sort();

				foreach (string line in allLabels)
				{
					labels += line + "\n";
				}
			}
			catch (System.Exception)
			{
				
			}
			
			this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new OneArgDelegate(UpdateLabelView), labels);
		}

        private void dispatchCommand(string command)
        {
            VICECOMManager vcom = VICECOMManager.getVICEComManager();

            if (command.IndexOf("!memmapzap") == 0)
            {
                vcom.addTextCommand("memmapzap", CommandStruct.eMode.DoCommandThenExit, null, null,this.Dispatcher);
            }
            else if (command.IndexOf("!memmapshow") == 0)
            {
                vcom.addTextCommand("memmapshow", CommandStruct.eMode.DoCommandReturnResults, new CommandStruct.CS_TextDelegate(memmapshow_callback),null, this.Dispatcher);
            }
            else if (command.IndexOf("!domem") == 0)
            {
                vcom.addBinaryMemCommand(0, 0xFFFF, full_mem_dump, null, this.Dispatcher);
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
            else
            {
                // Any other commands get here
                bool silent = false;
                if (command.IndexOf('!') == 0)
                {
                    command = command.Substring(1);
                    silent = true;
                }
 
                if (command.IndexOf("x") != 0)
                {
                    vcom.addTextCommand(command, CommandStruct.eMode.DoCommandReturnResults, new CommandStruct.CS_TextDelegate(command_just_show_reply),null, this.Dispatcher);
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

        private class ShowSrcDissStruct
        {
            public int startPrev;
            public int endNext;
            public string disassemblyBefore;
            public string disassemblyAfter;
            public CommandStruct.CS_TextDelegate displayDissCallback;
        }

        private ShowSrcDissStruct show_diss_common()
        {
            try
            {
                // MPi: TODO: Tweak the 10 range based on the display height?
                int range = 15;
                int startPrev = mPC;
                // Step backwards trying to find a good starting point to disassemble
                while (range-- > 0)
                {
                    AddrInfo addrInfo2 = mAddrInfoByAddr[startPrev];
                    if (addrInfo2.mPrevAddr < 0)
                    {
                        break;
                    }
                    startPrev = addrInfo2.mPrevAddr;
                }
                // MPi: TODO: Tweak the 10 range based on the display height?
                range = 20;
                // Step forwards trying to find a good ending point to disassemble
                int endNext = mPC;
                while (range-- > 0)
                {
                    AddrInfo addrInfo2 = mAddrInfoByAddr[endNext];
                    if (addrInfo2.mNextAddr < 0)
                    {
                        break;
                    }
                    endNext = addrInfo2.mNextAddr;
                }
                
                ShowSrcDissStruct sms = new ShowSrcDissStruct()
                {
                    endNext = endNext
                    ,startPrev = startPrev
                };
                return sms;
            }
            catch (Exception ex)
            {

            }
            return null;
        }

        private void show_src_diss_post_registers()
        {
            ShowSrcDissStruct sms = show_diss_common();
            if(sms == null) { SetSourceView("Source not found for this address "); return; }

            sms.displayDissCallback = show_src_diss_get_post_dissasem;
            string command = "d " + sms.startPrev.ToString("X") + " " + mPC.ToString("X");
            VICECOMManager vcom = VICECOMManager.getVICEComManager();
            CommandStruct.CS_TextDelegate callback = new CommandStruct.CS_TextDelegate(show_src_diss_get_pre_dissasem);
            vcom.addTextCommand(command, CommandStruct.eMode.DoCommandReturnResults, callback, sms, this.Dispatcher);
        }

        private void show_src_diss_get_pre_dissasem(string reply, object userData)
        {
            ShowSrcDissStruct sms = userData as ShowSrcDissStruct;
            sms.disassemblyBefore = reply;
            string command = "d " + mPC.ToString("X") + " " + sms.endNext.ToString("X");
            VICECOMManager vcom = VICECOMManager.getVICEComManager();
            vcom.addTextCommand(command, CommandStruct.eMode.DoCommandReturnResults, sms.displayDissCallback, sms, this.Dispatcher);
        }

        private void show_src_diss_get_post_dissasem(string reply, object userData)
        {
            ShowSrcDissStruct sms = userData as ShowSrcDissStruct;
            sms.disassemblyAfter = reply;
            string displayText = String.Empty;
            try
            {
                string lastSourceDisplayed = "";
                int lastSourceIndexDisplayed = -1;
                int lastSourceLineDisplayed = -1;

                int[] lastDisplayedLine = new int[mSourceFileNamesLength];
                int i;
                for (i = 0; i < mSourceFileNamesLength; i++)
                {
                    lastDisplayedLine[i] = 0;
                }

                //							gotText += disassemblyBefore;
                //							gotText += ">>>\n";
                //							gotText += disassemblyAfter;
                // Get something like:
                /*
                    .C:0427  AD 00 04    LDA $0400
                    .C:042a  AD 27 04    LDA $0427
                    ...
                    .C:0439  60          RTS
                    .C:043a  AD 3A 04    LDA $043A
                */
                string[] split = sms.disassemblyBefore.Split('\r');
                bool doingBefore = true;
                int index = 0;
                String lastSourceLine = "";
                while (index < split.Length)
                {
                    string line = split[index++];
                    if (line.Length < 7)
                    {
                        continue;
                    }
                    if (!line.StartsWith(".C:"))
                    {
                        continue;
                    }
                    string tAddr = line.Substring(3);
                    tAddr = tAddr.Substring(0, 4);
                    int theAddr = int.Parse(tAddr, NumberStyles.HexNumber);
                    if (doingBefore && theAddr >= mPC)
                    {
                        split = sms.disassemblyAfter.Split('\r');
                        index = 0;
                        doingBefore = false;
                        continue;
                    }

                    try
                    {
                        AddrInfo addrInfo = mAddrInfoByAddr[theAddr];
                        if (lastSourceDisplayed != mSourceFileNames[addrInfo.mFile])
                        {
                            lastSourceDisplayed = mSourceFileNames[addrInfo.mFile];
                            displayText += "--- " + lastSourceDisplayed + " ---\r";
                        }
                        if ((addrInfo.mLine - lastDisplayedLine[addrInfo.mFile]) > 5)
                        {
                            lastDisplayedLine[addrInfo.mFile] = addrInfo.mLine - 5;
                            if (lastDisplayedLine[addrInfo.mFile] < 0)
                            {
                                lastDisplayedLine[addrInfo.mFile] = 0;
                            }
                        }
                        if (lastDisplayedLine[addrInfo.mFile] > addrInfo.mLine)
                        {
                            lastDisplayedLine[addrInfo.mFile] = addrInfo.mLine;
                        }
                        for (i = lastDisplayedLine[addrInfo.mFile]; i <= addrInfo.mLine; i++)
                        {
                            if (lastSourceLine.Length > 0)
                            {
                                //gotText += "     " + "                                  " + lastSourceLine + "\n";
                                displayText += "     " + lastSourceLine + "\r";
                                lastSourceLine = "";
                            }

                            if ((lastSourceIndexDisplayed == addrInfo.mFile) && (lastSourceLineDisplayed == i))
                            {
                                // Stop displaying the same source and line multiple times in a row
                                continue;
                            }
                            lastSourceLine = string.Format("{0,5:###}", i) + ": " + mSourceFiles[addrInfo.mFile][i];
                            lastSourceIndexDisplayed = addrInfo.mFile;
                            lastSourceLineDisplayed = i;
                        }
                        lastDisplayedLine[addrInfo.mFile] = addrInfo.mLine + 1;
                    }
                    catch (System.Exception)
                    {

                    }

                    if (theAddr == mPC)
                    {
                        displayText += ">>>> ";
                    }
                    else
                    {
                        displayText += "     ";
                    }
                    if (lastSourceLine.Length > 0)
                    {
                        line = line.PadRight(34, ' ');
                        displayText += line + lastSourceLine + "\r";
                        lastSourceLine = "";
                    }
                    else
                    {
                        displayText += line + "\r";
                    }
                }
            }
            catch (System.Exception)
            {
            }

            SetSourceView(displayText);
        }

        private void show_diss_post_registers()
        {
            ShowSrcDissStruct sms = show_diss_common();
            if (sms == null) {
                SetSourceView("Source not found for this address ");
                sms = new ShowSrcDissStruct()
                {
                     endNext = mPC+0x20
                    ,startPrev = mPC
                };
            }
            sms.displayDissCallback = new CommandStruct.CS_TextDelegate(show_diss_get_post_dissasem);
            string command = "d " + sms.startPrev.ToString("X") + " " + mPC.ToString("X");
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
                    if (doingBefore && theAddr >= mPC)
                    {
                        split = reply.Split('\r');
                        index = 0;
                        doingBefore = false;
                        continue;
                    }

                    Brush brush = null;
                    if (theAddr == mPC)
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
            string displayText = string.Empty;
            try
            {
                AddrInfo addrInfo = mAddrInfoByAddr[mPC];
                // MPi: TODO: Tweak the 20 range based on the display height?
                int range = 20;
                int startPrev = mPC;
                // Step backwards trying to find a good starting point to disassemble
                while (range-- > 0)
                {
                    AddrInfo addrInfo2 = mAddrInfoByAddr[startPrev];
                    if (addrInfo2.mPrevAddr < 0)
                    {
                        break;
                    }
                    startPrev = addrInfo2.mPrevAddr;
                }

                displayText += "File:" + mSourceFileNames[addrInfo.mFile] + "\n";
                displayText += "Line:" + (addrInfo.mLine + 1) + "\n";
                int theLine = addrInfo.mLine - 10;  // MPi: TODO: Tweak the - 10 based on the display height?
                if (theLine < 0)
                {
                    theLine = 0;
                }
                // MPi: TODO: Tweak the 20 toDisplay based on the display height?
                int toDisplay = 30;
                while (toDisplay-- > 0)
                {
                    if (theLine == addrInfo.mLine)
                    {
                        displayText += "=>";
                    }
                    else
                    {
                        displayText += "  ";
                    }
                    string source = mSourceFiles[addrInfo.mFile][theLine++];
                    source = source.Replace('\n', '\r');
                    source.TrimEnd();

                    displayText += source;
                    displayText += "\r";
                }
            }
            catch (System.Exception)
            {
                /*SendCommand("m 0000 ffff");
                theReply = GetReply();
                // No source info, so just dump memory
                gotText += theReply;
                //>C:0000  2f 37 00 aa  b1 91 b3 22  00 00 00 4c  00 00 00 04   /7....."...L....
                //>C:0010  00 00 00 00  00 04 19 16  00 0a 76 a3  00 00 00 00   ..........v.....
                //...
                //>C:ff00  00 00 00 00  00 04 19 16  00 0a 76 a3  00 00 00 00   ..........v.....*/					
            }

            SetSourceView(displayText);
        }

        private void command_just_show_reply(string reply, object userData)
        {
            SetSourceView(reply);
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
                        BreakPointDataSource test = new BreakPointDataSource();
                        test.setFromMatch(match);
                        mBreakPoints.Add(test);
                    }

                    mBreakPointDisplay.ItemsSource = mBreakPoints;
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
                if(command.StartsWith("z"))
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
               
                // Produce a list of variable names and values relevant to the current PC and its zone
                List<LabelInfo> allLabels = new List<LabelInfo>();

				try
				{
					int theZone = mAddrInfoByAddr[mPC].mZone;
					while (theZone >= 0)
					{
						List<LabelInfo> theLabels = mLabelInfoByZone[theZone];
						allLabels.AddRange(theLabels);

						// MPi: TODO: Replace with previous zone in the hierarchy when ACME saves it
						if (theZone > 0)
						{
							theZone = 0;	// For now just display the global zone
						}
						else
						{
							break;
						}
					}
					allLabels.Sort((a,b) => b.mLabel.Length.CompareTo(a.mLabel.Length));
				}
				catch (System.Exception)
				{

				}

				// Now add all other labels to the end of the list
				allLabels.AddRange(mAllLabels);

				// Look for labels after each whitespace
				int pos = 0;
				while (pos < command.Length)
				{
					int testPos = command.IndexOf(' ', pos);
					if (testPos < 0)
					{
						testPos = command.IndexOf('~', pos);
					}
					if (testPos < 0)
					{
						break;
					}
					pos = testPos + 1;

					string remaining = command.Substring(pos);
					if (remaining[0] == '.')
					{
						remaining = remaining.Substring(1);
					}

					// Note the length order gets the most precise match
					LabelInfo found = null;

					if (remaining.Length > 0)
					{
						foreach (LabelInfo label in allLabels)
						{
							if (label.mLabel.Length >= remaining.Length && remaining.StartsWith(label.mLabel))
							{
								found = label;
								break;
							}
						}
					}

					// If it's found then reconstruct the command with the label replaced as a hex number
					if (null != found)
					{
						string theHex = "$" + found.mAddr.ToString("X");
						command = command.Substring(0, pos) + theHex + remaining.Substring(found.mLabel.Length);
						pos += theHex.Length;
					}
					else
					{
						pos++;
					}
				}

                dispatchCommand(command);
				mCommandBox.Clear();
			}
		}

        private void full_mem_dump(byte[] reply, object userData)
        {
            Buffer.BlockCopy(reply, 0, mMemoryC64, 0, reply.Length);
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
                if( BreakPointDispatcher.getBreakPointDispatcher().checkBreakPointAndDisptach(match) == false)
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
			if (mDoSource.IsChecked == true && mDoDisassembly.IsChecked == true )
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
    }
}
