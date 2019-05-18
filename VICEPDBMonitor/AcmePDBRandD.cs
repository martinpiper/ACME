using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace VICEPDBMonitor
{    
    class AcmePDBRandD : IPDBReaderAndDisplay
    {
        IRegisterSet m_registerSet;
        IMemDump m_memDump;
        RichTextBox m_textBox;
        TextBox m_labelsBox;
        PDBData m_PDBData;

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
            m_PDBData = PDBData.create(commandLineArgs);
        }

        public string PostEnterKeyForCommand(string command)
        {
            // Produce a list of variable names and values relevant to the current PC and its zone
            List<LabelInfo> allLabels = new List<LabelInfo>();

            try
            {
                int theZone = m_PDBData.getAddrInfoForAddr(m_registerSet.GetPC()).mZone; //mAddrInfoByAddr[mPC].mZone;
                while (theZone >= 0)
                {
                    List<LabelInfo> theLabels = m_PDBData.getLabelsForZone(theZone); //mLabelInfoByZone[theZone];
                    allLabels.AddRange(theLabels);

                    // MPi: TODO: Replace with previous zone in the hierarchy when ACME saves it
                    if (theZone > 0)
                    {
                        theZone = 0;    // For now just display the global zone
                    }
                    else
                    {
                        break;
                    }
                }
                allLabels.Sort((a, b) => b.mLabel.Length.CompareTo(a.mLabel.Length));
            }
            catch (System.Exception)
            {

            }

            // Now add all other labels to the end of the list
            allLabels.AddRange(m_PDBData.getAllLabels()); // mAllLabels);

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

            return command;
        }

        public void SetCodeWindowControl(Control textControl)
        {
            m_textBox = textControl as RichTextBox;
            if(m_textBox == null)
            {
                throw new Exception("Invalid control, this viewer needs a RichTextBox");
            }
        }

        public void SetLabelsWindowControl(Control textControl)
        {
            m_labelsBox = textControl as TextBox;
            if (m_labelsBox == null)
            {
                throw new Exception("Invalid control, this viewer needs a TextBox");
            }
        }

        public void SetLabelView(string text)
        {
            m_labelsBox.Text = text;
        }

        public void SetRegisterSet(IRegisterSet registerSet)
        {
            m_registerSet = registerSet;
        }

        public void SetMemDump(IMemDump memDump)
        {
            m_memDump = memDump;
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
                /*Run r = new Run("", mTextBox.CaretPosition.DocumentEnd);
				r.Background = null;*/
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

        public string ShowSrcDissGetPostDissaem(string reply, object userData)
        {
            ShowSrcDissStruct sms = userData as ShowSrcDissStruct;
            sms.disassemblyAfter = reply;
            string displayText = String.Empty;
            try
            {
                string lastSourceDisplayed = "";
                int lastSourceIndexDisplayed = -1;
                int lastSourceLineDisplayed = -1;

                int[] lastDisplayedLine = new int[m_PDBData.getNumFiles()]; //mSourceFileNamesLength
                int i;
                for (i = 0; i < m_PDBData.getNumFiles(); i++) //mSourceFileNamesLength
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
                    if (doingBefore && theAddr >= m_registerSet.GetPC())
                    {
                        split = sms.disassemblyAfter.Split('\r');
                        index = 0;
                        doingBefore = false;
                        continue;
                    }

                    try
                    {
                        AddrInfo addrInfo = m_PDBData.getAddrInfoForAddr(theAddr); //mAddrInfoByAddr[theAddr];
                        if (lastSourceDisplayed != m_PDBData.getSourceFileName(addrInfo.mFile)) //mSourceFileNames[addrInfo.mFile])
                        {
                            lastSourceDisplayed = m_PDBData.getSourceFileName(addrInfo.mFile); // mSourceFileNames[addrInfo.mFile];
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
                            lastSourceLine = string.Format("{0,5:###}", i) + ": " + m_PDBData.getLineFromSourceFile(addrInfo.mFile, i); // mSourceFiles[addrInfo.mFile][i];
                            lastSourceIndexDisplayed = addrInfo.mFile;
                            lastSourceLineDisplayed = i;
                        }
                        lastDisplayedLine[addrInfo.mFile] = addrInfo.mLine + 1;
                    }
                    catch (System.Exception)
                    {

                    }

                    if (theAddr == m_registerSet.GetPC())
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

            return displayText;
        }

        public string ShowSrcPostRegisters()
        {
            string displayText = string.Empty;
            try
            {
                AddrInfo addrInfo = m_PDBData.getAddrInfoForAddr(m_registerSet.GetPC()); // mAddrInfoByAddr[mPC];
                // MPi: TODO: Tweak the 20 range based on the display height?
                int range = 20;
                int startPrev = m_registerSet.GetPC();
                // Step backwards trying to find a good starting point to disassemble
                while (range-- > 0)
                {
                    AddrInfo addrInfo2 = m_PDBData.getAddrInfoForAddr(startPrev); // mAddrInfoByAddr[startPrev];
                    if (addrInfo2.mPrevAddr < 0)
                    {
                        break;
                    }
                    startPrev = addrInfo2.mPrevAddr;
                }

                displayText += "File:" + m_PDBData.getSourceFileName(addrInfo.mFile) + "\n"; // mSourceFileNames[addrInfo.mFile] + "\n";
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
                    string source = m_PDBData.getLineFromSourceFile(addrInfo.mFile, theLine++);  //mSourceFiles[addrInfo.mFile][theLine++];
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
            return displayText;
        }

        public ShowSrcDissStruct show_diss_common()
        {
            try
            {
                // MPi: TODO: Tweak the 10 range based on the display height?
                int range = 15;
                int startPrev = m_registerSet.GetPC();
                // Step backwards trying to find a good starting point to disassemble
                while (range-- > 0)
                {
                    AddrInfo addrInfo2 = m_PDBData.getAddrInfoForAddr(startPrev); //mAddrInfoByAddr[startPrev];
                    if (addrInfo2.mPrevAddr < 0)
                    {
                        break;
                    }
                    startPrev = addrInfo2.mPrevAddr;
                }
                // MPi: TODO: Tweak the 10 range based on the display height?
                range = 20;
                // Step forwards trying to find a good ending point to disassemble
                int endNext = m_registerSet.GetPC();
                while (range-- > 0)
                {
                    AddrInfo addrInfo2 = m_PDBData.getAddrInfoForAddr(endNext); //mAddrInfoByAddr[endNext];
                    if (addrInfo2.mNextAddr < 0)
                    {
                        break;
                    }
                    endNext = addrInfo2.mNextAddr;
                }

                ShowSrcDissStruct sms = new ShowSrcDissStruct()
                {
                    endNext = endNext
                    ,
                    startPrev = startPrev
                };
                return sms;
            }
            catch (Exception ex)
            {

            }
            return null;
        }

        public string UpdateLabels(bool usedLabels, bool execUsed,bool accessUsed, Dictionary<int,int> executedCount, Dictionary<int,int> accessedCount)
        {
            string labels = "";
            try
            {
                List<string> allLabels = new List<string>();
                int theZone = m_PDBData.getAddrInfoForAddr(m_registerSet.GetPC()).mZone;
                while (theZone >= 0)
                {
                    List<LabelInfo> theLabels = m_PDBData.getLabelsForZone(theZone); //mLabelInfoByZone[theZone];

                    foreach (LabelInfo aLabel in theLabels)
                    {
                        if ((usedLabels == false) || ((usedLabels == true) && (aLabel.mUsed == true)))
                        {
                            string labelText = "";
                            int count;
                            if (execUsed && executedCount.TryGetValue(aLabel.mAddr, out count))
                            {
                                labelText += "E" + count + ":";
                            }
                            if (accessUsed && accessedCount.TryGetValue(aLabel.mAddr, out count))
                            {
                                labelText += "A" + count + ":";
                            }
                            if (theZone != 0)
                            {
                                labelText = ".";
                            }

                            labelText += aLabel.mLabel + " $" + aLabel.mAddr.ToString("X") + m_memDump.GetMicroDump(aLabel.mAddr);
                            allLabels.Add(labelText);
                        }
                    }
                    // MPi: TODO: Replace with previous zone in the hierarchy when ACME saves it
                    if (theZone > 0)
                    {
                        theZone = 0;    // For now just display the global zone
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
            return labels;
        }
    }
}
