using System;

namespace LinkLibrary
{
    public class ProtocolDataContainer
    {
        public string ProtocolName { get; set; }
        public int CommandCode { get; set; }
        public object Data { get; set; }

        public ProtocolDataContainer(string protocolName, int commandCode, object data)
        {
            ProtocolName = protocolName;
            CommandCode = commandCode;
            Data = data;
        }
    }

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
        string ProtocolName { get; }
        bool Send(int cmdCode, ILink link, object param);
        void DateRecieved(object sender, LinkRxEventArgs e);

    }
}
