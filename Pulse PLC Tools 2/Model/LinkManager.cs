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
        MainVM mainVM;
        EventHandler<MessageDataEventArgs> messageInputHandler;

        public event EventHandler<MessageDataEventArgs> Message;

        public LinkManager(MainVM mainVM, SynchronizationContext context)
        {
            this.mainVM = mainVM;
            Message += mainVM.MessageInput;
            messageInputHandler = mainVM.MessageInput;
            Link = new LinkCOM();
            this.context = context;
            linkViewModel = mainVM.VM_Link;
            //Запускаем поток для сканирования COM портов
            ThreadPool.QueueUserWorkItem(Get_COM_List_Handler, linkViewModel);
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
                    Link.Connected += mainVM.Link_Connected;
                    Link.Disconnected += mainVM.Link_Disconnected;
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
                    Link.Connected += mainVM.Link_Connected;
                    Link.Disconnected += mainVM.Link_Disconnected;
                    Link.Connect();
                    break;
                default:
                    break;
            }
        }
        
        public void CloseLink()
        {
            Link?.Disconnect();
        }

        //Чтение списка COM портов в системe
        void Get_COM_List_Handler(object link_VM)
        {
            while (true)
            {
                string[] myPortList = System.IO.Ports.SerialPort.GetPortNames();
                context.Send((linkVM) => {
                    string selectedComPort = ((LinkVM)linkVM).SelectedComPort;
                    ((LinkVM)linkVM).ComPortList.Clear();
                    myPortList.ToList().ForEach(item => ((LinkVM)linkVM).ComPortList.Add(item));
                    ((LinkVM)linkVM).SelectedComPort = selectedComPort;
                    if (((LinkVM)linkVM).ComPortList.Count > 0 && ((LinkVM)linkVM).SelectedComPort == string.Empty)
                        ((LinkVM)linkVM).SelectedComPort = ((LinkVM)linkVM).ComPortList[0];
                    if (((LinkVM)linkVM).ComPortList.Count == 0) ((LinkVM)linkVM).SelectedComPort = "";
                }, link_VM);

                Thread.Sleep(500);  //Проверка каналов каждые 500 мс
            }
        }
    }
}
