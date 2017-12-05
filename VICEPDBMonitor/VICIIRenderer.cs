using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace VICEPDBMonitor
{
    class VICIIRenderer
    {
        public static void renderChar(int addr, int charX, int charY, bool multicolour, int charColour, int backgroundColour, int mulCol0, int mulCol1, WriteableBitmap wb)
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
            int spriteTLX = charX * 8;
            int spriteTLY = charY * 8;


            for (int sy = 0; sy < 8; ++sy)
            {
                rect.Y = spriteTLY + sy;
                for (int sx = 0; sx < 1; ++sx)
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
                                    wb.WritePixels(rect, VICPallete.palBGR32[backgroundColour], 8, 0);
                                    break;
                                case 64: //%01
                                    wb.WritePixels(rect, VICPallete.palBGR32[mulCol0], 8, 0);
                                    break;
                                case 128: //%10
                                    wb.WritePixels(rect, VICPallete.palBGR32[mulCol1], 8, 0);
                                    break;
                                case 192: //%11
                                    wb.WritePixels(rect, VICPallete.palBGR32[charColour], 8, 0);
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
                                wb.WritePixels(rect, VICPallete.palBGR32[charColour], 4, 0);
                            }
                            else
                            {
                                wb.WritePixels(rect, VICPallete.palBGR32[backgroundColour], 4, 0);
                            }
                            r = (byte)(r << 1); //get next pixel
                        } // p
                    }
                    addr++;
                } //sx
            } //sy
        }
    }
}
