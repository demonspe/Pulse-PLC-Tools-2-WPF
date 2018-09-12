using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinkLibrary
{
    public enum MessageType : int { SendBytes, ReceiveBytes, Error, Warning, Normal, NormalBold, Good, ToolBarInfo, MsgBox }

    public class MessageDataEventArgs : EventArgs
    {
        private byte[] bytes;
        private string str;

        public string MessageString
        {
            get { return str; }
            set
            {
                str = value;
                bytes = Encoding.Default.GetBytes(str);
            }
        }

        public byte[] Data
        {
            get { return bytes; }
            set
            {
                bytes = value;
                str = Encoding.Default.GetString(bytes);
            }
        }
        public MessageType MessageType { get; set; }
        public int Length { get; set; }
    }

    interface IMessage
    {
        event EventHandler<MessageDataEventArgs> Message; //Различные сообщения о статусе соединения, ошибках и тд
    }
}
