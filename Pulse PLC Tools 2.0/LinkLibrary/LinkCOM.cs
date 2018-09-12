using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LinkLibrary
{
    public class LinkCOM : ILink, IMessage
    {
        public SerialPort serialPort;
        public bool IsConnected { get { return serialPort.IsOpen; } }
        public string ConnectionString { get { return serialPort.PortName; } }
        public int LinkDelay { get; set; }
        public SerialPort Port { get { return serialPort; } }
        
        public event EventHandler<LinkRxEventArgs> DataRecieved = delegate { };
        public event EventHandler<EventArgs> Connected = delegate { };
        public event EventHandler<EventArgs> Disconnected = delegate { };
        public event EventHandler<MessageDataEventArgs> Message = delegate { };

        public LinkCOM()
        {
            serialPort = new SerialPort("COM1")
            {
                BaudRate = 9600,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One
            };
            serialPort.DataReceived += SerialPort_DataReceived;
            LinkDelay = 500;
        }
        
        public LinkCOM(string portName)
        {
            serialPort = new SerialPort(portName)
            {
                BaudRate = 9600,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One
            };
            serialPort.DataReceived += SerialPort_DataReceived;
            LinkDelay = 500;
        }

        public LinkCOM(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
            serialPort.DataReceived += SerialPort_DataReceived;
            LinkDelay = 500;
        }

        public void ClearBuffer()
        {
            if (serialPort.IsOpen)
            {
                serialPort.DiscardInBuffer();  //Очистим входной буффер
            }
        }

        public bool Connect()
        {
            if (serialPort.PortName != "")
            {
                serialPort.Encoding = Encoding.Default;
                try
                {
                    if (!serialPort.IsOpen) serialPort.Open();
                    Connected(this, new EventArgs());
                    Message(this, new MessageDataEventArgs() { MessageString = "Открыт канал связи [" + ConnectionString + "]", MessageType = MessageType.Normal });
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }

        public void Disconnect()
        {
            //Очищаем буфер
            ClearBuffer();
            //Закрываем порт
            try
            {
                serialPort.Close();
                Disconnected(this, new EventArgs());
            } catch { }
        }

        public bool Send(byte[] data) { return Send(data, data.Length); }

        public bool Send(byte[] data, int length)
        {
            if (serialPort.IsOpen)
            {
                try
                {
                    serialPort.Write(data, 0, length);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }


        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] bytes_buff = new byte[0];
            try
            {
                do
                {
                    //Получаем байт из буффера
                    Array.Resize(ref bytes_buff, bytes_buff.Length + 1);
                    bytes_buff[bytes_buff.Length - 1] = (byte)serialPort.ReadByte();

                    if (serialPort.BytesToRead == 0) Thread.Sleep(50);     //Время ожидания байта-------------------------!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                } while (serialPort.BytesToRead != 0);
                //Вызываем собитие приема данных
                DataRecieved(this, new LinkRxEventArgs() { Buffer = bytes_buff });
            }
            catch (IOException)
            {
                // Code to handle the exception goes here.
            }
        }
    }

    
}
