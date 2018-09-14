using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinkLibrary
{
    public class ProtocolEventArgs : EventArgs
    {
        public object DataObject { get; set; }
        public bool Status { get; }
        public ProtocolEventArgs(bool status)
        {
            Status = status;
        }
    }

    public interface IProtocol
    {
        event EventHandler<ProtocolEventArgs> CommandEnd; //Завершение команды (удачное или нет) если нет, то повтор команды

        bool Send(int cmdCode, ILink link, object param);
        void DateRecieved(object sender, LinkRxEventArgs e);

    }
}
