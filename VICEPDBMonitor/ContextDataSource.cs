using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VICEPDBMonitor
{
    public class ContextDataSource
    {
        public bool Enable { get; set; }
        public string Source { get; set; }
        public string Zone { get; set; }
        public string Device { get; set; }
        public bool previousEnable { get; set; }

        public string getKey()
        {
            return Source + ":" + Zone + ":" + Device;
        }
    }
}
