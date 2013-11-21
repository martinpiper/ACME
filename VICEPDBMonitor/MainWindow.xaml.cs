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
		List<string> sourceFileNames = new List<string>();
		List<string> sourceFileNamesFound = new List<string>();
		List<List<string>> sourceFiles = new List<List<string>>();
		Dictionary<int, AddrInfo> addrInfoByAddr = new Dictionary<int, AddrInfo>();
		MultiMap<int, LabelInfo> labelInfoByAddr = new MultiMap<int, LabelInfo>();
		MultiMap<int, LabelInfo> labelInfoByZone = new MultiMap<int, LabelInfo>();
		MultiMap<string, LabelInfo> labelInfoByLabel = new MultiMap<string, LabelInfo>();

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
						sourceFileNames.Capacity = lines;
						while (lines-- > 0)
						{
							line = file.ReadLine();
							string[] tokens = line.Split(':');
							sourceFileNames.Add(tokens[1]);
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
							addrInfoByAddr.Add(addrInfo.mAddr, addrInfo);
							if (prevAddr >= 0)
							{
								addrInfoByAddr[prevAddr].mNextAddr = addrInfo.mAddr;
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
							labelInfoByAddr.Add(labelInfo.mAddr, labelInfo);
							labelInfoByZone.Add(labelInfo.mZone, labelInfo);
							labelInfoByLabel.Add(labelInfo.mLabel, labelInfo);
						}
					}
				}

				file.Close();
			}

			foreach (string name in sourceFileNames)
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
					sourceFiles.Add(aFile);
					sourceFileNamesFound.Add(newPath);
				}
				catch (System.Exception ex)
				{
					sourceFiles.Add(new List<string>());
					sourceFileNamesFound.Add("");
				}
			}

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

		int mPC = 0;
		int mRegA = 0;
		int mRegX = 0;
		int mRegY = 0;
		int mSP = 0;
		int mNV_BDIZC = 0;

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
				byte[] msg = Encoding.ASCII.GetBytes("r										 \n");
				int ret = sock.Send(msg);
			}
			if (!sentMem && gotReg)
			{
				sentMem = true;
				// NOTE: Adding spaces for monitor bug workaround
				byte[] msg = Encoding.ASCII.GetBytes("m 0000 ffff							   \n");
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
				if (gotText.Length > 16)
				{
					// Look for the second line starting with this, which signifies the command was done.
					int foundFirstPos = gotText.IndexOf("(C:$");
					int foundPos = gotText.IndexOf("(C:$", 12);
					if ((foundFirstPos >= 0) && (foundPos > 0) && (foundPos > (gotText.Length - 20)))
					{
						foundFirstPos += 10;	// End of the first "(C:$"
						if (sentReg && !gotReg)
						{
							gotTextWork = gotText.Substring(foundFirstPos, foundPos - foundFirstPos);
							//  ADDR AC XR YR SP 00 01 NV-BDIZC LIN CYC  STOPWATCH
							//.;0427 ad 00 00 f4 2f 37 10100100 000 000   87547824
							string parse = gotTextWork.Substring(gotTextWork.IndexOf(".;"));
							string[] parse2 = parse.Split(' ');
							mPC = int.Parse(parse2[0].Substring(2) , NumberStyles.HexNumber);
							mRegA = int.Parse(parse2[1] , NumberStyles.HexNumber);
							mRegX = int.Parse(parse2[2], NumberStyles.HexNumber);
							mRegY = int.Parse(parse2[3], NumberStyles.HexNumber);
							mSP = int.Parse(parse2[4], NumberStyles.HexNumber);
//							mNV_BDIZC = int.Parse(parse2[7], NumberStyles.Binary); // TODO Binary

							gotReg = true;
							gotText = gotText.Substring(foundPos);
						}
						if (sentMem)
						{
							try
							{
								AddrInfo addrInfo = addrInfoByAddr[mPC];
								int range = 20;
								int startPrev = mPC;
								// Step backwards trying to find a good starting point to disassemble
								while (range-- > 0)
								{
									AddrInfo addrInfo2 = addrInfoByAddr[startPrev];
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
									AddrInfo addrInfo2 = addrInfoByAddr[endNext];
									if (addrInfo2.mNextAddr < 0)
									{
										break;
									}
									endNext = addrInfo2.mNextAddr;
								}

								gotTextWork += "File:" + sourceFileNames[addrInfo.mFile] + "\n";
								gotTextWork += "Line:" + (addrInfo.mLine+1) + "\n";
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
										gotTextWork += "=>";
									}
									else
									{
										gotTextWork += "  ";
									}
									gotTextWork += sourceFiles[addrInfo.mFile][theLine++];
									gotTextWork += "\n";
								}
							}
							catch (System.Exception ex2)
							{
								// No source info, so just dump memory
								gotTextWork += gotText.Substring(foundFirstPos, foundPos - foundFirstPos);
								//>C:0000  2f 37 00 aa  b1 91 b3 22  00 00 00 4c  00 00 00 04   /7....."...L....
								//>C:0010  00 00 00 00  00 04 19 16  00 0a 76 a3  00 00 00 00   ..........v.....
								//...
								//>C:ff00  00 00 00 00  00 04 19 16  00 0a 76 a3  00 00 00 00   ..........v.....					
							}

							textBox.Text = gotTextWork;
							sentExit = true;
							byte[] msg = Encoding.ASCII.GetBytes("x										 \n");
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

		private void commandBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				string command = commandBox.Text;
			}
		}
	}

}
