using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace VICEPDBMonitor
{
    class RegexMan
    {
        /*
            * group    desc
            * 1        BREAK or WATCH
            * 2        number
            * 3        Address with range in the form XXXX-$XXXX or XXXX
            * 4        first Address no $ so just XXXX
            * 5        second Address if present in form XXXX or same as 4 if not a range
            * 6        mode exec/load/store/load store/exec load store
            * 7        empty of "disabled" if the break point is disabled
            */
        public enum eBreakPointResult : int
        {
             mode = 1
            ,number
            ,address_range
            ,first_address
            ,second_address
            ,operation
            ,ifDisabled
        }

        public static Regex BreakPointResult = new Regex(@"^(BREAK|WATCH|):\s([0-9]+)\s+C:\$(([0-9a-fA-F]{4})\-?\$?([0-9a-fA-F]{4})?)\s+\(Stop on\s([a-z\s]+)\)\s*(disabled)?");
        /*
         * group    desc
         * 1        break point number
         * 2        break point mode, exec/load/store that triggered it
         * 3        address
         * 4        VIC II Raster Line
         * 5        VIC II Raster Cycle on Line
         */
        public enum eBreakPointHit : int
        {
             number = 1
            ,mode = 2
            ,address = 3
            ,rasterLine = 4
            ,rasterCycle = 5
        }

        public static Regex BreakPointHit = new Regex(@"^#([0-9]+)\s+\(Stop on\s+(exec|load|store)\s+([0-9a-fA-F]{4})\)\s+([0-9]{3})\s+([0-9]{3}).*");
    }
}
