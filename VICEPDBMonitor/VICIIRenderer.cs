using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace VICEPDBMonitor
{
    class VICIIRenderer
    {
        public static byte[] charsetHex1;
        public static byte[] charsetHex2;
        public static byte[] charsetHex3;
        public static byte[] charsetCHARROM;

        public static void initRenderer()
        {
            string currentDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            string charsetFolder = Path.Combine(currentDirectory, "charsets");

            charsetHex1 = File.ReadAllBytes(Path.Combine(charsetFolder, "hexcharset1.prg"));
            charsetHex2 = File.ReadAllBytes(Path.Combine(charsetFolder, "hexcharset2.prg"));
            charsetHex3 = File.ReadAllBytes(Path.Combine(charsetFolder, "hexcharset3.prg"));
            charsetCHARROM = File.ReadAllBytes(Path.Combine(charsetFolder, "CHARROM.chr"));

            for (int i = 2; i < 2048; ++i) //strip off the prg header
            {
                charsetHex1[i - 2] = charsetHex1[i];
                charsetHex2[i - 2] = charsetHex2[i];
                charsetHex3[i - 2] = charsetHex3[i];
            }
        }

        public enum eExtraCharsets
        {
            extraCharsetStart = 0x20000
            ,hexCharset1 = 0x20000
            ,hexCharset2 = 0x20800
            ,hexCharset3 = 0x21000
            ,charrom_lo = 0x21800
            ,charrom_hi = 0x22000
        }

        public static void renderChar(int addr, int charX, int charY, bool multicolour, int charColour, int backgroundColour, int mulCol0, int mulCol1, WriteableBitmap wb)
        {
            byte[] ram;

            //check to see if we are using an extra charset
            if (addr < (int)eExtraCharsets.extraCharsetStart)
            {
                C64RAM ramObjc = C64RAM.getInstace();
                ram = ramObjc.getRAM();
            }
            else
            {
                if (addr >= (int)eExtraCharsets.hexCharset1 && addr < (int)eExtraCharsets.hexCharset1 + 0x800)
                {
                    ram = charsetHex1;
                    addr &= 0x7ff;
                }
                else if (addr >= (int)eExtraCharsets.hexCharset2 && addr < (int)eExtraCharsets.hexCharset2 + 0x800)
                {
                    ram = charsetHex2;
                    addr &= 0x7ff;
                }
                else if (addr >= (int)eExtraCharsets.hexCharset3 && addr < (int)eExtraCharsets.hexCharset3 + 0x800)
                {
                    ram = charsetHex3;
                    addr &= 0x7ff;
                }
                else if (addr >= (int)eExtraCharsets.charrom_lo && addr < (int)eExtraCharsets.charrom_hi + 0x800)
                {
                    ram = charsetCHARROM;
                    addr &= 0xfff;
                }
                else
                {
                    C64RAM ramObjc = C64RAM.getInstace();
                    ram = ramObjc.getRAM();
                }
            }
            

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
									wb.WritePixels(rect, VICPallete.getSafePalBGR32(backgroundColour), 8, 0);
                                    break;
                                case 64: //%01
									wb.WritePixels(rect, VICPallete.getSafePalBGR32(mulCol0), 8, 0);
                                    break;
                                case 128: //%10
									wb.WritePixels(rect, VICPallete.getSafePalBGR32(mulCol1), 8, 0);
                                    break;
                                case 192: //%11
									wb.WritePixels(rect, VICPallete.getSafePalBGR32(charColour), 8, 0);
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
								wb.WritePixels(rect, VICPallete.getSafePalBGR32(charColour), 4, 0);
                            }
                            else
                            {
								wb.WritePixels(rect, VICPallete.getSafePalBGR32(backgroundColour), 4, 0);
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
