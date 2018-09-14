using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse_PLC_Tools_2
{
    public class DateTimeVM : BindableBase
    {
        private DateTime deviceDateTime;

        //Время прочитанное из устройства
        public DateTime DeviceDateTime { get => deviceDateTime;
            set {
                deviceDateTime = value;
                RaisePropertyChanged(nameof(DeviceDateTime));
                RaisePropertyChanged(nameof(PCDateTime));
                RaisePropertyChanged(nameof(Difference));
            }
        }
        //Время компьютера
        public DateTime PCDateTime { get => DateTime.Now; }
        //Разница в секундах
        public TimeSpan Difference { get => DeviceDateTime.Subtract(PCDateTime); }
        //Commands
        public DelegateCommand Read { get; }
        public DelegateCommand Write { get; }
        public DelegateCommand Correct { get; }

        public DateTimeVM()
        {

        }
    }
}
