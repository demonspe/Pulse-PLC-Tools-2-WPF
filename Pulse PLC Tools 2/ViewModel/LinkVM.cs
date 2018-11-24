using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Pulse_PLC_Tools_2
{
    public enum TypeOfLink { COM, TCP, GSM };

    public class LinkVM : BindableBase
    {
        private string comPortName;
        private string ipAddress;
        private ushort tcpPort;
        private string phoneNumber;
        private bool isConnected;
        private string connectionInfo;

        public ObservableCollection<string> ComPortList { get; }
        public string SelectedComPort { get => comPortName; set { comPortName = value; RaisePropertyChanged(nameof(SelectedComPort)); } }
        public string IP_Address { get => ipAddress; set { ipAddress = value; RaisePropertyChanged(nameof(IP_Address)); } }
        public ushort TCP_Port { get => tcpPort; set { tcpPort = value; RaisePropertyChanged(nameof(IP_Address)); } }
        public string PhoneNumber { get => phoneNumber; set { phoneNumber = value; RaisePropertyChanged(nameof(PhoneNumber)); } }
        public TypeOfLink SelectedLinkType { get; set; }
        public Visibility VisibilityLinesGSMCOM { get => SelectedLinkType == TypeOfLink.GSM ? Visibility.Visible : Visibility.Hidden; }
        public bool IsConnected { get => isConnected;
            set
            {
                isConnected = value;
                RaisePropertyChanged(nameof(IsConnected));
                RaisePropertyChanged(nameof(ConnectIsVisible));
                RaisePropertyChanged(nameof(DisconnectIsVisible));
            }
        }
        public Visibility ConnectIsVisible { get => !isConnected ? Visibility.Visible : Visibility.Hidden; }
        public Visibility DisconnectIsVisible { get => isConnected ? Visibility.Visible : Visibility.Hidden; }
        public string ImgSrcLinkStatus { get => IsConnected? "Pics/green.png" : "Pics/red.png"; } //Image source for link status image in ToolBar (bottom rigth)
        public string ConnectionInfo { get => connectionInfo; set {
                connectionInfo = value;
                RaisePropertyChanged(nameof(ConnectionInfo));
                RaisePropertyChanged(nameof(ImgSrcLinkStatus));
            }
        }

        //Commands
        public DelegateCommand<string> CommandSetLinkType { get; }

        public LinkVM()
        {
            SelectedLinkType = TypeOfLink.COM;
            ComPortList = new ObservableCollection<string>();
            SelectedComPort = "";
            IP_Address = "192.168.1.59";
            TCP_Port = 11111;
            PhoneNumber = "89271112233";

            CommandSetLinkType = new DelegateCommand<string>(str =>
            {
                SelectedLinkType = TypeOfLink.COM;
                if (str == "COM") SelectedLinkType = TypeOfLink.COM;
                if (str == "TCP") SelectedLinkType = TypeOfLink.TCP;
                if (str == "GSM") { SelectedLinkType = TypeOfLink.GSM; }
                RaisePropertyChanged(nameof(VisibilityLinesGSMCOM));
            });
        }
    }
}
