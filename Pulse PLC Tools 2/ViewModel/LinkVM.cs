using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse_PLC_Tools_2
{
    public enum TypeOfLink { COM, TCP, GSM };

    public class LinkVM : BindableBase
    {
        private string comPortName;
        private string ipAddress;
        private ushort tcpPort;
        private string phoneNumber;
        
        public string COM_PortName { get => comPortName; set { comPortName = value; RaisePropertyChanged(nameof(COM_PortName)); } }
        public string IP_Address { get => ipAddress; set { ipAddress = value; RaisePropertyChanged(nameof(IP_Address)); } }
        public ushort TCP_Port { get => tcpPort; set { tcpPort = value; RaisePropertyChanged(nameof(IP_Address)); } }
        public string PhoneNumber { get => phoneNumber; set { phoneNumber = value; RaisePropertyChanged(nameof(PhoneNumber)); } }
        public TypeOfLink SelectedLinkType { get; set; }
        
        //Commands
        public DelegateCommand<string> CommandSetLinkType { get; }
        public DelegateCommand CommandOpenLink { get; }
        public DelegateCommand CommandCloseLink { get; }

        public LinkVM()
        {
            COM_PortName = "";
            IP_Address = "192.168.1.59";
            TCP_Port = 11111;
            PhoneNumber = "89271112233";

            CommandSetLinkType = new DelegateCommand<string>(str =>
            {
                SelectedLinkType = TypeOfLink.COM;
                if (str == "COM") SelectedLinkType = TypeOfLink.COM;
                if (str == "TCP") SelectedLinkType = TypeOfLink.TCP;
                if (str == "GSM") SelectedLinkType = TypeOfLink.GSM;
            });
        }
    }
}
