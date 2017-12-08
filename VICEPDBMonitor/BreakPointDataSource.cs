using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VICEPDBMonitor
{
    class BreakPointDataSource
    {
        public int Number { get; set; }
        public string NumberString { get { return Number.ToString(); } }
        public string Address { get; set; }
        public string EndAddress { get; set; }
        public string Mode { get; set; }
        public int AddressNumber { get; set; }
        public int EndAddressNumber { get; set; }
        public bool Disabled { get; set; }
        public bool Range { get; set; }

        public void setFromMatch(Match match)
        {
            Number = Int32.Parse(match.Groups[2].Value);
            Address = match.Groups[3].Value;
            Disabled = match.Groups[7].Length == 0;
            Mode = match.Groups[6].Value;
            try
            {
                AddressNumber = Int32.Parse(match.Groups[4].Value, System.Globalization.NumberStyles.HexNumber);
            }
            catch (Exception ex)
            {
                AddressNumber = -1;
            }
            if( match.Groups[5].Value.Length > 0 )
            {
                try
                {
                    EndAddressNumber = Int32.Parse(match.Groups[5].Value, System.Globalization.NumberStyles.HexNumber);
                }
                catch (Exception ex)
                {
                    EndAddressNumber = -1;
                }
            }
            else
            {
                EndAddressNumber = AddressNumber;
                Address += "      ";
            }             
        }
    }
}
