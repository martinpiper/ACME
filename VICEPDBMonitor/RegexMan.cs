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

        /*
         * group    desc
         * 1        address
         * 2        opcode hex values
         * 3        last opcode hex value- ignore
         * 4        opcode string
         * 5        opcode param string
         * 6        last char in opcode param string - ingore
         * 7        A register Hex
         * 8        X register Hex
         * 9        Y register Hex
         * 10       SP register Hex
         * 11       N or ' '
         * 12       V or ' '
         * 13       B or ' '
         * 14       D or ' '
         * 15       I or ' '
         * 16       Z or ' '
         * 17       C or ' '
         */
         
        public enum eCPUHistoryLine : int
        {
             address        = 1
            ,opcode_hex     = 2
            ,opcode_string  = 4
            ,opcode_params  = 5
            ,a_reg          = 7
            ,x_reg          = 8
            ,y_reg          = 9
            ,sp_reg         = 10
            ,status_N       = 11
            ,status_V       = 12
            ,status_B       = 13
            ,status_D       = 14
            ,status_I       = 15
            ,status_Z       = 16
            ,status_C       = 17
        }
        public static Regex CPUHistoryLine = new Regex("^([0-9a-f][0-9a-f][0-9a-z][0-9a-z])\\s*(([0-9A-F][0-9A-F] ){0,3})\\s*([A-Z][A-Z][A-Z]) (([\\$_\\.\\(\\)\\,a-zA-Z0-9#])*)\\s*- A:([0-9a-f][0-9a-f]) X:([0-9a-f][0-9a-f]) Y:([0-9a-f][0-9a-f]) SP:([0-9a-f][0-9a-f]) (N| )(V| )-(B| )(D| )(I| )(Z| )(C| )");
    }
}
