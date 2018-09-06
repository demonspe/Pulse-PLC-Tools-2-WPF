using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse_PLC_Tools_2._0
{
    public interface ILink
    {
        event EventHandler<LinkRxEventArgs> DataRecieved;
        event EventHandler<EventArgs> Connected;
        event EventHandler<EventArgs> Disconnected;

        bool IsConnected { get; }          //Состояние соединения
        string ConnectionString { get; }   //Номер порта, телефон, IP адрес

        bool Send(byte[] data);             //Отправить данные в канал
        bool Send(byte[] data, int length); //Отправить данные определенной длины
        bool Connect();
        void Disconnect();
        void ClearBuffer();
    }

    public class LinkRxEventArgs : EventArgs
    {
        public byte[] Buffer { get; set; }
    }
}
