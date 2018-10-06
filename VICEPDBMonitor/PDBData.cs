using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Globalization;

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

    public class PDBData
    {
        private static PDBData g_PDBData = null;

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

        public static PDBData getInstance() { return g_PDBData; }

        public static PDBData create(string[] commandLineArgs)
        {
            g_PDBData = new PDBData();
            g_PDBData.parseData(commandLineArgs);
            return g_PDBData;
        }

        public void parseData(string[] commandLineArgs)
        {
            int i;
            string line;
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
                                addrInfo.mLine = int.Parse(tokens[3]) - 1;  // Files lines are 1 based in the debug file
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
                                    labelInfo.mZone += baseZone;    // Helps to distinguish zones for multiple PDB files
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
                for (l = localFileIndex; l < mSourceFileNamesLength; l++)
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
        }

        public AddrInfo getAddrInfoForAddr(int PC)
        {
            return mAddrInfoByAddr[PC];
        }

        public List<LabelInfo> getLabelsForZone(int zone)
        {
            return mLabelInfoByZone[zone];
        }

        public int getNumFiles()
        {
            return mSourceFileNamesLength;
        }

        public string getSourceFileName(int num)
        {
            return mSourceFileNames[num];
        }

        public string getLineFromSourceFile(int file, int line)
        {
            return mSourceFiles[file][line];
        }

        public List<LabelInfo> getAllLabels()
        {
            return mAllLabels;
        }
    }
}
