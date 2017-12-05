using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VICEPDBMonitor
{
	class C128ViewModel
	{
		public ObservableCollection<string> VICColourNames { get; private set; }

		public C128ViewModel()
		{
			VICColourNames = new ObservableCollection<string>
			{
				"Black",
				"White",
				"Red",
				"Cyan",
				"Purple",
				"Green",
				"Blue",
				"Yellow",
				"Orange",
				"Brown",
				"Pink",
				"Dark Gray",
				"Mid Gray",
				"Lt Green",
				"Lt Blue",
				"Lt Gray"
			};
		}
	}
}
