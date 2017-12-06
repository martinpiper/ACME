using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VICEPDBMonitor
{
	class VICPallete
	{
		static public byte[] getSafePalBGR32(int index)
		{
			if (index < 0)
			{
				index = 0;
			}
			else if (index >= palBGR32.Length)
			{
				index = palBGR32.Length - 1;
			}
			return palBGR32[index];
		}

    static private byte[][] palBGR32 =
		{
			new byte[]{ 0,0,0,255,0,0,0,255},
			new byte[]{ 255,255,255,255,255,255,255,255 },
			new byte[]{ 64,74,146,255,64,74,146,255 },
			new byte[]{ 204,197,132,255,204,197,132,255 },
			new byte[]{ 182,81,147,255,182,81,147,255 },
			new byte[]{ 75,177,114,255,75,177,114,255 },
			new byte[]{ 170,58,72,255,170,58,72,255 },
			new byte[]{ 124,223,213,255,124,223,213,255 },
			new byte[]{ 45,105,153,255,45,105,153,255 },
			new byte[]{ 0,82,103,255,0,82,103,255 },
			new byte[]{ 120,129,193,255,120,129,193,255 },
			new byte[]{ 96,96,96,255,96,96,96,255  },
			new byte[]{ 138,138,138,255,138,138,138,255 },
			new byte[]{ 145,236,179,255,145,236,179,255  },
			new byte[]{ 222,122,134,255,222,122,134,255 },
			new byte[]{ 179,179,179,255, 179, 179, 179, 255 }
		};
	}
}
