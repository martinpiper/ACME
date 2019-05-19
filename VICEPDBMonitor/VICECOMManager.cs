using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace VICEPDBMonitor
{
    public class CommandStruct
    {
        public enum eMode
        {
             DoCommandThrowAwayResults = 0
            ,DoCommandReturnResults = 1
            ,DoCommandThenExit = 2
            ,DoCommandOnly = 3
            ,DoCommandFireCallback = 4
        }
        public delegate void CS_TextDelegate(string reply, object userData);
        public delegate void CS_BinaryDelegate(byte[] reply, object userData);
        public string command;
        public eMode mode;
        public bool binary;
        public byte[] binaryParams;
        public CS_TextDelegate textDelegate;
        public CS_BinaryDelegate binaryDelegate;
        public object userData;
        public Dispatcher dispatch;
    }

    class VICECOMManager
    {
        public delegate void NoArgDelegate();
        public delegate void OneArgDelegate(String arg);

        static VICECOMManager g_vicecommanager = null;

        List<CommandStruct> mCommands = new List<CommandStruct>();

        Socket mSocket;
        String mGotTextWorking = "";
        OneArgDelegate mErrorCallback;
        OneArgDelegate mVICEMsg;
        Dispatcher mErrorDispatcher;

        public static VICECOMManager getVICEComManager()
        {
            if( g_vicecommanager == null)
            {
                g_vicecommanager = new VICECOMManager();
            }
            return g_vicecommanager;
        }

        public void setErrorCallback(OneArgDelegate del, Dispatcher dispat) { mErrorCallback = del; mErrorDispatcher = dispat; }
        public void setVICEmsgCallback(OneArgDelegate del) { mVICEMsg = del;  }

        public void addTextCommand(string command, CommandStruct.eMode mode, CommandStruct.CS_TextDelegate callback,object userData, Dispatcher dispatch)
        {
            CommandStruct cs = new CommandStruct
            {
                binary = false,
                mode = mode,
                textDelegate = callback,
                command = command,
                userData = userData,
                dispatch = dispatch
            };
            mCommands.Add(cs);
        }

        public void addBinaryMemCommand(int start, int end, CommandStruct.CS_BinaryDelegate callback, object userData, Dispatcher dispatch)
        {
			// https://sourceforge.net/p/vice-emu/code/HEAD/tree/trunk/vice/src/monitor/monitor_network.c#l267
			
			byte[] sendCommand = new byte[5];

            sendCommand[0] = 0x1; // mem dump
            sendCommand[1] = (byte)(start & 255);
            sendCommand[2] = (byte)((start >> 8) & 255);
            sendCommand[3] = (byte)(end & 255);
            sendCommand[4] = (byte)((end >> 8) & 255);

            CommandStruct cs = new CommandStruct
            {
                binary = true,
                mode = CommandStruct.eMode.DoCommandReturnResults,
                binaryDelegate = callback,
                binaryParams = sendCommand,
                userData = userData,
                dispatch = dispatch
            };
            mCommands.Add(cs);
        }

        private VICECOMManager()
        {
            NoArgDelegate fetcher = new NoArgDelegate(this.BackgroundThread);
            fetcher.BeginInvoke(null, null);
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

                    CommandStruct lastCommand = null;

                    while (mSocket.Connected)
                    {
                        if (mSocket.Poll(0, SelectMode.SelectError))
                        {
                            break;
                        }
                        wasConnected = true;
                        if (mCommands.Count > 0)
                        {
							ConsumeData();
                            lastCommand = mCommands[0];
                            mCommands.RemoveAt(0);

                            if(lastCommand.binary == true)
                            {
                                // do binary here
                                byte[] response = sendBinaryCommand(lastCommand.binaryParams);
                                lastCommand.dispatch.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal,lastCommand.binaryDelegate, response, lastCommand.userData);
                            }
                            else
                            { // string based
                                SendCommand(lastCommand.command);
                                switch(lastCommand.mode)
                                {
                                    default:
                                    case CommandStruct.eMode.DoCommandThrowAwayResults:
                                        GetReply();
                                        if(lastCommand.textDelegate != null)
                                        {
                                            lastCommand.dispatch.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, lastCommand.textDelegate, null, lastCommand.userData);
                                        }
                                        break;
                                    case CommandStruct.eMode.DoCommandReturnResults:
                                        string reply = GetReply();                                        
                                        lastCommand.dispatch.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, lastCommand.textDelegate, reply, lastCommand.userData);
                                        break;
                                    case CommandStruct.eMode.DoCommandThenExit:
                                        GetReply();
                                        SendCommand("x");
                                        break;
                                    case CommandStruct.eMode.DoCommandOnly:
                                        ConsumeData(); 
                                        break; //don't a single thing
                                    case CommandStruct.eMode.DoCommandFireCallback:
                                        if (lastCommand.textDelegate != null)
                                        {
                                            lastCommand.dispatch.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, lastCommand.textDelegate, null, lastCommand.userData);
                                        }
                                        break;
                                }
                            }

                            /*

                            if (gotText.Length > 0)
                            {
                                gotText = gotText.Replace("\n", "\r");
                                this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new OneArgDelegate(SetSourceView), gotText);
                            }*/
                        }
                        else
                        {
                            Thread.Sleep(100);
                            if (mSocket.Available > 0)
                            {
                                //mNeedNewMemoryDump = true;
                                // This happens if a break/watch point is hit, then a reply is received without any command being sent
                                string theReply = GetReply();
                                //gotText += theReply;
                                //								this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new OneArgDelegate(SetSourceView), gotText);
                                mErrorDispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, mVICEMsg,theReply);
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
                            mErrorDispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, mErrorCallback, "Not connected");
                        }
                    }

                    Thread.Sleep(250);
                }
                catch (System.Exception /*ex*/)
                {
                    mErrorDispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, mErrorCallback, "Exception. Not connected");
                    if (mSocket != null)
                    {
                        mSocket.Dispose();
                        mSocket = null;
                    }
                    Thread.Sleep(250);
                }
            }

        }

        private void SendCommand(string command)
        {
            if (command.Length > 0)
            {
                // Add padding to avoid the VICE monitor command truncation bug
                command += "                                                                           \n";
                byte[] msg = Encoding.ASCII.GetBytes(command);
                ConsumeData();
                SendBytes(msg);
            }
        }

        private void SendBytes(byte[] msg)
        {
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

        public string GetReply(bool canBeEmptyReply = false)
        {
			mGotTextWorking = "";
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
                catch (System.Exception)
                {
                    //					this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new OneArgDelegate(UpdateUserInterface), "Connected exception: " + ex.ToString());
                    Thread.Sleep(10);
                }

                int foundFirstPos = mGotTextWorking.IndexOf("(C:$");
                if (foundFirstPos >= 0 && mGotTextWorking.Length >= 9)
                {
                    int foundThirdPos = mGotTextWorking.IndexOf(") ", foundFirstPos);
                    if (foundThirdPos > foundFirstPos)
                    {
						theReply = mGotTextWorking.Substring(0, foundFirstPos);
                        // Start the next command buffer with valid text
						mGotTextWorking = mGotTextWorking.Substring(foundThirdPos + 2);
						mGotTextWorking.Trim();
                        theReply = theReply.Replace("\n", "\r");
						theReply.Trim();
						if (theReply.Length == 0 && mGotTextWorking.Length > 0)
						{
							continue;
						}
                        return theReply;
                    }
                }
            }

            return "";
        }

        private void ConsumeData()
        {
            // Try to consume anything before sending commands...
            while (mSocket.Available > 0)
            {
                byte[] bytes = new byte[500000];
                mSocket.Receive(bytes);
            }
			mGotTextWorking = "";
        }

        private byte[] sendBinaryCommand(byte[] binaryParams)
        {
            byte[] sendCommand = new byte[binaryParams.Length+3];
            sendCommand[0] = 0x2;
            sendCommand[1] = (byte)binaryParams.Length;
            int j = 0;
            for (; j < binaryParams.Length; ++j)
            {
                sendCommand[2 + j] = binaryParams[j]; // this will be a handful of bytes mostly, actuall eventually it won't be but for now ;)
                //well it can't be over 254 for starters ;)
            }
            sendCommand[j] = 0;

            SendBytes(sendCommand);
        

            Thread.Sleep(10);  //wait for response
            const int kBufferSize = 64 * 1024;
            byte[] buffer = new byte[kBufferSize];

            int actual = 0;
            while (mSocket.Connected && actual < 6)
            {
                try
                {
                    actual += mSocket.Receive(buffer, actual, kBufferSize - actual, SocketFlags.None);
                }
                catch (System.Exception ex)
                {
                    //this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new OneArgDelegate(UpdateUserInterface), "Connected exception: " + ex.ToString());
                    Thread.Sleep(100);
                }
            }

            if (buffer[0] != 0x2)
            {
                return new byte[0]; //return an empty array
            }

            int responseLength = buffer[1] + (buffer[2] << 8) + (buffer[3] << 16) + (buffer[4] << 24);

            if (buffer[5] != 0)
            {
                return new byte[0]; //return an empty array
            }

            if (responseLength > kBufferSize - 5)
            {
                return new byte[0]; // to much data
            }

            byte[] responseBuffer = new byte[responseLength];

            int counter = 0;
            for (int i = 6; i < actual && i > 0; i++, counter++)
            {
                responseBuffer[counter] = buffer[i];
            }

            while (counter < responseLength)
            {
                if (mSocket.Connected)
                {
                    int tooRead = mSocket.Available;
                    while (tooRead > 0)
                    {
                        actual += mSocket.Receive(responseBuffer, counter, responseLength - counter, SocketFlags.None);

                        counter += actual;
                        tooRead -= actual;
                    }
                }
                Thread.Sleep(100);
            }
            return responseBuffer;
        }

    }
}
