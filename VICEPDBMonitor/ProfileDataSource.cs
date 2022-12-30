using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VICEPDBMonitor
{
    class ProfileDataSource
    {
        public string Address { get; set; }
        public string Label { get; set; }
        public int Calls { get; set; }
        public int Cycles { get; set; }
        public double CyclesPerCall { get; set; }
    }
}
