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
using System.Windows.Shapes;


namespace VICEPDBMonitor
{
    /// <summary>
    /// Interaction logic for GeneralView.xaml
    /// </summary>
    public partial class GeneralView : Window
    {
        int m_bytesRead;

        int m_backgroundColour;
        int m_mulCol0;
        int m_mulCol1;

        bool m_hiresOrMulti;

        WriteableBitmap m_wb;
        MainWindow m_mainWindow;

        Type scriptControlType;
        dynamic scriptControl;

        public GeneralView(MainWindow mainWindow)
        {
            InitializeComponent();

            scriptControlType = Type.GetTypeFromProgID("MSScriptControl.ScriptControl");
            scriptControl = Activator.CreateInstance(scriptControlType);
            scriptControl.Language = "JScript";

            m_backgroundColour = 0;
            m_mainWindow = mainWindow;
            DataContext = new C128ViewModel();
            setAllHires();
            handleDisplayView();
        }

        public void setAllHires()
        {
            m_hiresOrMulti = false;
        }

        public void setAllMulti()
        {
            m_hiresOrMulti = true;
        }

        private void handleDisplayView()
        {
            VICECOMManager vcom = VICECOMManager.getVICEComManager();
            vcom.addTextCommand("bank ram", CommandStruct.eMode.DoCommandThrowAwayResults, null, null, null);
            vcom.addBinaryMemCommand(0, 0xffff, new CommandStruct.CS_BinaryDelegate(got_ramLo), 1234, this.Dispatcher);
            vcom.addTextCommand("bank cpu", CommandStruct.eMode.DoCommandThrowAwayResults, null, null, null);
        }

        private void got_ramLo(byte[] data, object none)
        {
            C64RAM ram = C64RAM.getInstace();
            ram.injectBinaryData(0, data);

            renderView();
        }

        public void renderView()
        {
            theDebug.Text = "Debug output";
            string script = theScript.Text;

            int width = 320;
            int height = 200;
            try
            {
                width = int.Parse(theWidth.Text);
            }
            catch (System.Exception)
            {
            }

            try
            {
                height = int.Parse(theHeight.Text);
            }
            catch (System.Exception)
            {
            }

            m_wb = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, BitmapPalettes.WebPalette);

            int sprCol = spriteCol.SelectedIndex;

            try
            {
                for (int y = 0; y < height; ++y)
                {
                    for (int x = 0; x < width; x += 8)
                    {
                        renderByte(script, x, y, m_hiresOrMulti, sprCol, m_wb);
                    } //x
                }// y
            }
            catch (System.Exception e)
            {
                theDebug.Text = e.Message;
            }

            canvas.Source = m_wb;
        }

        void renderByte(string script, int posX, int posY, bool multicolour, int sprColour, WriteableBitmap wb)
        {
            script = script.Replace("xpos", posX.ToString());
            script = script.Replace("ypos", posY.ToString());

            C64RAM ramObjc = C64RAM.getInstace();
            byte[] ram = ramObjc.getRAM();

            Int32Rect rect = new Int32Rect();
            if (multicolour)
            {
                rect.Width = 2;
            }
            else
            {
                rect.Width = 1;
            }
            rect.Height = 1;
            rect.X = posX;
            rect.Y = posY;

            int addr = 0;
            object result = scriptControl.Eval(script);
            addr = Convert.ToInt32(result);

            addr = addr & 0xffff;
            byte r = ram[addr];

            if (multicolour)
            {
                for (int p = 0; p < 8; p += 2)
                {
                    switch (r & 192)
                    {
                        default:
                        case 0:  //%00
                            wb.WritePixels(rect, VICPallete.getSafePalBGR32(m_backgroundColour), 8, 0);
                            break;
                        case 64: //%01
                            wb.WritePixels(rect, VICPallete.getSafePalBGR32(m_mulCol0), 8, 0);
                            break;
                        case 128: //%10
                            wb.WritePixels(rect, VICPallete.getSafePalBGR32(sprColour), 8, 0);
                            break;
                        case 192: //%11
                            wb.WritePixels(rect, VICPallete.getSafePalBGR32(m_mulCol1), 8, 0);
                            break;
                    }
                    r = (byte)(r << 2); //get next pixel
                    rect.X += 2;
                }
            }
            else
            {
                for (int p = 0; p < 8; ++p)
                {
                    if ((r & 128) == 128)
                    {
                        wb.WritePixels(rect, VICPallete.getSafePalBGR32(sprColour), 4, 0);
                    }
                    else
                    {
                        wb.WritePixels(rect, VICPallete.getSafePalBGR32(m_backgroundColour), 4, 0);
                    }
                    r = (byte)(r << 1); //get next pixel
                    rect.X++;
                } // p
            }
        }

        private void backgroundColour_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            m_backgroundColour = backgroundColour.SelectedIndex;
            m_mulCol0 = mcol0.SelectedIndex;
            m_mulCol1 = mcol1.SelectedIndex;
            renderView();
        }

        private void canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void allHiresButton_Click(object sender, RoutedEventArgs e)
        {
            setAllHires();
            renderView();
        }

        private void allMultiButton_Click(object sender, RoutedEventArgs e)
        {
            setAllMulti();
            renderView();
        }

        private void canvas_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            Point mouse = Mouse.GetPosition(canvas);
            double x = mouse.X / 24.0;
            double y = mouse.Y / 21.0;

            int SpriteX = (int)Math.Floor(x);
            int SpriteY = (int)Math.Floor(y);

            C64RAM ram = C64RAM.getInstace();
            byte[] RAM = ram.getRAM();

            int dataPtr = 0;
            dataPtr += SpriteY * (16 * 64);
            dataPtr += SpriteX * 64;

            hoverTip.Content = string.Format("{0:X02}@{1:X04}", SpriteY * 16 + SpriteX, dataPtr);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            handleDisplayView();
            renderView();
        }

        private void theWidth_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void theHeight_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void theScript_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void theScript_TextChanged_1(object sender, TextChangedEventArgs e)
        {

        }
    }
}
