using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse_PLC_Tools_2._0
{
    interface ILink
    {
        event EventHandler<LinkRxEventArgs> DataRecieved;

        bool IsConnected { get; }               //Состояние соединения
        string ConnectionString { get; set; }   //Номер порта, телефон, IP адрес


        bool Connect();
        void Disconnect();
        void ClearBuffer();
    }

    class LinkRxEventArgs
    {
        byte[] buff;
    }
}
