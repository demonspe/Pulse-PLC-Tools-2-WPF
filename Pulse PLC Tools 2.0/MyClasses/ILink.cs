using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse_PLC_Tools_2._0
{
    public enum Msg_Type : int { Error, Warning, Normal, NormalBold, Good, ToolBarInfo, MsgBox }
    public enum Msg_Direction : int { Send, Receive }

    public class StringMessageEventArgs : EventArgs
    {
        public Msg_Type MessageType { get; set; }
        public string MessageString { get; set; }
    }

    public class LinkMessageEventArgs : EventArgs
    {
        public byte[] Data { get; set; }
        public int Length { get; set; }
        public Msg_Direction Direction { get; set; }
    }

    public class LinkRxEventArgs : EventArgs
    {
        public byte[] Buffer { get; set; }
    }

    public interface ILink
    {
        event EventHandler<StringMessageEventArgs> ServiceMessage; //Различные сообщения о статусе соединения, ошибках и тд
        event EventHandler<LinkRxEventArgs> DataRecieved;   
        event EventHandler<EventArgs> Connected;
        event EventHandler<EventArgs> Disconnected;

        bool IsConnected { get; }           //Состояние соединения
        string ConnectionString { get; }    //Номер порта, телефон, IP адрес
        int LinkDelay { get; set; }         //Возможная максимальная задержка возникающая в канале связи (завязана на типе канала)

        bool Send(byte[] data);             //Отправить данные в канал
        bool Send(byte[] data, int length); //Отправить данные определенной длины
        bool Connect();
        void Disconnect();
        void ClearBuffer();
    }
}
