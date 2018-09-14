using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse_PLC_Tools_2._0.ViewModel
{
    public enum TypeOfLink { COM, TCP, GSM };

    public class LinkVM : BindableBase
    {
        private string comPortName;
        private string ipAddress;
        private string phoneNumber;

        public string COM_PortName { get => comPortName; set { comPortName = value; RaisePropertyChanged(nameof(COM_PortName)); } }
        public string IP_Address { get => ipAddress; set { ipAddress = value; RaisePropertyChanged(nameof(IP_Address)); } }
        public string PhoneNumber { get => phoneNumber; set { phoneNumber = value; RaisePropertyChanged(nameof(PhoneNumber)); } }

        private TypeOfLink selectedLinkType;
        public TypeOfLink SelectedLinkType { get => selectedLinkType; }

        public DelegateCommand<string> CommandSetLinkType { get; }

        public LinkVM()
        {
            CommandSetLinkType = new DelegateCommand<string>(str =>
            {
                selectedLinkType = TypeOfLink.COM;
                if (str == "COM") selectedLinkType = TypeOfLink.COM;
                if (str == "TCP") selectedLinkType = TypeOfLink.TCP;
                if (str == "GSM") selectedLinkType = TypeOfLink.GSM;
            });
        }
    }
}
