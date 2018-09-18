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
    public class LinkManager
    {
        public ILink Link { get; set; }
        SynchronizationContext context;
        LinkVM linkViewModel;

        public LinkManager(LinkVM viewModel, SynchronizationContext context)
        {
            Link = new LinkCOM();
            this.context = context;
            linkViewModel = viewModel;
            //Запускаем поток для сканирования COM портов
            ThreadPool.QueueUserWorkItem(Get_COM_List_Handler, linkViewModel.ComPortList);
        }

        public void OpenLink()
        {
            switch (linkViewModel.SelectedLinkType)
            {
                case TypeOfLink.COM:
                    Link = new LinkCOM(linkViewModel.SelectedComPort);
                    ((LinkCOM)Link).Message += Service_Message;
                    Link.Connected += Link_Connected;
                    Link.Disconnected += Link_Disconnected;
                    Link.Connect();
                    break;
                case TypeOfLink.TCP:
                    break;
                case TypeOfLink.GSM:
                    Link = new LinkGSM(linkViewModel.SelectedComPort, 30000);
                    ((LinkGSM)Link).PhoneNumber = linkViewModel.PhoneNumber;
                    ((LinkGSM)Link).Message += Service_Message;
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
            Link.Disconnect();
        }

        private void Service_Message(object sender, MessageDataEventArgs e)
        {

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
