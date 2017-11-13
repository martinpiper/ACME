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

namespace VICEPDBMonitor
{
	public class MultiMap<K, V>
	{
		Dictionary<K, List<V>> mDictionary = new Dictionary<K, List<V>>();

		public void Add(K key, V value)
		{
			List<V> list;
			if (mDictionary.TryGetValue(key, out list))
			{
				// 2A.
				list.Add(value);
			}
			else
			{
				// 2B.
				list = new List<V>();
				list.Add(value);
				mDictionary[key] = list;
			}
		}

		public int Count
		{
			get
			{
				return mDictionary.Count;
			}
		}

		public IEnumerable<K> Keys
		{
			get
			{
				return mDictionary.Keys;
			}
		}

		public List<V> this[K key]
		{
			get
			{
				List<V> list;
				if (!mDictionary.TryGetValue(key, out list))
				{
					list = new List<V>();
					mDictionary[key] = list;
				}
				return list;
			}
		}
	}
	
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
		bool mUsedLabels = false;
		bool mAccessUsed = false;
		bool mExecUsed = false;
		List<string> mSourceIncludes = new List<string>();
		string[] mSourceFileNames = null;
		int mSourceFileNamesLength = 0;
		List<string> mSourceFileNamesFound = new List<string>();
		List<List<string>> mSourceFiles = new List<List<string>>();
		SortedDictionary<int, AddrInfo> mAddrInfoByAddr = new SortedDictionary<int, AddrInfo>();
		MultiMap<int, LabelInfo> mLabelInfoByAddr = new MultiMap<int, LabelInfo>();
		MultiMap<int, LabelInfo> mLabelInfoByZone = new MultiMap<int, LabelInfo>();
		MultiMap<string, LabelInfo> mLabelInfoByLabel = new MultiMap<string, LabelInfo>();

		private delegate void NoArgDelegate();
		private delegate void OneArgDelegate(String arg);
		private delegate void TwoArgDelegate(String arg , Brush brush);

		public MainWindow()
		{
			InitializeComponent();

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
								mLabelInfoByAddr.Add(labelInfo.mAddr, labelInfo);
								mLabelInfoByZone.Add(labelInfo.mZone, labelInfo);
								mLabelInfoByLabel.Add(labelInfo.mLabel, labelInfo);
							}
						}
					}

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

			HandleCodeView();

			NoArgDelegate fetcher = new NoArgDelegate(this.BackgroundThread);
			fetcher.BeginInvoke(null, null);
		}

		int mPC = 0;
		int mRegA = 0;
		int mRegX = 0;
		int mRegY = 0;
		int mSP = 0;
		int mNV_BDIZC = 0;
		List<string> mCommands = new List<string>();

		Socket mSocket;
		String mGotTextWorking = "";

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

		private void SetSourceView(String text)
		{
			mTextBox.BeginChange();
			mTextBox.Document.Blocks.Clear();
			if (null == text || text.Length == 0)
			{
				return;
			}
			Run r = new Run("", mTextBox.CaretPosition.DocumentEnd);
			r.Background = null;

			mTextBox.AppendText(text);

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
			}
			mTextBox.EndChange();
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

		private void SendCommand(string command)
		{
			if (command.Length > 0)
			{
				// Add padding to avoid the VICE monitor command truncation bug
				command += "                                                                           \n";
				byte[] msg = Encoding.ASCII.GetBytes(command);
				int sent = 0;
				while (mSocket.Connected && (sent < msg.Length))
				{
					int ret = mSocket.Send(msg, sent, msg.Length - sent, SocketFlags.None);
					if (ret > 0)
					{
						sent += ret;
					}
					else
					{
						Thread.Sleep(10);
					}
				}
			}
		}

		private string GetReply()
		{
			string theReply = "";

			while (mSocket.Connected)
			{
				byte[] bytes = new byte[500000];
				try
				{
					int got = mSocket.Receive(bytes);
					if (got > 0)
					{
						mGotTextWorking += Encoding.ASCII.GetString(bytes, 0, got);
					}
				}
				catch (System.Exception ex)
				{
//					this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new OneArgDelegate(UpdateUserInterface), "Connected exception: " + ex.ToString());
					Thread.Sleep(100);
				}

				int foundFirstPos = mGotTextWorking.IndexOf("(C:$");
				if (foundFirstPos >= 0 && mGotTextWorking.Length > 16)
				{
					int foundSecondPos = mGotTextWorking.IndexOf("(C:$", foundFirstPos + 9);
					if (foundSecondPos > foundFirstPos)
					{
						int foundThirdPos = mGotTextWorking.IndexOf(") ", foundSecondPos + 4);
						if (foundThirdPos > foundSecondPos)
						{
							foundFirstPos += 10;	// End of the first "(C:$"
							theReply = mGotTextWorking.Substring(foundFirstPos, foundSecondPos - foundFirstPos);
							// Start the next command buffer with valid text
							mGotTextWorking = mGotTextWorking.Substring(foundSecondPos);
							return theReply;
						}
					}
				}
			}			
			
			return "";
		}


		Dictionary<int, int> mAccessedCount = new Dictionary<int, int>();
		Dictionary<int, int> mExecutedCount = new Dictionary<int, int>();

		private void ParseProfileInformation(string theReply)
		{
//			addr: IO ROM RAM
//			0000: -- --- rw-
//			0001: -- --- rw-
			string[] split = theReply.Split('\n');
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
			string parse = theReply.Substring(theReply.IndexOf(".;"));
			string[] parse2 = parse.Split(' ');
			mPC = int.Parse(parse2[0].Substring(2), NumberStyles.HexNumber);
			mRegA = int.Parse(parse2[1], NumberStyles.HexNumber);
			mRegX = int.Parse(parse2[2], NumberStyles.HexNumber);
			mRegY = int.Parse(parse2[3], NumberStyles.HexNumber);
			mSP = int.Parse(parse2[4], NumberStyles.HexNumber);
//			mNV_BDIZC = int.Parse(parse2[7], NumberStyles.Binary); // TODO Binary

			this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new OneArgDelegate(UpdateRegsView), theReply);
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

							labelText += aLabel.mLabel + " $" + aLabel.mAddr.ToString("X");
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
			catch (System.Exception ex)
			{
				
			}
			
			this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new OneArgDelegate(UpdateLabelView), labels);
		}

		private void BackgroundThread()
		{
			while (true)
			{
				try
				{
					String gotText = "";
					bool wasConnected = false;

					if (mSocket == null)
					{
						mSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

						mSocket.Blocking = false;
					}

					try
					{
						mSocket.Connect("localhost", 6510);
					}
					catch (System.Exception /*ex*/)
					{
					}

					string lastCommand = "";

					while (mSocket.Connected)
					{
						if (mSocket.Poll(0, SelectMode.SelectError))
						{
							break;
						}
						wasConnected = true;
						if (mCommands.Count > 0)
						{
							lastCommand = mCommands[0];
							mCommands.RemoveAt(0);

							if (lastCommand.IndexOf("!memmapzap") == 0)
							{
								string theReply;
								mGotTextWorking = "";
								SendCommand("memmapzap");
								theReply = GetReply();
								SendCommand("x");
							}
							else if (lastCommand.IndexOf("!memmapshow") == 0)
							{
								string theReply;
								mGotTextWorking = "";
//								SendCommand("r");
//								theReply = GetReply();
								SendCommand("memmapshow");
								theReply = GetReply();

								ParseProfileInformation(theReply);
								UpdateLabels();

								SendCommand("memmapzap");
								theReply = GetReply();
								SendCommand("x");
							}
							else if (lastCommand.IndexOf("!sm") == 0)
							{
								SendCommand("r");
								string theReply = GetReply();

								ParseRegisters(theReply);
								UpdateLabels();

								gotText = "";

								try
								{
									int[] lastDisplayedLine = new int[mSourceFileNamesLength];
									int i;
									for (i = 0; i < mSourceFileNamesLength; i++)
									{
										lastDisplayedLine[i] = 0;
									}
									// MPi: TODO: Tweak the 10 range based on the display height?
									int range = 10;
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

									string command = "d " + startPrev.ToString("X") + " " + mPC.ToString("X");
									SendCommand(command);
									string disassemblyBefore = GetReply();
									command = "d " + mPC.ToString("X") + " " + endNext.ToString("X");
									SendCommand(command);
									string disassemblyAfter = GetReply();

									string lastSourceDisplayed = "";
									int lastSourceIndexDisplayed = -1;
									int lastSourceLineDisplayed = -1;

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
									string[] split = disassemblyBefore.Split('\n');
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
										string tAddr = line.Substring(3);
										tAddr = tAddr.Substring(0, 4);
										int theAddr = int.Parse(tAddr, NumberStyles.HexNumber);
										if (doingBefore && theAddr >= mPC)
										{
											split = disassemblyAfter.Split('\n');
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
												gotText += "--- " + lastSourceDisplayed + " ---\n";
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
//													gotText += "     " + "                                  " + lastSourceLine + "\n";
													gotText += "     " + lastSourceLine + "\n";
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
											gotText += ">>>> ";
										}
										else
										{
											gotText += "     ";
										}
										if (lastSourceLine.Length > 0)
										{
											line = line.PadRight(34, ' ');
											gotText += line + lastSourceLine + "\n";
											lastSourceLine = "";
										}
										else
										{
											gotText += line + "\n";
										}
									}
								}
								catch (System.Exception)
								{
									SendCommand("m 0000 ffff");
									theReply = GetReply();
									// No source info, so just dump memory
									gotText += theReply;
									//>C:0000  2f 37 00 aa  b1 91 b3 22  00 00 00 4c  00 00 00 04   /7....."...L....
									//>C:0010  00 00 00 00  00 04 19 16  00 0a 76 a3  00 00 00 00   ..........v.....
									//...
									//>C:ff00  00 00 00 00  00 04 19 16  00 0a 76 a3  00 00 00 00   ..........v.....					
								}
							}
							else if (lastCommand.IndexOf("!s") == 0)
							{
								SendCommand("r");
								string theReply = GetReply();

								ParseRegisters(theReply);
								UpdateLabels();

								gotText = "";

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

									gotText += "File:" + mSourceFileNames[addrInfo.mFile] + "\n";
									gotText += "Line:" + (addrInfo.mLine + 1) + "\n";
									int theLine = addrInfo.mLine - 10;	// MPi: TODO: Tweak the - 10 based on the display height?
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
											gotText += "=>";
										}
										else
										{
											gotText += "  ";
										}

										gotText += mSourceFiles[addrInfo.mFile][theLine++];
										gotText += "\n";
									}
								}
								catch (System.Exception)
								{
									SendCommand("m 0000 ffff");
									theReply = GetReply();
									// No source info, so just dump memory
									gotText += theReply;
									//>C:0000  2f 37 00 aa  b1 91 b3 22  00 00 00 4c  00 00 00 04   /7....."...L....
									//>C:0010  00 00 00 00  00 04 19 16  00 0a 76 a3  00 00 00 00   ..........v.....
									//...
									//>C:ff00  00 00 00 00  00 04 19 16  00 0a 76 a3  00 00 00 00   ..........v.....					
								}
							}
							else if (lastCommand.IndexOf("!cls") == 0)
							{
								gotText = "";
								this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new OneArgDelegate(SetSourceView), gotText);
							}
							else if (lastCommand.IndexOf("!d") == 0)
							{
								SendCommand("r");
								string theReply = GetReply();

								ParseRegisters(theReply);
								UpdateLabels();

								gotText = "";

								try
								{
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
									// MPi: TODO: Tweak the 20 range based on the display height?
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

									string command = "d " + startPrev.ToString("X") + " " + mPC.ToString("X");
									SendCommand(command);
									string disassemblyBefore = GetReply();
									command = "d " + mPC.ToString("X") + " " + endNext.ToString("X");
									SendCommand(command);
									string disassemblyAfter = GetReply();

									string[] split = disassemblyBefore.Split('\n');
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
											split = disassemblyAfter.Split('\n');
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
										line += "\r";

										gotText += line;
									}

								}
								catch (System.Exception)
								{
									SendCommand("m 0000 ffff");
									theReply = GetReply();
									// No source info, so just dump memory
									gotText += theReply;
									//>C:0000  2f 37 00 aa  b1 91 b3 22  00 00 00 4c  00 00 00 04   /7....."...L....
									//>C:0010  00 00 00 00  00 04 19 16  00 0a 76 a3  00 00 00 00   ..........v.....
									//...
									//>C:ff00  00 00 00 00  00 04 19 16  00 0a 76 a3  00 00 00 00   ..........v.....					
								}
							}
							else
							{
								// Any other commands get here
								bool silent = false;
								if (lastCommand.IndexOf('!') == 0)
								{
									lastCommand = lastCommand.Substring(1);
									silent = true;
								}

								SendCommand(lastCommand);
								string theReply = "";
								if (lastCommand.IndexOf("x") != 0)
								{
									theReply = GetReply();
									gotText += theReply;
								}
								if (silent)
								{
									gotText = "";
								}
							}

							if (gotText.Length > 0)
							{
								gotText = gotText.Replace("\n", "\r");
								this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new OneArgDelegate(SetSourceView), gotText);
							}
						}
						else
						{
							Thread.Sleep(100);
							if (mSocket.Available > 0)
							{
								// This happens if a break/watch point is hit, then a reply is received without any command being sent
								string theReply = GetReply();
								gotText += theReply;
								this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new OneArgDelegate(SetSourceView), gotText);
								mCommands.Add("r");
								this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new NoArgDelegate(HandleCodeView));
							}
						}
					} //< while (mSocket.Connected)

					if (wasConnected)
					{
						// Only if it was connected the dispose and try again
						if (mSocket != null)
						{
							mSocket.Dispose();
							mSocket = null;
							this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new OneArgDelegate(SetSourceView), "Not connected");
						}
					}

					Thread.Sleep(250);
				}
				catch (System.Exception /*ex*/)
				{
					this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new OneArgDelegate(SetSourceView), "Exception. Not connected");
					if (mSocket != null)
					{
						mSocket.Dispose();
						mSocket = null;
					}
					Thread.Sleep(250);
				}
			}

		}

		private void commandBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				string command = mCommandBox.Text;
				mCommands.Add(command);
				mCommandBox.Clear();
			}
		}

		private void HandleCheckBoxes()
		{
			mUsedLabels = (mCheckUsedLabels.IsChecked == true);
			mAccessUsed = (mCheckAccessUse.IsChecked == true);
			mExecUsed = (mCheckExecUse.IsChecked == true);
		}
		private void HandleCodeView()
		{
			HandleCheckBoxes();
			mCommands.Add("!r");
			if (mDoSource.IsChecked == true && mDoDisassembly.IsChecked == true )
			{
				mCommands.Add("!sm");
			}
			else if (mDoSource.IsChecked == true && mDoDisassembly.IsChecked == false)
			{
				mCommands.Add("!s");
			}
			else if (mDoSource.IsChecked == false && mDoDisassembly.IsChecked == true)
			{
				mCommands.Add("!d");
			}
			else if (mDoSource.IsChecked == false && mDoDisassembly.IsChecked == false)
			{
				mCommands.Add("!cls");
			}
		}

		private void Button_Click_Break(object sender, RoutedEventArgs e)
		{
			HandleCodeView();
		}

		private void Button_Click_Go(object sender, RoutedEventArgs e)
		{
			mCommands.Add("x");
		}

		private void Button_Click_StepIn(object sender, RoutedEventArgs e)
		{
			mCommands.Add("!z");
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
			mCommands.Add("!n");
			HandleCodeView();
		}

		private void Button_Click_StepOut(object sender, RoutedEventArgs e)
		{
			mCommands.Add("!ret");
			HandleCodeView();
		}

		private void Button_Click_ProfileClear(object sender, RoutedEventArgs e)
		{
			mAccessedCount.Clear();
			mExecutedCount.Clear();
			mCommands.Add("!memmapzap");
		}

		private void Button_Click_ProfileAdd(object sender, RoutedEventArgs e)
		{
			mCommands.Add("!memmapshow");
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

	}
}
