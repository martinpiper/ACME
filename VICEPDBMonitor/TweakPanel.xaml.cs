using System;
using System.Collections.Generic;
using System.Globalization;
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
    public class TweakPanelSubComponent
    {
        public enum eDisplayType
        {
            Decimal = 0,
            Hex
        }

        public enum eWidth
        {
            Byte = 0,
            Word
        }

        public eDisplayType mType;
        public eWidth mWidth;
        public int mAddress;
        public int mTextBoxTag;
        public TextBox mTextBox;

        public TweakPanelSubComponent(eDisplayType type, eWidth width, int address,int textBoxTag,TextBox textBox )
        {
            mType = type;
            mWidth = width;
            mAddress = address;
            mTextBoxTag = textBoxTag;
            mTextBox = textBox;
        }

        public void setFromValue(MainWindow mw)
        {
            int value = 0;
            switch(mWidth)
            {
                default:
                case eWidth.Byte:
                    value = mw.getSafeC64Memory(mAddress);
                    break;
                case eWidth.Word:
                    value = mw.getSafeC64Memory(mAddress) + (mw.getSafeC64Memory(mAddress + 1) * 256);
                    break;
            }

            switch(mType)
            {
                default:
                case eDisplayType.Decimal:
                    mTextBox.Text = String.Format("{0:d}", value);
                    break;
                case eDisplayType.Hex:
                    switch (mWidth)
                    {
                        default:
                        case eWidth.Byte:
                            mTextBox.Text = String.Format("{0:X02}", value);
                            break;
                        case eWidth.Word:
                            mTextBox.Text = String.Format("{0:X04}", value);
                            break;
                    }
                    break;
            }
        }

        public void updateC64(MainWindow mw)
        {
            VICECOMManager vcom = VICECOMManager.getVICEComManager();
            string text = mTextBox.Text;
            int value = 0;
            string command = "";
            switch (mType)
            {
                default:
                case eDisplayType.Decimal:
                    if(Int32.TryParse(text, System.Globalization.NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                    {
                        command = String.Format(">{0:X04} {1:X02}", mAddress, value);
                    }

                    mTextBox.Text = String.Format("{0:d}", value);
                    break;
                case eDisplayType.Hex:
                    switch (mWidth)
                    {
                        default:
                        case eWidth.Byte:
                            if (Int32.TryParse(text, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
                            {
                                command = String.Format(">{0:X04} {1:X02}", mAddress, value);
                            }
                            break;
                        case eWidth.Word:
                            if (Int32.TryParse(text, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
                            {
                                command = String.Format(">{0:X04} {1:X02} {2:X02}", mAddress, value&0xFF,(value&0xFF00)/256);
                            }
                            break;
                    }
                    break;
            }
            if(command.Length > 0)
            {
                vcom.addTextCommand(command, CommandStruct.eMode.DoCommandThrowAwayResults, null, null, mw.Dispatcher);
            }
        }
    }
    /// <summary>
    /// Interaction logic for TweakPanel.xaml
    /// </summary>
    public partial class TweakPanel : Window
    {
        public MultiMap<int, TweakPanelSubComponent> mTagToSubComponent;
        MainWindow mMW;

        public TweakPanel(MainWindow main)
        {
            InitializeComponent();
            mMW = main;
        }

        private void buttonRebuildControls_Click(object sender, RoutedEventArgs e)
        {
            string format = mScript.Text;
            string[] parts = format.Split(';');
            mTagToSubComponent = new MultiMap<int, TweakPanelSubComponent>();
            mDynamic.Children.Clear();
            int tag = 1;
            mMW.TestForMemoryDump(true); // make sure everything is up to date

            try
            {
                foreach (string part in parts)
                {
                    string[] commParts = part.Split(':');
                    string address = commParts[0].Trim();
                    if (address.StartsWith("$"))
                    {
                        address = address.Substring(1); //remove $
                    }
                    string type = commParts[1].Trim().ToLower();
                    TweakPanelSubComponent.eDisplayType displayType = TweakPanelSubComponent.eDisplayType.Decimal;
                    TweakPanelSubComponent.eWidth width = TweakPanelSubComponent.eWidth.Byte;
                    switch (type[0])
                    {
                        case 'd': //decimal
                            displayType = TweakPanelSubComponent.eDisplayType.Decimal;
                            break;
                        case 'h': //hex
                            displayType = TweakPanelSubComponent.eDisplayType.Hex;
                            break;
                        default:
                            break;
                    }

                    switch (type[1])
                    {
                        case 'b': //byte
                            width = TweakPanelSubComponent.eWidth.Byte;
                            break;
                        case 'w': //word
                            width = TweakPanelSubComponent.eWidth.Word;
                            break;
                        default:
                            break;
                    }

                    Label lab = new Label();
                    lab.Content = address;
                    TextBox tb = new TextBox();
                    tb.Text = "dummy";
                    tb.Tag = tag;
                    StackPanel sp = new StackPanel();
                    sp.Orientation = Orientation.Horizontal;
                    sp.Children.Add(lab);
                    sp.Children.Add(tb);

                    mDynamic.Children.Add(sp);

                    int address_int = 64 * 1024 + 1;
                    Int32.TryParse(address, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out address_int);

                    TweakPanelSubComponent sub = new TweakPanelSubComponent(displayType, width, address_int, tag, tb);
                    sub.setFromValue(mMW);
                    mTagToSubComponent.Add(tag, sub);
                    tag += 1;
                }
            }
            catch(Exception /*e*/)
            {
                MessageBox.Show("invalid desc string");                
            }
        }

        private void buttonUpdateC64_Click(object sender, RoutedEventArgs e)
        {
            foreach(int key in mTagToSubComponent.Keys)
            {
                TweakPanelSubComponent sub = mTagToSubComponent[key][0];
                sub.updateC64(mMW);
            }
        }
    }
}
