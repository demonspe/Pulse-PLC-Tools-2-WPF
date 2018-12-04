using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinkLibrary
{
    public class LinkGSM : ILink, IMessage
    {
        private SerialPort Port { get; set; }
        private string ModemState { get; set; }
        public bool IsConnected { get; set; }
        public string ConnectionString { get; set; }
        public int LinkDelay { get; set; }

        public double ModemTimeout { get; set; }
        public int TryCount { get; set; }
        public int CurrentTryNumber { get; set; }
        public string PhoneNumber { get; set; }

        public string ComPort { get; set; }
        public int PortTimeout { get; set; }
        public bool IsAlive { get; set; }
        public string Vendor { get; set; }
        public int SignalStrenght { get; set; }


        public event EventHandler<LinkRxEventArgs> DataRecieved = delegate { };
        public event EventHandler<EventArgs> Connected = delegate { };
        public event EventHandler<EventArgs> Disconnected = delegate { };
        public event EventHandler<MessageDataEventArgs> Message = delegate { };


        System.Timers.Timer timer;

        public LinkGSM()
        {
            TryCount = 3;
            ModemTimeout = 45000;
            IsConnected = false;
            timer = new System.Timers.Timer();
            timer.Stop();
            CurrentTryNumber = 1;
        }


        public void ClearBuffer()
        {
            if (Port != null && Port.IsOpen)
                Port.DiscardInBuffer();
        }

        public bool Connect()
        {
            if (OpenPort())
            {
                timer.Interval = ModemTimeout;
                timer.Elapsed += Timer_Elapsed;
                timer.Start();
                if (Port.IsOpen)
                {
                    string modemMessage = "Calling " + PhoneNumber + ", try: " + CurrentTryNumber.ToString() + " of: " + TryCount.ToString();
                    Message(this, new MessageDataEventArgs() { MessageString = modemMessage, MessageType = MessageType.Normal });

                    byte[] callString = Encoding.Default.GetBytes("ATDP" + PhoneNumber + "\r");
                    Send(callString);

                    return true;
                }
                else
                    return false;
            }
            return false;

        }

        public void Disconnect()
        {
            if (OpenPort())
            {
                try
                {
                    Port.Write("AT%P");
                    Thread.Sleep(1500);

                    Port.Write("+++");
                    Thread.Sleep(1500);

                    Port.Write("ATH0\r");
                    Thread.Sleep(1500);

                    Disconnected(this, null);
                    ClosePort();

                }
                catch (Exception ex)
                {
                    Message(this, new MessageDataEventArgs { MessageString = ex.Message, MessageType = MessageType.Error });
                }
            }
        }

        public bool Send(byte[] data)
        {
            Send(data, data.Length);
            return true;
        }

        public bool Send(byte[] data, int length)
        {
            try
            {
                Thread.Sleep(250);
                Port.Write(data, 0, length);
                return true;
            }
            catch (Exception ex)
            {
                Message(this, new MessageDataEventArgs { MessageString = ex.Message, MessageType = MessageType.Error });
                return false;
            }
        }

        private bool OpenPort()
        {
            if (Port == null && ComPort != null && ComPort != string.Empty)
            {
                Port = new SerialPort(ComPort, 9600, Parity.None, 8, StopBits.One);
                Port.Encoding = Encoding.Default;
            }

            if (!Port.IsOpen)
            {
                try
                {
                    Port.Open();
                    Port.DataReceived += Port_DataReceived;
                    return true;
                }
                catch (Exception ex)
                {
                    Message(this, new MessageDataEventArgs { MessageString = ex.Message, MessageType = MessageType.Error });
                    return false;
                }
            }
            else
                return true;
        }

        private void ClosePort()
        {
            Port.Close();
            Port.DataReceived -= Port_DataReceived;
        }

        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] buf = new byte[0];
            int i = 0;

            do //Цикл 
            {
                Array.Resize<byte>(ref buf, buf.Length + 1);
                buf[i] = Convert.ToByte(Port.ReadByte());
                i++;
                if (Port.BytesToRead == 0)
                    Thread.Sleep(250);
            }
            while (Port.BytesToRead != 0);

            string strBuf = Encoding.Default.GetString(buf);
            strBuf = strBuf.Replace("\r", "");
            strBuf = strBuf.Replace("\n", "");

            timer.Stop();
            Thread.Sleep(250);

            if (strBuf.StartsWith("CON"))
            {
                IsConnected = true;
                Thread.Sleep(250);
                Connected(this, null);
                Message(this, new MessageDataEventArgs { MessageString = strBuf, MessageType = MessageType.Normal });
                CurrentTryNumber = 1;
                buf = null;
                return;
            }

            if (strBuf.StartsWith("DIA") || strBuf.StartsWith("RIN"))
            {
                Message(this, new MessageDataEventArgs { MessageString = strBuf, MessageType = MessageType.Normal });
                buf = null;
                return;
            }

            if ((strBuf.StartsWith("NO") || strBuf.StartsWith("BUS")))
            {
                Message(this, new MessageDataEventArgs { MessageString = strBuf, MessageType = MessageType.Normal });
                if (!IsConnected)
                {
                    if (CurrentTryNumber < TryCount)
                    {
                        CurrentTryNumber++;
                        ClosePort();
                        Connect();
                        return;
                    }
                    else
                    {
                        Message(this, new MessageDataEventArgs { MessageString = "Не удалось связаться с удаленным модемом", MessageType = MessageType.Error });
                        Message(this, new MessageDataEventArgs { MessageString = "Не удалось связаться с удаленным модемом", MessageType = MessageType.MsgBox });
                        CurrentTryNumber = 1;
                    }
                }
                else
                {
                    Message(this, new MessageDataEventArgs { MessageString = "Соеденение разорвано", MessageType = MessageType.Error });
                    IsConnected = false;
                    Disconnected(this, null);
                    ClosePort();
                }
                return;
            }

            if (strBuf.StartsWith("ATDP"))
            {
                Message(this, new MessageDataEventArgs { MessageString = strBuf, MessageType = MessageType.Normal });
                buf = null;
                return;
            }

            if (strBuf == "OK")
            {
                ModemState = "OK";
                return;
            }

            if (strBuf.StartsWith("ATH"))
            {
                return;
            }

            if (strBuf.StartsWith("+CSQ"))
            {
                timer.Stop();
                SignalStrenght = Convert.ToInt32(Regex.Replace(strBuf, @"[^\d]+", ""));
                SignalStrenght = -113 + (SignalStrenght / 100) * 2; // (signalStrenght * 100) / 300;
                return;
            }

            if (strBuf.StartsWith("+COPS"))
            {
                timer.Stop();
                Regex regex = new Regex(@"\D*[^,],");
                Vendor = regex.Matches(strBuf)[1].Value.Replace(",", "");
                return;
            }
            DataRecieved(this, new LinkRxEventArgs() { Buffer = buf });
            buf = null;
        }

        #region Инициализация
        public void Initialize()
        {

            ConnectionString = "Инициализация устройства " + ComPort + "...";
            Message(this, new MessageDataEventArgs { MessageString = ConnectionString, MessageType = MessageType.Normal });
            if (OpenPort())
            {
                Vendor = null;
                SignalStrenght = 0;
                timer.Elapsed += InitTimer_Elapsed;

                GetModemState();
                timer.Interval = 2000;
                timer.Start();
                ModemState = null;
                while (ModemState == null) { }

                if (ModemState == null || ModemState == "NO")
                {
                    Message(this, new MessageDataEventArgs { MessageString = "Устройство " + ComPort + " не является HAYES совместимым модемом.", MessageType = MessageType.Error });
                    return;
                }

                timer.Interval = 60000;
                GetVendorName();
                timer.Start();
                while (Vendor == null) { }

                Thread.Sleep(250);

                timer.Interval = 10000;
                GetSignalStrength();
                timer.Start();
                while (SignalStrenght == 0) { }

                ConnectionString = Vendor + "; Уровень сигнала: " + SignalStrenght.ToString() + "дБ";

                Message(this, new MessageDataEventArgs { MessageString = ConnectionString, MessageType = MessageType.Normal });
                ClosePort();
            }
        }

        private void GetSignalStrength()
        {
            Send(Encoding.Default.GetBytes("AT+CSQ\r"));
        }

        private void GetVendorName()
        {
            Send(Encoding.Default.GetBytes("AT+COPS=?\r"));
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer.Stop();
            string message = "No answer from modem in " + (timer.Interval / 1000).ToString("#.0") + " sec.";
            Message(this, new MessageDataEventArgs() { MessageString = message, MessageType = MessageType.Error });
            ClosePort();
            timer.Elapsed -= Timer_Elapsed;
        }

        private void InitTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer.Stop();
            ModemState = "NO";
            SignalStrenght = -1;
            Vendor = "No data";
        }

        public bool GetModemState()
        {
            try
            {
                Send(Encoding.Default.GetBytes("AT\r"));
                return true;
            }
            catch (Exception ex)
            {
                Message(this, new MessageDataEventArgs() { MessageString = ex.Message, MessageType = MessageType.Error });
                return false;
            }
        }
        #endregion
    }
}
