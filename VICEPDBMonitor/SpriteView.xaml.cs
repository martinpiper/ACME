using System;
using System.Collections.Generic;
using System.IO;
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
    /// Interaction logic for SpriteView.xaml
    /// </summary>
    public partial class SpriteView : Window
    {
		int m_startAddress;
		int m_startBank;
		int m_bytesRead;
		int m_rounds; //how many times have we requested sprite data

		int m_backgroundColour;
		int m_mulCol0;
		int m_mulCol1;

		bool[,] m_hiresOrMulti; 

		WriteableBitmap m_wb;
        MainWindow m_mainWindow;

		public SpriteView(MainWindow mainWindow)
        {
            InitializeComponent();
			m_backgroundColour = 0;
            m_mainWindow = mainWindow;
			//DataContext = new C128ViewModel();
			m_hiresOrMulti = new bool[16, 16];
			setAllHires();
		}

		public void setAllHires()
		{
			for( int x = 0; x < 16; ++x)
			{
				for( int y = 0; y < 16; ++y)
				{
					m_hiresOrMulti[x, y] = false;
				}
			}
		}

		public void setAllMulti()
		{
			for (int x = 0; x < 16; ++x)
			{
				for (int y = 0; y < 16; ++y)
				{
					m_hiresOrMulti[x, y] = true;
				}
			}
		}

        private void handleDisplaySprites(int startAddress, int bank)
        {

            m_startAddress = startAddress;
            m_startBank = bank;

            VICECOMManager vcom = VICECOMManager.getVICEComManager();
            vcom.addTextCommand("bank ram", CommandStruct.eMode.DoCommandThrowAwayResults, null, null, null);
            vcom.addBinaryMemCommand(m_startAddress,         m_startAddress + 0x2fff, new CommandStruct.CS_BinaryDelegate(got_ramLo), null, this.Dispatcher);
            vcom.addBinaryMemCommand(m_startAddress+ 0x2000, m_startAddress + 0x4fff, new CommandStruct.CS_BinaryDelegate(got_ram), null, this.Dispatcher);
            vcom.addTextCommand("bank cpu", CommandStruct.eMode.DoCommandThrowAwayResults, null, null, null);
        }

        private void got_ramLo(byte[] data, object none)
        {
            C64RAM ram = C64RAM.getInstace();
            ram.injectBinaryData(m_startAddress, data);
        }
        private void got_ram(byte[] data, object none)
        {
            C64RAM ram = C64RAM.getInstace();
            ram.injectBinaryData(m_startAddress+0x2000, data);
            renderSprites();
        }
        /*m_mainWindow.SendCommand("bank ram\n");
        m_mainWindow.GetReply();           

        byte[] data = m_mainWindow.sendBinaryMemCommandAndGetData(startAddress, startAddress + (16 * 1024) - 1);
        C64RAM ram = C64RAM.getInstace();
        ram.injectBinaryData(m_startAddress, data);
        renderSprites();
        m_mainWindow.SendCommand("bank cpu\n");
        m_mainWindow.GetReply();*/
   
		/*public bool handleDataFromVICESprite(string data)
		{
			C128RAM ram = C128RAM.getInstace();
			m_bytesRead -= ram.injectRAMFromVICE_m(data);
			if (m_bytesRead <= 0)
			{
				Dispatcher.BeginInvoke((Action)(() =>
				{
					renderSprites();
				}));
				VICECOMM vc = VICECOMM.getInstance();
				vc.SendCommandToVICE("bank cpu\n");
				return true; // I'm not waiting on more data, so remove my handler
			}
			else
			{
				doRound();
			}
			return false; //wait for more data
		}*/

		public void renderSprites()
		{
			//byte[] imageArray = new byte[(16 * 24) * (16 * 21) * 4]; //16x16 with 4 bits per pixel

			m_wb = new WriteableBitmap(24 * 16, 21 * 16, 96, 96, PixelFormats.Bgra32, BitmapPalettes.WebPalette);

			int dataPtr = (m_startBank * (64 * 1024)) + m_startAddress;

			
			int sprCol = spriteCol.SelectedIndex;

			for ( int y = 0; y < 16; ++y)
			{
				for( int x = 0; x < 16; ++x)
				{
					renderSprite(dataPtr, x, y, m_hiresOrMulti[x, y], sprCol, m_wb);
					dataPtr += 64;
				} //x
			}// y
			
			canvas.Source = m_wb;
		}

		void renderSprite(int addr, int spriteX,int spriteY, bool multicolour, int sprColour, WriteableBitmap wb)
		{

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
			int spriteTLX = spriteX * 24;
			int spriteTLY = spriteY * 21;
			

			for (int sy = 0; sy < 21; ++sy)
			{
				rect.Y = spriteTLY + sy;
				for (int sx = 0; sx < 3; ++sx)
				{

					byte r = ram[addr];
					//int bitmapIndex = ((spriteTLY + sy) * (24 * 4)) + ((spriteTLX + (sx*8)) * 4);
					if (multicolour)
					{
						for (int p = 0; p < 8; p += 2)
						{
							rect.X = spriteTLX + (sx * 8) + p;
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
						}
					}
					else
					{
						for (int p = 0; p < 8; ++p)
						{

							rect.X = spriteTLX + (sx * 8) + p;

							if ((r & 128) == 128)
							{
								wb.WritePixels(rect, VICPallete.getSafePalBGR32(sprColour), 4, 0);
							}
							else
							{
								wb.WritePixels(rect, VICPallete.getSafePalBGR32(m_backgroundColour), 4, 0);
							}
							r = (byte)(r << 1); //get next pixel
						} // p
					}
					addr++;
				} //sx
			} //sy
		}
		private void bank00_Click(object sender, RoutedEventArgs e)
		{
			handleDisplaySprites(0, 0);
		}

		private void bank01_Click(object sender, RoutedEventArgs e)
		{
			handleDisplaySprites(0x4000, 0);
		}

		private void bank02_Click(object sender, RoutedEventArgs e)
		{
			handleDisplaySprites(0x8000, 0);
		}

		private void bank03_Click(object sender, RoutedEventArgs e)
		{
			handleDisplaySprites(0xC000, 0);
		}

		private void backgroundColour_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			m_backgroundColour = backgroundColour.SelectedIndex;
			m_mulCol0 = mcol0.SelectedIndex;
			m_mulCol1 = mcol1.SelectedIndex;
			renderSprites();
		}

		private void canvas_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if( e.LeftButton == MouseButtonState.Pressed && e.ClickCount == 2)
			{
				Point clickPoint = e.GetPosition(canvas);
				double x = clickPoint.X / 24.0;
				double y = clickPoint.Y / 21.0;

				int SpriteX = (int)Math.Floor(x);
				int SpriteY = (int)Math.Floor(y);

				m_hiresOrMulti[SpriteX, SpriteY] = !m_hiresOrMulti[SpriteX, SpriteY];

				int dataPtr = (m_startBank * (64 * 1024)) + m_startAddress;
				dataPtr += SpriteY * (16 * 64);
				dataPtr += SpriteX * 64;
				int sprCol = spriteCol.SelectedIndex;

				renderSprite(dataPtr, SpriteX, SpriteY, m_hiresOrMulti[SpriteX, SpriteY], sprCol, m_wb);
			}
		}

		private void allHiresButton_Click(object sender, RoutedEventArgs e)
		{
			setAllHires();
			renderSprites();
		}

		private void allMultiButton_Click(object sender, RoutedEventArgs e)
		{
			setAllMulti();
			renderSprites();
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

            int dataPtr = (m_startBank * (64 * 1024)) + m_startAddress;
            dataPtr += SpriteY * (16 * 64);
            dataPtr += SpriteX * 64;

            hoverTip.Content = string.Format("{0:X02}@{1:X04}", SpriteY*16+SpriteX, dataPtr);
        }
    }
}
