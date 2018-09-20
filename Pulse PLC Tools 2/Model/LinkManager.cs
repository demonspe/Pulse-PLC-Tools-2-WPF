using LinkLibrary;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pulse_PLC_Tools_2
{
    public class LinkManager : IMessage
    {
        public ILink Link { get; set; }
        SynchronizationContext context;
        LinkVM linkViewModel;
        EventHandler<MessageDataEventArgs> messageInputHandler;

        public event EventHandler<MessageDataEventArgs> Message;

        public LinkManager(MainVM mainVM, SynchronizationContext context)
        {
            Message += mainVM.MessageInput;
            messageInputHandler = mainVM.MessageInput;
            Link = new LinkCOM();
            this.context = context;
            linkViewModel = mainVM.VM_Link;
            //Запускаем поток для сканирования COM портов
            ThreadPool.QueueUserWorkItem(Get_COM_List_Handler, linkViewModel.ComPortList);
        }

        public void OpenLink()
        {
            switch (linkViewModel.SelectedLinkType)
            {
                case TypeOfLink.COM:
                    if (linkViewModel.SelectedComPort == null || linkViewModel.SelectedComPort == string.Empty)
                    {
                        Message(this, new MessageDataEventArgs() { MessageString = "Не выбран COM порт", MessageType = MessageType.Warning });
                        return;
                    }
                    Link = new LinkCOM(linkViewModel.SelectedComPort);
                    ((LinkCOM)Link).Message += messageInputHandler;
                    Link.Connected += Link_Connected;
                    Link.Disconnected += Link_Disconnected;
                    Link.Connect();
                    break;
                case TypeOfLink.TCP:
                    break;
                case TypeOfLink.GSM:
                    if (linkViewModel.SelectedComPort == null || linkViewModel.SelectedComPort == string.Empty)
                    {
                        Message(this, new MessageDataEventArgs() { MessageString = "Не выбран COM порт", MessageType = MessageType.Warning });
                        return;
                    }
                    Link = new LinkGSM(linkViewModel.SelectedComPort, 20000);
                    ((LinkGSM)Link).PhoneNumber = linkViewModel.PhoneNumber;
                    ((LinkGSM)Link).Message += messageInputHandler;
                    Link.Connected += Link_Connected;
                    Link.Connect();
                    break;
                default:
                    break;
            }
        }

        private void Link_Disconnected(object sender, EventArgs e)
        {
            linkViewModel.IsConnected = false;
        }

        private void Link_Connected(object sender, EventArgs e)
        {
            linkViewModel.IsConnected = true;
        }

        public void CloseLink()
        {
            Link?.Disconnect();
        }

        //Чтение списка COM портов в системe
        void Get_COM_List_Handler(object ComPortList)
        {
            while (true)
            {
                string[] myPortList = System.IO.Ports.SerialPort.GetPortNames();
                context.Send((list) => {
                    ((ObservableCollection<string>)list).Clear();
                    myPortList.ToList().ForEach(item => ((ObservableCollection<string>)list).Add(item));
                }, ComPortList);

                Thread.Sleep(500);  //Проверка каналов каждые 500 мс
            }
        }
    }
}
