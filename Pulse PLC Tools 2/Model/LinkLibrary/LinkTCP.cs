using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Net.NetworkInformation;

namespace LinkLibrary
{
    public class LinkTCP : ILink, IMessage
    {
        TcpClient client;
        NetworkStream tcpStream;
        IPAddress ipAddress;
        UInt16 portNumber;
        int linkDelay;

        public string IPAddress { get => ipAddress.ToString(); set { System.Net.IPAddress.TryParse(value, out ipAddress);  } }
        public UInt16 PortNumber { get => portNumber; set { portNumber = value; } }

        public bool IsConnected { get { if (client != null) return client.Connected; else return false; } }
        public string ConnectionString { get { if (ipAddress != null) return ipAddress.ToString() + ":" + portNumber.ToString(); else return ""; } }
        public int LinkDelay { get => linkDelay; set { linkDelay = value; } }
        
        public event EventHandler<LinkRxEventArgs> DataRecieved = delegate { };
        public event EventHandler<EventArgs> Connected = delegate { };
        public event EventHandler<EventArgs> Disconnected = delegate { };
        public event EventHandler<MessageDataEventArgs> Message = delegate { };

        public LinkTCP()
        {
            IPAddress = "";
            PortNumber = 0;
            linkDelay = 10000;
        }

        public void ClearBuffer()
        {
            throw new NotImplementedException();
        }

        public bool Connect()
        {
            // Устанавливаем удаленную точку для сокета
            if (IPAddress != string.Empty)
            {
                //valid ip
                try
                {
                    // Создаем новый экземпляр TcpClient
                    client = new TcpClient();
                    Message(this, new MessageDataEventArgs() { MessageString = "Попытка подключения к " + IPAddress + ":"+PortNumber, MessageType = MessageType.Normal });
                    // Соединяемся с хостом
                    client.Connect(IPAddress, PortNumber);
                    //Открываем поток
                    tcpStream = client.GetStream();
                    //Запускаем поток для приема данных
                    ThreadPool.QueueUserWorkItem(DataRecieveHandler);
                    Message(this, new MessageDataEventArgs() { MessageString = IPAddress + ":" + PortNumber + " подключено.", MessageType = MessageType.Good });
                    //Событие при успешном подключении
                    Connected(this, new EventArgs());
                    ThreadPool.QueueUserWorkItem(PingServer);
                    return true;
                }
                catch (Exception ex)
                {
                    Message(this, new MessageDataEventArgs() { MessageString = "Не удалось подключиться к узлу. \r\n\r\n" + ex.ToString(), MessageType = MessageType.Error });
                    return false;
                }
            }
            else
            {
                //is not valid ip
                Message(this, new MessageDataEventArgs() { MessageString = "Неверный формат IP адреса или порта", MessageType = MessageType.Warning });
                return false;
            }
        }

        public void Disconnect()
        {
            client.Close();
            Disconnected(this, new EventArgs());
        }
        
        public bool Send(byte[] data, int length)
        {
            send:
            try
            {
                tcpStream.Write(data, 0, length);
                return true;
            }
            catch
            {
                //Проблемы со связью, повторная попытка подключения
                Disconnected(this, new EventArgs());
                Connect();
                goto send;
            }
        }

        public bool Send(byte[] data)
        {
            return Send(data, data.Length);
        }

        void DataRecieveHandler(object stateInfo)
        {
            while(client.Connected) //Пока работает подключение
            {
                if(tcpStream.DataAvailable) //Забираем данные если есть
                {
                    //byte[] bytes_buff = new byte[0];
                    List<byte> buffer = new List<byte>();
                    try
                    {
                        do
                        {
                            //Получаем байт из буффера
                            //Array.Resize(ref bytes_buff, bytes_buff.Length + 1);
                            //bytes_buff[bytes_buff.Length - 1] = (byte)tcpStream.ReadByte();
                            buffer.Add((byte)tcpStream.ReadByte());

                            if (!tcpStream.DataAvailable) Thread.Sleep(50);     //Время ожидания байта-------------------------!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                        } while (tcpStream.DataAvailable);
                        //Вызываем собитие приема данных
                        DataRecieved(this, new LinkRxEventArgs() { Buffer = buffer.ToArray() });
                    }
                    catch (IOException)
                    {
                        // Code to handle the exception goes here.
                    }
                }
                Thread.Sleep(200);
            }
        }

        void PingServer(object o)
        {
            Ping ping = new Ping();
            PingReply pingReply = null;

            while(IsConnected)
            {
                pingReply = ping.Send(IPAddress, LinkDelay);
                if (pingReply.Status == IPStatus.TimedOut)
                {
                    Message(this, new MessageDataEventArgs() { MessageString = "Проблемы со связью на " + IPAddress, MessageType = MessageType.Error });
                    Disconnect();
                    return;
                }
            }
            
        }
    }
}
