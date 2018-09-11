using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.IO.Ports;
using System.Text.RegularExpressions;

namespace Pulse_PLC_Tools_2._0
{
    public class LinkGSM : ILink
    {
        public bool IsConnected { get { return isConnected; } }
        public string ConnectionString { get { return conString; } }
        public double ModemTimeout { get { return timer.Interval; } set { timer.Interval = value; } }
        public int TryCount { get { return tryCount; } set { tryCount = value; } }
        public string PhoneNumber { get { return phoneNumber; } set { phoneNumber = value; } }
        public int LinkDelay { get { return linkTimeout; } set { linkTimeout = value; } }

        public event EventHandler<LinkRxEventArgs> DataRecieved;
        public event EventHandler<EventArgs> Connected;
        public event EventHandler<EventArgs> Disconnected;
        public event EventHandler<StringMessageEventArgs> ServiceMessage;

        private int linkTimeout = 5000;
        private SerialPort port;
        private string phoneNumber;
        private bool isConnected;
        private int tryCount = 3;
        private int currentTryCount = 1;
        private string conString;
        private int signalStrenght = 0;
        private string vendor = null;

        System.Timers.Timer timer;
        System.Timers.Timer initTimer = new System.Timers.Timer(10000);

        public LinkGSM(string comPort, double modemTimeout)
        {
            port = new SerialPort(comPort, 9600, Parity.None, 8, StopBits.One);
            port.DataReceived += Port_DataReceived;
            timer = new System.Timers.Timer(modemTimeout);
            timer.Stop();
            initTimer.Stop();
            timer.Elapsed += Timer_Elapsed;
            port.Encoding = Encoding.Default;
            port.Open();
            isConnected = false;
            conString = port.PortName;
        }

        public void ClearBuffer()
        {
            Thread.Sleep(250);
            port.DiscardInBuffer();
        }

        #region Соеденение
        public bool Connect()
        {
            timer.Start();
            if (port.IsOpen)
            {
                string modemMessage = "Calling " + phoneNumber + ", try: " + currentTryCount.ToString() + " of: " + tryCount.ToString();
                ServiceMessage(this, new StringMessageEventArgs() { MessageString = modemMessage, MessageType = Msg_Type.Normal });

                byte[] callString = Encoding.Default.GetBytes("ATDP" + phoneNumber + "\r");
                Send(callString);

                return true;
            }
            else
                return false;
        }

        public void Disconnect()
        {
            try
            {
                port.Write("AT%P");
                Thread.Sleep(1500);

                port.Write("+++");
                Thread.Sleep(1500);

                port.Write("ATH0\r");
                Thread.Sleep(1500);

                Disconnected(this, null);
            }
            catch (Exception ex)
            {
                byte[] message = Encoding.Default.GetBytes(ex.Message);
                DataRecieved(this, new LinkRxEventArgs() { Buffer = message });
            }
        }
        #endregion

        #region Отправка пакетов
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
                port.Write(data, 0, length);
                return true;
            }
            catch (Exception ex)
            {
                ServiceMessage(this, new StringMessageEventArgs { MessageString = ex.Message, MessageType = Msg_Type.Error });
                return false;
            }
        }
        #endregion

        #region Обработка входящих пакетов
        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] buf = new byte[0];
            int i = 0;

            do //Цикл 
            {
                Array.Resize<byte>(ref buf, buf.Length + 1);
                buf[i] = Convert.ToByte(port.ReadByte());
                i++;
                if (port.BytesToRead == 0)
                    Thread.Sleep(250);
            }
            while (port.BytesToRead != 0);

            string strBuf = Encoding.Default.GetString(buf);
            strBuf = strBuf.Replace("\r", "");
            strBuf = strBuf.Replace("\n", "");

            timer.Stop();
            Thread.Sleep(250);

            if (strBuf.StartsWith("CON"))
            {
                isConnected = true;
                Connected(this, null);
                ServiceMessage(this, new StringMessageEventArgs { MessageString = strBuf, MessageType = Msg_Type.Normal });
                currentTryCount = 1;
                buf = null;
                return;
            }

            if (strBuf.StartsWith("DIA") || strBuf.StartsWith("RIN"))
            {
                ServiceMessage(this, new StringMessageEventArgs { MessageString = strBuf, MessageType = Msg_Type.Normal });
                buf = null;
                return;
            }

            if ((strBuf.StartsWith("NO") || strBuf.StartsWith("BUS")))
            {
                ServiceMessage(this, new StringMessageEventArgs { MessageString = strBuf, MessageType = Msg_Type.Normal });
                if (!isConnected)
                {
                    if (currentTryCount <= tryCount)
                    {
                        currentTryCount++;
                        Connect();
                        return;
                    }
                    else
                    {

                    }
                }
                else
                {
                    Disconnected(this, null);
                }
            }

            if (strBuf.StartsWith("ATDP"))
            {
                ServiceMessage(this, new StringMessageEventArgs { MessageString = strBuf, MessageType = Msg_Type.Normal });
                buf = null;
                return;
            }

            if (strBuf == "OK")
            {
                return;
            }

            if (strBuf.StartsWith("ATH"))
            {
                return;
            }

            if (strBuf.StartsWith("+CSQ"))
            {
                initTimer.Stop();
                signalStrenght = Convert.ToInt32(Regex.Replace(strBuf, @"[^\d]+", ""));
                signalStrenght = (signalStrenght * 100) / 300;
                return;
            }

            if (strBuf.StartsWith("+COPS"))
            {
                initTimer.Stop();
                Regex regex = new Regex(@"\D*[^,],");
                vendor = regex.Matches(strBuf)[1].Value.Replace(",", "");
                return;
            }

            else
            {
                DataRecieved(this, new LinkRxEventArgs() { Buffer = buf });
            }

            buf = null;
        }
        #endregion

        #region Инициализация модема
        public void Initialize()
        {
            vendor = null;
            signalStrenght = 0;

            initTimer.Elapsed += InitTimer_Elapsed;
            initTimer.Interval = 40000;
            GetVendorName();
            initTimer.Start();
            while (vendor == null) { }

            Thread.Sleep(250);

            initTimer.Interval = 10000;
            GetSignalStrength();
            initTimer.Start();
            while (signalStrenght == 0) { }

            conString += "; " + vendor + "; Уровень сигнала: " + signalStrenght.ToString() + "%";

            ServiceMessage(this, new StringMessageEventArgs { MessageString = conString, MessageType = Msg_Type.Normal });

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
            byte[] message = Encoding.Default.GetBytes("No answer from modem in " + (timer.Interval / 1000).ToString("#.0") + " sec.");
            DataRecieved(this, new LinkRxEventArgs() { Buffer = message });
        }

        private void InitTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            initTimer.Stop();

            signalStrenght = -1;
            vendor = "No data";
        }
        #endregion
    }
}
 