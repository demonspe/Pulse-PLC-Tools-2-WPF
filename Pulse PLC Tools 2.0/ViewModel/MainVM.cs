using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse_PLC_Tools_2._0.ViewModel
{
    public class MainVM : BindableBase
    {
        public ImpParams Imp1 { get; set; }
        public ImpParams Imp2 { get; set; }
        DeviceMainParams Device { get; set; }

        public MainVM()
        {
            Imp1 = new ImpParams(1);
            Imp2 = new ImpParams(2);
            Device = new DeviceMainParams();
        }
    }
}
