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
		List<string> mSourceFileNames = new List<string>();
		List<string> mSourceFileNamesFound = new List<string>();
		List<List<string>> mSourceFiles = new List<List<string>>();
		Dictionary<int, AddrInfo> mAddrInfoByAddr = new Dictionary<int, AddrInfo>();
		MultiMap<int, LabelInfo> mLabelInfoByAddr = new MultiMap<int, LabelInfo>();
		MultiMap<int, LabelInfo> mLabelInfoByZone = new MultiMap<int, LabelInfo>();
		MultiMap<string, LabelInfo> mLabelInfoByLabel = new MultiMap<string, LabelInfo>();

		private delegate void NoArgDelegate();
		private delegate void OneArgDelegate(String arg);

		public MainWindow()
		{
			InitializeComponent();

			string[] commandLineArgs = Environment.GetCommandLineArgs();
			string line;
			int prevAddr = -1;

			// Read the file and display it line by line.
			using (System.IO.StreamReader file = new System.IO.StreamReader(commandLineArgs[1]))
			{
				while ((line = file.ReadLine()) != null)
				{
					if (line.IndexOf("FILES:") == 0)
					{
						int lines = int.Parse(line.Substring(6));
						mSourceFileNames.Capacity = lines;
						while (lines-- > 0)
						{
							line = file.ReadLine();
							string[] tokens = line.Split(':');
							mSourceFileNames.Add(tokens[1]);
						}
					}
					else if (line.IndexOf("ADDRS:") == 0)
					{
						int lines = int.Parse(line.Substring(6));
						while (lines-- > 0)
						{
							line = file.ReadLine();
							string[] tokens = line.Split(':');
							AddrInfo addrInfo = new AddrInfo();
							addrInfo.mAddr = int.Parse(tokens[0].Substring(1), NumberStyles.HexNumber);
							addrInfo.mPrevAddr = prevAddr;
							addrInfo.mZone = int.Parse(tokens[1]);
							addrInfo.mFile = int.Parse(tokens[2]);
							addrInfo.mLine = int.Parse(tokens[3]) - 1;	// Files lines are 1 based in the debug file
							mAddrInfoByAddr.Add(addrInfo.mAddr, addrInfo);
							if (prevAddr >= 0)
							{
								mAddrInfoByAddr[prevAddr].mNextAddr = addrInfo.mAddr;
							}
							prevAddr = addrInfo.mAddr;
						}
					}
					else if (line.IndexOf("LABELS:") == 0)
					{
						int lines = int.Parse(line.Substring(7));
						while (lines-- > 0)
						{
							line = file.ReadLine();
							string[] tokens = line.Split(':');
							LabelInfo labelInfo = new LabelInfo();
							labelInfo.mAddr = int.Parse(tokens[0].Substring(1), NumberStyles.HexNumber);
							labelInfo.mZone = int.Parse(tokens[1]);
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

			foreach (string name in mSourceFileNames)
			{
				try
				{
					List<string> aFile = new List<string>();
					string newPath = name;
					if (!System.IO.File.Exists(newPath))
					{
						newPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(commandLineArgs[1]), name);
						if (!System.IO.File.Exists(newPath))
						{
							newPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(commandLineArgs[1]), System.IO.Path.GetFileName(name));
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
				catch (System.Exception ex)
				{
					mSourceFiles.Add(new List<string>());
					mSourceFileNamesFound.Add("");
				}
			}

			mCommands.Add("r");
			mCommands.Add("m 0000 ffff");
			mCommands.Add("x");

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

		private void UpdateUserInterface(String text)
		{
			mTextBox.Text = text;
		}

		private void BackgroundThread()
		{
			Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			String gotText = "";
			String gotTextWorking = "";
			socket.Blocking = false;
			try
			{
				socket.Connect("localhost", 6510);
			}
			catch (System.Exception /*ex*/)
			{
			}

			bool pendingCommand = false;
			string lastCommand = "";

			while (socket.Connected)
			{
				if (!pendingCommand && (mCommands.Count > 0))
				{
					lastCommand = mCommands[0];
					mCommands.RemoveAt(0);
					// Add padding to avoid the VICE monitor command truncation bug
					lastCommand += "                                                                           \n";
					byte[] msg = Encoding.ASCII.GetBytes(lastCommand);
					int sent = 0;
					while (sent < msg.Length)
					{
						int ret = socket.Send(msg, sent , msg.Length - sent, SocketFlags.None);
						if (ret > 0)
						{
							sent += ret;
						}
						else
						{
							Thread.Sleep(10);
						}
					}
					// If it's not "x" then pend for the reply
					if (lastCommand.IndexOf("x") != 0)
					{
						pendingCommand = true;
					}
				}
				byte[] bytes = new byte[500000];
				try
				{
					int got = socket.Receive(bytes);
					if (got > 0)
					{
						gotTextWorking += Encoding.ASCII.GetString(bytes, 0, got);
					}
				}
				catch (System.Exception ex)
				{
//					this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new OneArgDelegate(UpdateUserInterface), "Connected exception: " + ex.ToString());
					Thread.Sleep(100);
				}

				int foundFirstPos = gotTextWorking.IndexOf("(C:$");
				if (foundFirstPos >= 0 && gotTextWorking.Length > 16)
				{
					int foundSecondPos = gotTextWorking.IndexOf("(C:$", foundFirstPos + 10 + 4);
					if (foundSecondPos > foundFirstPos)
					{
						int foundThirdPos = gotTextWorking.IndexOf(") ", foundSecondPos + 4);
						if (foundThirdPos > foundSecondPos)
						{
							foundFirstPos += 10;	// End of the first "(C:$"
							string theReply = gotTextWorking.Substring(foundFirstPos , foundSecondPos - foundFirstPos);
							// Start the next command buffer with valid text
							gotTextWorking = gotTextWorking.Substring(foundSecondPos);

							// Process the reply
							if (lastCommand.IndexOf("r") == 0)
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
//								mNV_BDIZC = int.Parse(parse2[7], NumberStyles.Binary); // TODO Binary
								gotText = theReply;

								try
								{
									AddrInfo addrInfo = mAddrInfoByAddr[mPC];
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

									gotText += "File:" + mSourceFileNames[addrInfo.mFile] + "\n";
									gotText += "Line:" + (addrInfo.mLine + 1) + "\n";
									int theLine = addrInfo.mLine - 10;	// MPi: TODO: Tweak the - 10 based on the display height?
									if (theLine < 0)
									{
										theLine = 0;
									}
									int toDisplay = 20;
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
								catch (System.Exception )
								{
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
								gotText += theReply;
							}
							pendingCommand = false;
							this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new OneArgDelegate(UpdateUserInterface), gotText);
						}
					}
				}

			}

			this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new OneArgDelegate(UpdateUserInterface), "Not connected");
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
	}

}
