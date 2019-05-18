using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VICEPDBMonitor
{
    public class AssertDataSource
    {
        public bool Enable { get; set; }
        public int Number { get; set; }
        public int Address { get; set; }
        public string Label { get; set; }
        public string Condition { get; set; }
        public string Msg { get; set; }

        public string NumberString { get { return Number.ToString(); } }
        public string AddressString { get { return Address.ToString("X4"); } }
    }
}
