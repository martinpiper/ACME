using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Documents;
using Newtonsoft.Json;
using System.IO;
using System.Globalization;

namespace VICEPDBMonitor
{
    class FunctionData
    {
        public int StartAddr { get; set; }
        public int EndAddr { get; set; }
        public List<KeyValuePair<int, List<string>>> AddrsToCodeList { get; set; }
        public List<int> ReadFrom { get; set; }
        public List<int> WriteTo { get; set; }
        public List<int> ReadWrite { get; set; }
        public Dictionary<string, string> LocalToFullCodeLabels { get; set; }
    }

    class AssertData
    {
        public int Num { get; set; }
        public int Address { get; set; }
        public string Label { get; set; }
        public string Condition { get; set; }
        public string Msg { get; set; }
    }

    class JsonPDB
    {
        public List<FunctionData> funcData;
        public Dictionary<int, string> VariableLabels;
        public Dictionary<int, string> CodeLabels;
        public List<AssertData> AssertInfo;
    }

    class FunctionJSONRAndD : IPDBReaderAndDisplay
    {
        IRegisterSet m_registerSet;
        IMemDump m_memDump;
        RichTextBox m_textBox;
        TextBox m_labelsBox;
        JsonPDB m_pDBData;
        Dictionary<string, int> m_allLabelsToAddr;

        FunctionData m_lastFunction;
        string m_srcString;

        public void AppendTextSouceView(string text, Brush brush)
        {
            if (null == text || text.Length == 0)
            {
                return;
            }

            Run r = new Run("", m_textBox.CaretPosition.DocumentEnd);
            r.Background = brush;

            m_textBox.AppendText(text);

            r = new Run("", m_textBox.CaretPosition.DocumentEnd);
            r.Background = null;
        }

        public void CreatePDBFromARGS(string[] commandLineArgs, MainWindow window)
        {
            m_pDBData = JsonConvert.DeserializeObject<JsonPDB>(File.ReadAllText(commandLineArgs[1]));
            m_lastFunction = null;
            m_srcString = String.Empty;
            m_allLabelsToAddr = new Dictionary<string, int>(m_pDBData.VariableLabels.Count + m_pDBData.CodeLabels.Count);
            foreach(int addr in m_pDBData.VariableLabels.Keys)
            {
                string label = m_pDBData.VariableLabels[addr];
                m_allLabelsToAddr[label] = addr;
            }
            foreach (int addr in m_pDBData.CodeLabels.Keys)
            {
                string label = m_pDBData.CodeLabels[addr];
                m_allLabelsToAddr[label] = addr;
            }
            int num = 1;
            foreach(AssertData AD in m_pDBData.AssertInfo)
            {
                AssertDataSource ADS = new AssertDataSource();
                ADS.Enable = true;
                ADS.Address = AD.Address;
                ADS.Label = AD.Label;
                ADS.Condition = AD.Condition;
                ADS.Msg = AD.Msg;
                ADS.Number = num++;
                window.AddAssert(ADS);
            }
        }

        public string PostEnterKeyForCommand(string command)
        {
            string[] parts = command.Split(' ');
            int i = 1; //the first part is the command so don't look it up
            command = parts[0];
            while(i < parts.Length)
            {
                if(parts[i].StartsWith("_"))
                {
                    if(m_lastFunction != null)
                    {
                        if(m_lastFunction.LocalToFullCodeLabels.ContainsKey(parts[i]))
                        {
                            parts[i] = m_lastFunction.LocalToFullCodeLabels[parts[i]];
                        }
                    }
                }
                if(m_allLabelsToAddr.ContainsKey(parts[i]))
                {
                    parts[i] = "$" + m_allLabelsToAddr[parts[i]].ToString("X");
                }
                command += " " + parts[i];
                ++i;                
            }
            
            /*
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
                int found = -1;
                string label = null;
                if (remaining.Length > 0)
                {
                    foreach (int addr in m_pDBData.VariableLabels.Keys)
                    {
                        label = m_pDBData.VariableLabels[addr];
                        if (label.Length >= remaining.Length && remaining.StartsWith(label))
                        {
                            found = addr;
                            break;
                        }
                    }
                    if(found < 0)
                    {
                        if (remaining.StartsWith("_"))
                        {
                            foreach (string full in m_lastFunction.LocalToFullCodeLabels.Keys)
                            {
                                label = m_pDBData.CodeLabels[addr];
                                if (label.Length >= remaining.Length && remaining.StartsWith(label))
                                {
                                    found = addr;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            foreach (int addr in m_pDBData.CodeLabels.Keys)
                            {
                                label = m_pDBData.CodeLabels[addr];
                                if (label.Length >= remaining.Length && remaining.StartsWith(label))
                                {
                                    found = addr;
                                    break;
                                }
                            }
                        }
                    }
                }

                // If it's found then reconstruct the command with the label replaced as a hex number
                if (-1 != found)
                {
                    string theHex = "$" + found.ToString("X");
                    command = command.Substring(0, pos) + theHex + remaining.Substring(m_pDBData.VariableLabels[found].Length);
                    pos += theHex.Length;
                }
                else
                {
                    pos++;
                }
            }*/

            return command;
        }

        public void SetCodeWindowControl(Control textControl)
        {
            m_textBox = textControl as RichTextBox;
        }

        public void SetLabelsWindowControl(Control textControl)
        {
            m_labelsBox = textControl as TextBox;
        }

        public void SetLabelView(string text)
        {
            m_labelsBox.Text = text;
        }

        public void SetMemDump(IMemDump memDump)
        {
            m_memDump = memDump;
        }

        public void SetRegisterSet(IRegisterSet registerSet)
        {
            m_registerSet = registerSet;
        }

        public void SetSouceView(string text)
        {
            //text = EnrichDumpWithMemory(text);
            m_textBox.BeginChange();
            m_textBox.Document.Blocks.Clear();
            if (null == text || text.Length == 0)
            {
                m_textBox.EndChange();
                return;
            }
            try
            {
                int split = text.IndexOf("=>");
                if (split < 0)
                {
                    split = text.IndexOf(">>>>");
                }
                if (split < 0)
                {
                    m_textBox.AppendText(text);
                    m_textBox.EndChange();
                }
                else
                {
                    m_textBox.EndChange();
                    string before = text.Substring(0, split);
                    int endOfLine = text.IndexOf('\r', split);
                    string currLine = text.Substring(split, endOfLine - split);
                    string after = text.Substring(endOfLine);
                    Run topRun = new Run(before)
                    {
                        Background = m_textBox.Background
                    };
                    Run currRun = new Run(currLine)
                    {
                        Background = Brushes.LightGray
                    };
                    Run afterRun = new Run(after)
                    {
                        Background = m_textBox.Background
                    };
                    Paragraph para = new Paragraph();
                    para.Inlines.Add(topRun);
                    para.Inlines.Add(currRun);
                    para.Inlines.Add(afterRun);
                    FlowDocument flow = new FlowDocument(para);
                    m_textBox.Document = flow;
                    TextPointer tp = currRun.ContentStart;
                    System.Windows.Rect BottomRect = flow.ContentEnd.GetCharacterRect(LogicalDirection.Backward);
                    m_textBox.CaretPosition = tp;
                    System.Windows.Rect letter = m_textBox.CaretPosition.GetCharacterRect(LogicalDirection.Forward);
                    double Y = letter.Top;
                    double Bottom = BottomRect.Bottom;
                    double Height = m_textBox.ActualHeight;
                    if (Y < Height) Y = 0.0;
                    else if (Y > Bottom - Height) Y = Bottom - Height;
                    else Y -= Height / 2.0;
                    m_textBox.ScrollToVerticalOffset(Y);
                }
            }
            catch (System.Exception)
            {
            }
        }

        public string ShowSrcDissGetPostDissaem(string reply, object userData)
        {
            int PC = m_registerSet.GetPC();
            FunctionData currFunc = getFuncForPC();
            if (currFunc != null)
            {
                ShowSrcDissStruct sms = userData as ShowSrcDissStruct;
                sms.disassemblyAfter = reply;
                string display = String.Empty;
                Dictionary<int, List<string>> dissAddrToLines;
                int pad = 16;
                try
                {
                    string allDissasembly = sms.disassemblyBefore + "\r" + sms.disassemblyAfter;
                    string[] dissLines = allDissasembly.Split('\r');
                    dissAddrToLines = new Dictionary<int, List<string>>(dissLines.Length);
                    int index = 0;
                    while(index < dissLines.Length)
                    {
                        string line = dissLines[index++];
                        //valid .C:XXXX line?
                        if (line.Length < 7)
                        {
                            continue;
                        }
                        if (!line.StartsWith(".C:"))
                        {
                            continue;
                        }
                        //yes
                        string tAddr = line.Substring(3);
                        tAddr = tAddr.Substring(0, 4);
                        int theAddr = int.Parse(tAddr, NumberStyles.HexNumber);
                        if(dissAddrToLines.ContainsKey(theAddr))
                        {
                            dissAddrToLines[theAddr].Add(line);
                        }
                        else
                        {
                            List<string> temp = new List<string>(4);
                            temp.Add(line);
                            dissAddrToLines[theAddr] = temp;
                        }
                        if(line.Length > pad)
                        {
                            pad = line.Length+4;
                        }
                    }
                    //so now I have a dictionary of address to dissasembly lines
                    foreach (KeyValuePair<int, List<string>> item in currFunc.AddrsToCodeList)
                    {
                        if (item.Key == PC)
                        {
                            int count = item.Value.Count;
                            int dissasemblyCount = 0;
                            if( dissAddrToLines.ContainsKey(item.Key))
                            {
                                dissasemblyCount = dissAddrToLines[item.Key].Count;
                            }
                            for (int i = 0; i < count; ++i)
                            {
                                string line = String.Empty;
                                if (i == count - 1)
                                {
                                    line += "=>";
                                }
                                else
                                {
                                    line += "  ";
                                }
                                if (i < dissasemblyCount)
                                {
                                    line += dissAddrToLines[item.Key][i];
                                }
                                line = line.PadRight(pad, ' ');
                                line += item.Value[i].Replace('\n', '\r');
                                display += line + "\r";
                            }
                        }
                        else
                        {
                            int dissasemblyCount = 0;
                            if (dissAddrToLines.ContainsKey(item.Key))
                            {
                                dissasemblyCount = dissAddrToLines[item.Key].Count;
                            }
                            int count = item.Value.Count;
                            for (int i = 0; i < count; ++i)
                            {
                                string line = "  ";                                
                                if (i < dissasemblyCount)
                                {
                                    line += dissAddrToLines[item.Key][i];
                                }
                                line = line.PadRight(pad, ' ');
                                line += item.Value[i].Replace('\n', '\r');
                                display += line + "\r";
                            }
                        }
                    }
                    return display;
                }
                catch(Exception)
                {

                }
            }
            return "No source to show";
        }

        public string ShowSrcPostRegisters()
        {
            int PC = m_registerSet.GetPC();
            FunctionData currFunc = getFuncForPC();
            if (currFunc != null)
            {
                string display = string.Empty;
                foreach (KeyValuePair<int, List<string>> item in currFunc.AddrsToCodeList)
                {
                    if (item.Key == PC)
                    {
                        int count = item.Value.Count;
                        for (int i = 0; i < count; ++i)
                        {
                            if (i == count - 1)
                            {
                                display += "=>";
                            }
                            else
                            {
                                display += "  ";
                            }
                            display += item.Value[i].Replace('\n', '\r');
                            display += "\r";
                        }
                    }
                    else
                    {
                        foreach (string line in item.Value)
                        {
                            display += "  " + line.Replace('\n', '\r');
                            display += "\r";
                        }
                    }
                }
                m_lastFunction = currFunc;
                m_srcString = display + "\r";
                return m_srcString;
            }
            else
            {
                return String.Format("Func for PC {0:X4} not found.", PC);
            }
        }

        public ShowSrcDissStruct show_diss_common()
        {
            ShowSrcDissStruct sms;
            FunctionData func = getFuncForPC();
            if (func != null)
            {
                sms = new ShowSrcDissStruct()
                {
                    endNext = func.EndAddr,
                    startPrev = func.StartAddr
                };
            }
            else
            {
                sms = new ShowSrcDissStruct()
                {
                    endNext = m_registerSet.GetPC() + 0x40,
                    startPrev = m_registerSet.GetPC()
                };
            }
            return sms;
        }

        public string UpdateLabels(bool usedLabels, bool execUsed, bool accessUsed, Dictionary<int, int> executedCount, Dictionary<int, int> accessedCount)
        {
            if(m_lastFunction != null)
            {
                string labels = "Vars:\r";
                foreach(int addr in m_lastFunction.ReadWrite)
                {
                    string label = "unknown";
                    if(m_pDBData.VariableLabels.ContainsKey(addr))
                    {
                        label = m_pDBData.VariableLabels[addr];
                    }
                    labels += String.Format("{0:X4}:{1} {2}\r", addr, label, m_memDump.GetMicroDump(addr));
                }
                labels += "Reads:\r";
                foreach (int addr in m_lastFunction.ReadFrom)
                {
                    string label = "unknown";
                    if (m_pDBData.VariableLabels.ContainsKey(addr))
                    {
                        label = m_pDBData.VariableLabels[addr];
                    }
                    labels += String.Format("{0:X4}:{1} {2}\r", addr, label, m_memDump.GetMicroDump(addr));
                }
                labels += "Writes:\r";
                foreach (int addr in m_lastFunction.WriteTo)
                {
                    string label = "unknown";
                    if (m_pDBData.VariableLabels.ContainsKey(addr))
                    {
                        label = m_pDBData.VariableLabels[addr];
                    }
                    labels += String.Format("{0:X4}:{1}\r", addr, label);
                }
                return labels;
            }
            return "unknown function";
        }

        FunctionData getFuncForPC()
        {
            int PC = m_registerSet.GetPC();
            FunctionData currFunc = m_pDBData.funcData.Find(match => PC >= match.StartAddr && PC < match.EndAddr);
            return currFunc;
        }
    }
}
