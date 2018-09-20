using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using LinkLibrary;


namespace Pulse_PLC_Tools_2
{
    
    public enum PLC_Request : int { PLCv1, Time_Synchro, Serial_Num, E_Current, E_Start_Day } //Добавить начало месяца и тд
    public enum Journal_type : int { POWER = 1, CONFIG, INTERFACES, REQUESTS }
    public enum ImpNum : int { IMP1 = 1, IMP2 }

    public enum AccessType : int { No_Access, Read, Write }

    public class PulsePLCv2LoginPass
    {
        public byte[] Serial { get; }
        public byte[] Pass { get; }
        public string SerialString { get; }
        public string PassString { get; }

        public PulsePLCv2LoginPass(byte[] serialNum, byte[] pass)
        {
            //Подгоняем под нужный формат
            Serial = new byte[4] { 0, 0, 0, 0 };
            for (int i = 0; i < serialNum.Length; i++)
            {
                if (i < 4)
                    Serial[i] = serialNum[i];
                else break;
            }
            Pass = new byte[6] { 255, 255, 255, 255, 255, 255 };
            for (int i = 0; i < pass.Length; i++)
            {
                if (i < 6)
                    Pass[i] = pass[i];
                else break;
            }
            //В виде строк
            SerialString = Serial[0].ToString("00") + Serial[1].ToString("00") + Serial[2].ToString("00") + Serial[3].ToString("00");
            PassString = Encoding.Default.GetString(Pass).Trim(Convert.ToChar(255));
        }
    }

    public class ImpParamsForProtocol
    {
        public ImpParams Imp { get; }
        public ImpNum Num { get; }

        public ImpParamsForProtocol(ImpParams imp, ImpNum num)
        {
            Imp = imp;
            Num = num;
        }
    }

    public class ImpExParamsForProtocol
    {
        public ImpExParams Imp { get; }
        public ImpNum Num { get; }

        public ImpExParamsForProtocol(ImpExParams imp, ImpNum num)
        {
            Imp = imp;
            Num = num;
        }
    }

    public class ImpExParams
    {

    }

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

    public class PulsePLCv2Protocol : IProtocol, IMessage
    {
        public string ProtocolName { get => "PulsePLCv2"; }
        ProtocolDataContainer DataContainer { get; set; }
        //Комманды посылаемые на устройство
        public enum Commands : int
        {
            None,
            Check_Pass,
            Close_Session,
            EEPROM_Burn,
            Reboot,
            Clear_Errors,
            Search_Devices,
            Read_Journal,
            Read_DateTime,
            Write_DateTime,
            Read_Main_Params,
            Write_Main_Params,
            Read_IMP,
            Read_IMP_extra,
            Write_IMP,
            Read_PLC_Table,
            Read_PLC_Table_En,
            Read_E_Data,
            Write_PLC_Table,
            Read_E_Current,
            Read_E_Start_Day,
            Read_E_Month,
            Request_PLC,
            EEPROM_Read_Byte,
            Pass_Write,
            SerialWrite,
            Bootloader
        }

        //Пришел ответ на запрос
        public event EventHandler<ProtocolEventArgs> CommandEnd = delegate { };
        public event EventHandler<MessageDataEventArgs> Message = delegate { }; //IMessage
        public event EventHandler AccessEnd = delegate { };

        //Выполняемая сейчас команда
        Commands CurrentCommand { get; set; }
        //Текущий канал
        ILink CurrentLink { get; set; }
        //Доступ к выполнению команд на устройстве
        AccessType access = AccessType.No_Access;

        //Таймеры
        DispatcherTimer timer_Timeout;
        DispatcherTimer timer_Access;
        //Время отправки запроса (для подсчета времени ответа)
        Stopwatch ping = new Stopwatch();

        //Буффер для передачи
        byte[] TxBytes = new byte[512];
        int tx_len;
        //Буффер для приема
        byte[] Rx_Bytes = new byte[0];

        //Коды команд и их символьные представления для отправки на устройство
        private Dictionary<Commands, string> CommandCodes;
        private Dictionary<Commands, int> CommandTimeout;

        public PulsePLCv2Protocol()
        {
            InitCommandCodes();
            InitCommandTimeout();

            //Ограничивает время ожидания ответа
            timer_Timeout = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 10) };
            timer_Timeout.Tick += Timer_Timeout_Tick;
            //Показывает есть ли доступ к устройству
            timer_Access = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 30000) };
            timer_Access.Tick += Timer_Access_Tick;
        }

        void InitCommandCodes()
        {
            CommandCodes = new Dictionary<Commands, string>();
            CommandTimeout = new Dictionary<Commands, int>();
            //---Заполним коды команд---
            //Доступ
            CommandCodes.Add(Commands.Check_Pass, "Ap");
            CommandCodes.Add(Commands.Close_Session, "Ac");
            //Системные
            CommandCodes.Add(Commands.Bootloader, "Su");
            CommandCodes.Add(Commands.SerialWrite, "Ss");
            CommandCodes.Add(Commands.EEPROM_Read_Byte, "Se");
            CommandCodes.Add(Commands.EEPROM_Burn, "Sb");
            CommandCodes.Add(Commands.Clear_Errors, "Sc");
            CommandCodes.Add(Commands.Reboot, "Sr");
            CommandCodes.Add(Commands.Request_PLC, "SR");
            //Чтение
            CommandCodes.Add(Commands.Search_Devices, "RL");
            CommandCodes.Add(Commands.Read_Journal, "RJ");
            CommandCodes.Add(Commands.Read_DateTime, "RT");
            CommandCodes.Add(Commands.Read_Main_Params, "RM");
            CommandCodes.Add(Commands.Read_IMP, "RI");
            CommandCodes.Add(Commands.Read_IMP_extra, "Ri");
            CommandCodes.Add(Commands.Read_PLC_Table, "RP");
            CommandCodes.Add(Commands.Read_PLC_Table_En, "RP");
            CommandCodes.Add(Commands.Read_E_Current, "REc");
            CommandCodes.Add(Commands.Read_E_Start_Day, "REd");
            CommandCodes.Add(Commands.Read_E_Month, "REm");
            //Запись
            CommandCodes.Add(Commands.Write_DateTime, "WT");
            CommandCodes.Add(Commands.Write_Main_Params, "WM");
            CommandCodes.Add(Commands.Write_IMP, "WI");
            CommandCodes.Add(Commands.Write_PLC_Table, "WP");
            CommandCodes.Add(Commands.Pass_Write, "Wp");
            //-------------
        }
        void InitCommandTimeout()
        {
            CommandTimeout = new Dictionary<Commands, int>();
            //---Заполним коды команд---
            //Доступ
            CommandTimeout.Add(Commands.Check_Pass,         100);
            CommandTimeout.Add(Commands.Close_Session,      100);
            //Системные
            CommandTimeout.Add(Commands.Bootloader,         100);
            CommandTimeout.Add(Commands.SerialWrite,        200);
            CommandTimeout.Add(Commands.EEPROM_Read_Byte,   100);
            CommandTimeout.Add(Commands.EEPROM_Burn,        100);
            CommandTimeout.Add(Commands.Clear_Errors,       100);
            CommandTimeout.Add(Commands.Reboot,             100);
            CommandTimeout.Add(Commands.Request_PLC,        15000);
            //Чтение
            CommandTimeout.Add(Commands.Search_Devices,     500);
            CommandTimeout.Add(Commands.Read_Journal,       200);
            CommandTimeout.Add(Commands.Read_DateTime,      100);
            CommandTimeout.Add(Commands.Read_Main_Params,   100);
            CommandTimeout.Add(Commands.Read_IMP,           100);
            CommandTimeout.Add(Commands.Read_IMP_extra,     100);
            CommandTimeout.Add(Commands.Read_PLC_Table,     200);
            CommandTimeout.Add(Commands.Read_PLC_Table_En,  200);
            CommandTimeout.Add(Commands.Read_E_Current,     100);
            CommandTimeout.Add(Commands.Read_E_Start_Day,   100);
            CommandTimeout.Add(Commands.Read_E_Month,       100);
            //Запись
            CommandTimeout.Add(Commands.Write_DateTime,     500);
            CommandTimeout.Add(Commands.Write_Main_Params,  500);
            CommandTimeout.Add(Commands.Write_IMP,          500);
            CommandTimeout.Add(Commands.Write_PLC_Table,    2000);
            CommandTimeout.Add(Commands.Pass_Write,         500);
            //-------------
        }

        public bool Send(int cmdCode, ILink link, object param)
        {
            if (link == null) return false;
            if (!link.IsConnected) return false;
            //Установим канал и команду
            CurrentLink = link;
            CurrentCommand = (Commands)cmdCode;

            if (CurrentCommand == Commands.Check_Pass)     return CMD_Check_Pass((PulsePLCv2LoginPass)param);
            if (CurrentCommand == Commands.Close_Session)  return CMD_Close_Session();
            if (CurrentCommand == Commands.Search_Devices) return CMD_Search_Devices();
            //Доступ - Чтение
            if (access != AccessType.Read && access != AccessType.Write) {
                Message(this, new MessageDataEventArgs() { MessageType = MessageType.MsgBox, MessageString = "Нет доступа к данным устройства. Сначала авторизуйтесь." }); return false; }
            if (CurrentCommand == Commands.Read_Journal)       return CMD_Read_Journal((Journal_type)param);
            if (CurrentCommand == Commands.Read_DateTime)      return CMD_Read_DateTime();
            if (CurrentCommand == Commands.Read_Main_Params)   return CMD_Read_Main_Params();
            if (CurrentCommand == Commands.Read_IMP)           return CMD_Read_Imp_Params((ImpNum)param);
            if (CurrentCommand == Commands.Read_IMP_extra)     return CMD_Read_Imp_Extra_Params((ImpNum)param);
            if (CurrentCommand == Commands.Read_PLC_Table || CurrentCommand == Commands.Read_PLC_Table_En || CurrentCommand == Commands.Read_E_Data) return CMD_Read_PLC_Table((byte[])param);
            if (CurrentCommand == Commands.Read_E_Current)     return CMD_Read_E_Current((byte)param);
            if (CurrentCommand == Commands.Read_E_Start_Day)   return CMD_Read_E_Start_Day((byte)param);
            //Доступ - Запись
            if (access != AccessType.Write) { Message(this, new MessageDataEventArgs() { MessageType = MessageType.MsgBox, MessageString = "Нет доступа к записи параметров на устройство." }); return false; }
            if (CurrentCommand == Commands.Bootloader)         return CMD_BOOTLOADER();
            if (CurrentCommand == Commands.SerialWrite)        return CMD_SerialWrite((PulsePLCv2LoginPass)param);
            if (CurrentCommand == Commands.Pass_Write)         return CMD_Pass_Write((DeviceMainParams)param);
            if (CurrentCommand == Commands.EEPROM_Burn)        return CMD_EEPROM_BURN();
            if (CurrentCommand == Commands.EEPROM_Read_Byte)   return CMD_EEPROM_Read_Byte((UInt16)param);
            if (CurrentCommand == Commands.Reboot)             return CMD_Reboot();
            if (CurrentCommand == Commands.Clear_Errors)       return CMD_Clear_Errors();
            if (CurrentCommand == Commands.Write_DateTime)     return CMD_Write_DateTime();
            if (CurrentCommand == Commands.Write_Main_Params)  return CMD_Write_Main_Params((DeviceMainParams)param);
            if (CurrentCommand == Commands.Write_IMP)          return CMD_Write_Imp_Params((ImpParamsForProtocol)param);
            if (CurrentCommand == Commands.Write_PLC_Table)    return CMD_Write_PLC_Table((byte[])param);
            if (CurrentCommand == Commands.Request_PLC)        return CMD_Request_PLC((byte[])param);
            return false;
        }

        int Handle(byte[] rxBytes, int length)
        {
            if(rxBytes[0] == 0 &&  rxBytes[1] == 'P' && rxBytes[2] == 'l' && rxBytes[3] == 's' )
            {
                byte CMD_Type = rxBytes[4];
                byte CMD_Name = rxBytes[5];

                //Доступ
                if (CMD_Type == 'A')
                {
                    if (CMD_Name == 'p' && CurrentCommand == Commands.Check_Pass) { CMD_Check_Pass(rxBytes); return 0; }
                }
                //Системные команды
                if (CMD_Type == 'S')
                {
                    if (CMD_Name == 'u' && CurrentCommand == Commands.Bootloader) { CMD_BOOTLOADER(); return 0; }
                    if (CMD_Name == 's' && CurrentCommand == Commands.SerialWrite) { CMD_SerialWrite(rxBytes); return 0; }
                    if (CMD_Name == 'r' && CurrentCommand == Commands.Reboot) { CMD_Reboot(); return 0; }
                    if (CMD_Name == 'b' && CurrentCommand == Commands.EEPROM_Burn) { CMD_EEPROM_BURN(); return 0; }
                    if (CMD_Name == 'e' && CurrentCommand == Commands.EEPROM_Read_Byte) { CMD_EEPROM_Read_Byte(rxBytes); return 0; }
                    if (CMD_Name == 'c' && CurrentCommand == Commands.Clear_Errors) { CMD_Clear_Errors(); return 0; }
                    if (CMD_Name == 'R' && CurrentCommand == Commands.Request_PLC) { CMD_Request_PLC(rxBytes, length); return 0; }
                }
                //Команды чтения
                if (CMD_Type == 'R')
                {
                    //Поиск устройств
                    if (CMD_Name == 'L' && CurrentCommand == Commands.Search_Devices) { if (CMD_Search_Devices(rxBytes)) return 0; else return 1; }
                    //Чтение журнала
                    if (CMD_Name == 'J' && CurrentCommand == Commands.Read_Journal) { CMD_Read_Journal(rxBytes); return 0; }
                    //Чтение времени
                    if (CMD_Name == 'T' && CurrentCommand == Commands.Read_DateTime) { CMD_Read_DateTime(rxBytes); return 0; }
                    //Чтение основных параметров
                    if (CMD_Name == 'M' && CurrentCommand == Commands.Read_Main_Params) { CMD_Read_Main_Params(rxBytes); return 0; }
                    //Чтение основных параметров
                    if (CMD_Name == 'I' && CurrentCommand == Commands.Read_IMP) { CMD_Read_Imp_Params(rxBytes); return 0; }
                    //Чтение основных параметров
                    if (CMD_Name == 'i' && CurrentCommand == Commands.Read_IMP_extra) { CMD_Read_Imp_Extra_Params(rxBytes); return 0; }
                    //Чтение таблицы PLC - Активные адреса
                    if (CMD_Name == 'P' && (CurrentCommand == Commands.Read_PLC_Table ||
                                            CurrentCommand == Commands.Read_PLC_Table_En ||
                                            CurrentCommand == Commands.Read_E_Data)) { CMD_Read_PLC_Table(rxBytes); return 0; }
                    //Чтение Показаний - Активные адреса
                    if (CMD_Name == 'E') {
                        if (rxBytes[6] == 'c' && CurrentCommand == Commands.Read_E_Current) { CMD_Read_E(rxBytes); return 0; }
                        if (rxBytes[6] == 'd' && CurrentCommand == Commands.Read_E_Start_Day) { CMD_Read_E(rxBytes); return 0; }
                    }
                }
                //Команды записи
                if (CMD_Type == 'W')
                {
                    if (CMD_Name == 'p' && CurrentCommand == Commands.Pass_Write) { CMD_Pass_Write(rxBytes); return 0; }
                    //Запись времени
                    if (CMD_Name == 'T' && CurrentCommand == Commands.Write_DateTime) { CMD_Write_DateTime(rxBytes); return 0; }
                    //Запись основных параметров
                    if (CMD_Name == 'M' && CurrentCommand == Commands.Write_Main_Params) { CMD_Write_Main_Params(rxBytes); return 0; }
                    //Запись параметров импульсных входов
                    if (CMD_Name == 'I' && CurrentCommand == Commands.Write_IMP) { CMD_Write_Imp_Params(rxBytes); return 0; }
                    //Запись таблицы PLC
                    if (CMD_Name == 'P' && CurrentCommand == Commands.Write_PLC_Table) { CMD_Write_PLC_Table(rxBytes); return 0; }
                }
            }
            return 2;
            
            //return
            //0 - обработка успешна (конец)
            //1 - оработка успешна (ожидаем следующее сообщение)
            //2 - неверный формат (нет такого кода команды)
        }

        public void DateRecieved(object sender, LinkRxEventArgs e)
        {
            //Забираем данные
            for (int i = 0; i < e.Buffer.Length; i++)
            {
                Array.Resize<byte>(ref Rx_Bytes, Rx_Bytes.Length + 1);
                Rx_Bytes[Rx_Bytes.Length - 1] = e.Buffer[i];
                if (CRC16.ComputeChecksum(Rx_Bytes, Rx_Bytes.Length) == 0) break;
            }

            //Проверяем CRC16
            if (CRC16.ComputeChecksum(Rx_Bytes, Rx_Bytes.Length) == 0)
            {
                int handle_code = Handle(Rx_Bytes, Rx_Bytes.Length);

                //Комманда выполнена успешно
                if (handle_code == 0)
                {
                    Message(this, new MessageDataEventArgs() { Data = Rx_Bytes, Length = Rx_Bytes.Length, MessageType = MessageType.ReceiveBytes });
                    Request_End(true);
                    //Обновляем таймер доступа (в устройстве он обновляется при получении команды по интерфейсу)
                    timer_Access.Stop();
                    timer_Access.Start();

                    return;
                }

                //Получилось обработать и ждем следующую часть сообщения
                if (handle_code == 1)
                {
                    Message(this, new MessageDataEventArgs() { Data = Rx_Bytes, Length = Rx_Bytes.Length, MessageType = MessageType.ReceiveBytes });
                    //ping_ms = 0;
                    return;
                }

                //Не верный формат сообщения
                if(handle_code == 2)
                {
                    //Отправим в Log окно 
                    Message(this, new MessageDataEventArgs() { Data = Rx_Bytes, Length = Rx_Bytes.Length, MessageType = MessageType.ReceiveBytes });
                    Message(this, new MessageDataEventArgs() { MessageType = MessageType.Error, MessageString = "Неверный формат ответа" });
                    Message(this, new MessageDataEventArgs() { MessageType = MessageType.ToolBarInfo, MessageString = "Неверный формат ответа. Попробуйте еще раз." });
                    Request_End(false);
                }
            }
            else //CRC16 != 0
            {
                //Request_End(false);

                //Отправим в Log окно 
                Message(this, new MessageDataEventArgs() { Data = Rx_Bytes, Length = Rx_Bytes.Length, MessageType = MessageType.ReceiveBytes });
                Message(this, new MessageDataEventArgs() { MessageType = MessageType.Error, MessageString = "Неверная контрольная сумма" });
                Message(this, new MessageDataEventArgs() { MessageType = MessageType.ToolBarInfo, MessageString = "Неверная контрольная сумма. Попробуйте еще раз." });
            }
        }

        private void Timer_Timeout_Tick(object sender, EventArgs e)
        {
            //Если ожидали данных но не дождались
            if (CurrentCommand != Commands.None)
            {
                if (CurrentCommand == Commands.Close_Session) { Request_End(true); return; }
                if (CurrentCommand == Commands.Search_Devices) { Request_End(true); return; }
                Message(this, new MessageDataEventArgs() { MessageType = MessageType.Error, MessageString = "Истекло время ожидания ответа" });
                //Комманда не выполнилась
                Request_End(false);
            }
        }

        private void Timer_Access_Tick(object sender, EventArgs e)
        {
            access = AccessType.No_Access;
            timer_Access.Stop();
            AccessEnd(this, null);
        }

        //Выставить флаги о том что запрос отправлен
        private bool Request_Start() { return Request_Start(null); }
        private bool Request_Start(object prepareDataContainer)
        {
            //Запускаем таймер ожидания ответа
            timer_Timeout.Stop();
            timer_Timeout.Interval = new TimeSpan(0, 0, 0, 0, CurrentLink.LinkDelay + CommandTimeout[CurrentCommand]);
            timer_Timeout.Start();
            //Добавим контрольную сумму
            Add_Tx(CRC16.ComputeChecksumBytes(TxBytes, tx_len));
            //Время отправки запроса
            ping.Restart();
            //Подготовим данные для передачи во View
            DataContainer = new ProtocolDataContainer(ProtocolName, (int)CurrentCommand, prepareDataContainer);
            if (CurrentLink.Send(TxBytes, tx_len))
            {
                Message(this, new MessageDataEventArgs() { Data = TxBytes, Length = tx_len, MessageType = MessageType.SendBytes });
                return true;
            }
            return false;
        }

        //Сбросить флаги ожидания ответа на запрос
        private void Request_End(bool status)
        {
            Rx_Bytes = new byte[0];
            timer_Timeout.Stop();
            CurrentCommand = Commands.None;
            if(!status) DataContainer = null;
            CommandEnd(this, new ProtocolEventArgs(status) { DataObject = DataContainer });
        }

        //Возвращает время в милисекундах прошедшее с последнего запроса Request_Start
        private long Ping()
        {
            ping.Stop();
            return ping.ElapsedMilliseconds;
        }
        private string PingStr()
        {
            return " ("+Ping()+" ms)";
        }

        //Работа с буфером отправки
        private void Clear_Tx() { tx_len = 0; }
        private void Add_Tx(byte data) { TxBytes[tx_len++] = data; }
        private void Add_Tx(bool data) { Add_Tx(data ? (byte)1 : (byte)0); }
        private void Add_Tx(string data) { Add_Tx(Encoding.Default.GetBytes(data)); }
        private void Add_Tx(byte[] data) { Add_Tx(data, data.Length); }
        private void Add_Tx(byte[] data, int length) { for (int i = 0; i < length; i++) Add_Tx(data[i]); }
        private void Start_Add_Tx(Commands cmd)
        {
            Clear_Tx(); //Очистим буфер передачи
            Add_Tx(0);  //Добавим первый байт сообщение
            Add_Tx(Encoding.UTF8.GetBytes("Pls" + CommandCodes[cmd]));//Добавим остальные байты начала сообщения
        }

        #region КАНАЛ
        //Запрос ПОИСК УСТРОЙСТВ в канале (и режима работы)
        private bool CMD_Search_Devices()
        {
            Start_Add_Tx(Commands.Search_Devices);
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Поиск устройств" });
            return Request_Start(new List<string>());
        }
        //Обработка ответа
        private bool CMD_Search_Devices(byte[] rxBytes)
        {
            int mode = rxBytes[6];
            string mode_ = "";
            if (mode == 0) mode_ = " [Счетчик]";
            if (mode == 1) mode_ = " [Фаза А]";
            if (mode == 2) mode_ = " [Фаза B]";
            if (mode == 3) mode_ = " [Фаза C]";
            string serial_num = rxBytes[7].ToString("00") + rxBytes[8].ToString("00") + rxBytes[9].ToString("00") + rxBytes[10].ToString("00");
            ((List<string>)DataContainer.Data).Add(serial_num + mode_);
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Ответил " + serial_num + mode_+ PingStr() });
            if (mode == 0 || mode == 3)
                return true;    //заканчиваем
            else
                return false;   //ожидаем следующее сообщение
        }

        //Запрос ПРОВЕРКА ПАРОЛЯ
        private bool CMD_Check_Pass(PulsePLCv2LoginPass param)
        {
            Start_Add_Tx(Commands.Check_Pass);
            //Серийник
            byte[] serial = param.Serial;
            Add_Tx(serial);
            //Пароль
            byte[] pass_ = param.Pass;
            byte[] pass_buf = new byte[6] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            for (int i = 0; i < 6; i++) if(i < pass_.Length) pass_buf[i] = pass_[i];
            Add_Tx(pass_buf);
            Message(this, new MessageDataEventArgs() {
                MessageType = MessageType.Normal,
                MessageString = "Авторизация: [" + 
                serial[0].ToString("00") + 
                serial[1].ToString("00") + 
                serial[2].ToString("00") + 
                serial[3].ToString("00") + "] [" +  Encoding.Default.GetString(pass_buf) +"]"
            });
            //Отправляем запрос
            return Request_Start();
        }
        //Обработка запроса
        private void CMD_Check_Pass(byte[] rxBytes)
        {
            string accessStr = "_";
            string service_mode = rxBytes[6] == 1 ? "[Sercvice mode]" : "";
            if (rxBytes[7] == 's') { accessStr = "Нет доступа "; access = AccessType.Write; }
            if (rxBytes[7] == 'r') { accessStr = "Чтение "; access = AccessType.Read; }
            if (rxBytes[7] == 'w') { accessStr = "Запись "; access = AccessType.Write; }
            DataContainer.Data = access;
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Доступ открыт: " + accessStr + service_mode + PingStr() });
            //Таймер доступа ДОДЕЛАТЬ (добавить отображение значка доступа и сообщения типа "Запись/Чтение")
            timer_Access.Start();
        }

        //Запрос ЗАКРЫТИЕ СЕССИИ (закрывает доступ к данным)
        private bool CMD_Close_Session()
        {
            //Первые байты по протоколу конфигурации
            Start_Add_Tx(Commands.Close_Session);
            access = AccessType.No_Access;
            AccessEnd(this, null);
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.NormalBold, MessageString = "Закрыть сессию" });
            return Request_Start();
        }
        #endregion

        #region СЕРВИСНЫЕ КОМАНДЫ
        //Запрос BOOTLOADER (стирает сектор флеш памяти чтобы устройство загружалось в режиме bootloader)
        private bool CMD_BOOTLOADER()
        {
            Start_Add_Tx(Commands.Bootloader);
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Включить режим обновления" });
            return Request_Start();
        }
        //Обработка запроса
        private void CMD_BOOTLOADER(byte[] rxBytes)
        {
            Message(this, new MessageDataEventArgs() {
                MessageType = MessageType.Good,
                MessageString = "Теперь отключи питание устройства и подключи по USB к компьютеру. Устройство должно определиться как флеш накопитель." + PingStr()
            });
        }

        //Запрос - Запись серийного номера
        private bool CMD_SerialWrite(PulsePLCv2LoginPass serial)
        {
            Start_Add_Tx(Commands.SerialWrite);
            Add_Tx(serial.Serial); //Новый серийник 4 байта
            Message(this, new MessageDataEventArgs() {
                MessageType = MessageType.Normal,
                MessageString = "ЗАПИСЬ СЕРИЙНОГО НОМЕРА " + serial.SerialString
            });
            return Request_Start();
        }
        //Обработка запроса
        private void CMD_SerialWrite(byte[] rxBytes)
        {
            if (rxBytes[6] == 'O' && rxBytes[7] == 'K')
                Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "СЕРИЙНЫЙ НОМЕР ЗАПИСАН" + PingStr() });
        }

        //Запрос EEPROM BURN (сброс к заводским настройкам)
        private bool CMD_EEPROM_BURN()
        {
            Start_Add_Tx(Commands.EEPROM_Burn);
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Сброс к заводским параметрам" });
            return Request_Start();
        }
        //Обработка запроса
        private void CMD_EEPROM_BURN(byte[] rxBytes)
        {
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "После перезагрузки устройство запишет в память заводские настройки." + PingStr() });
        }

        //Запрос EEPROM READ BYTE (чтение байта из памяти)
        private bool CMD_EEPROM_Read_Byte(ushort eep_adrs)
        {
            Start_Add_Tx(Commands.EEPROM_Read_Byte);
            Add_Tx((byte)(eep_adrs >> 8));  //Старший
            Add_Tx((byte)eep_adrs);       //Младший
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Чтение байта EEPROM" });
            return Request_Start();
        }
        //Обработка запроса
        private void CMD_EEPROM_Read_Byte(byte[] rxBytes)
        {
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Байт прочитан int: " + rxBytes[6] + ", ASCII: '" + Convert.ToChar(rxBytes[6]) + "'" + PingStr() });
        }

        //Запрос ПЕРЕЗАГРУЗКА
        private bool CMD_Reboot()
        {
            Start_Add_Tx(Commands.Reboot);
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Перезагрузить" });
            return Request_Start();
        }
        //Обработка запроса
        private void CMD_Reboot(byte[] rxBytes)
        {
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Устройство перезагружается.." + PingStr() });
        }
        #endregion

        #region ОСНОВНЫЕ ПАРАМЕТРЫ (режимы работы, ошибки, пароли)
        //Запрос - ЧТЕНИЕ ОСНОВНЫХ ПАРАМЕТРОВ 
        private bool CMD_Read_Main_Params()
        {
            Start_Add_Tx(Commands.Read_Main_Params);
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Чтение основных параметров" });
            return Request_Start();
        }
        //Обработка ответа
        private void CMD_Read_Main_Params(byte[] rxBytes)
        {
            DeviceMainParams device = new DeviceMainParams();
            //Версия прошивки
            device.VersionFirmware = "v2." + rxBytes[7] + "." + rxBytes[8];
            device.VersionEEPROM = "v1." + rxBytes[6];
            //Параметры
            device.WorkMode = (WorkMode)rxBytes[9];
            device.BatteryMode = (BatteryMode)rxBytes[10];
            device.RS485_WorkMode = (InterfaceMode)rxBytes[11];
            device.Bluetooth_WorkMode = (InterfaceMode)rxBytes[12];
            //Ошибки
            device.ErrorsByte = rxBytes[13];
            //Передаем данные
            DataContainer.Data = device;
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Основные параметры успешно прочитаны" + PingStr() });
        }

        //Запрос - ЗАПИСЬ ОСНОВНЫХ ПАРАМЕТРОВ 
        private bool CMD_Write_Main_Params(DeviceMainParams device)
        {
            Start_Add_Tx(Commands.Write_Main_Params);
            //Параметры
            Add_Tx((byte)device.WorkMode);
            Add_Tx((byte)device.BatteryMode);
            Add_Tx((byte)device.RS485_WorkMode);
            Add_Tx((byte)device.Bluetooth_WorkMode);
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Запись основных параметров" });
            return Request_Start();
        }
        //Обработка ответа
        private void CMD_Write_Main_Params(byte[] rxBytes)
        {
            if (rxBytes[6] == 'O' && rxBytes[7] == 'K') Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Основные параметры успешно записаны" + PingStr() });
            if (rxBytes[6] == 'e' && rxBytes[7] == 'r') Message(this, new MessageDataEventArgs() { MessageType = MessageType.Error, MessageString = "Ошибка при записи." + PingStr() });
        }

        //Запрос - ОЧИСТИТЬ ФЛАГИ ОШИБОК
        private bool CMD_Clear_Errors()
        {
            Start_Add_Tx(Commands.Clear_Errors);
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Очистка флагов ошибок." });
            return Request_Start();
        }
        //Обработка запроса
        private void CMD_Clear_Errors(byte[] rxBytes)
        {
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Флаги ошибок сброшены" + PingStr() });
        }

        //Запрос - Запись паролей
        private bool CMD_Pass_Write(DeviceMainParams device)
        {
            Start_Add_Tx(Commands.Pass_Write);
            //Пароль на запись
            Add_Tx(device.NewPassWrite);//Флаг записи
            Add_Tx(device.PassWrite);
            //Пароль на чтение
            Add_Tx(device.NewPassRead); //Флаг записи
            Add_Tx(device.PassRead);

            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Запись новых паролей" });
            return Request_Start();
        }
        //Обработка запроса
        private void CMD_Pass_Write(byte[] bytes_buff)
        {
            if (bytes_buff[6] == 'O' && bytes_buff[7] == 'K')
                Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Пароли успешно записаны" + PingStr() });
        }
        #endregion

        #region ПАРАМЕТРЫ ИМПУЛЬСНЫХ ВХОДОВ
        //Запрос ЧТЕНИЕ ПАРАМЕТРОВ ИМПУЛЬСНЫХ ВХОДОВ
        private bool CMD_Read_Imp_Params(ImpNum imp_num)
        {
            Start_Add_Tx(Commands.Read_IMP);
            Add_Tx(Convert.ToByte(imp_num.ToString()));
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Чтение настроек IMP" + imp_num });
            return Request_Start();
        }
        //Обработка ответа
        private void CMD_Read_Imp_Params(byte[] rxBytes)
        {
            ImpParams Imp = null;
            if (rxBytes[6] != '1' && rxBytes[6] != '2') return;
            int pntr = 7;
            Imp.IsEnable = rxBytes[pntr++];
            if (Imp.IsEnable != 0)
            {
                Imp.Adrs_PLC = rxBytes[pntr++];
                //Тип протокола
                Imp.Ascue_protocol = rxBytes[pntr++];
                //Адрес аскуэ
                Imp.Ascue_adrs = (ushort)(rxBytes[pntr++] << 8);
                Imp.Ascue_adrs += rxBytes[pntr++];
                //Пароль для аскуэ (6)
                Imp.Ascue_pass[0] = rxBytes[pntr++];
                Imp.Ascue_pass[1] = rxBytes[pntr++];
                Imp.Ascue_pass[2] = rxBytes[pntr++];
                Imp.Ascue_pass[3] = rxBytes[pntr++];
                Imp.Ascue_pass[4] = rxBytes[pntr++];
                Imp.Ascue_pass[5] = rxBytes[pntr++];
                //Эмуляция переполнения (1)
                Imp.Perepoln = (ImpOverflowType)rxBytes[pntr++];
                //Передаточное число (2)
                Imp.A = (UInt16)(rxBytes[pntr++] << 8);
                Imp.A += rxBytes[pntr++];
                //Тарифы (11)
                Imp.T_qty = (ImpNumOfTarifs)rxBytes[pntr++];
                Imp.T1_Time_1 = new ImpTime(rxBytes[pntr++], rxBytes[pntr++]);
                Imp.T3_Time_1 = new ImpTime(rxBytes[pntr++], rxBytes[pntr++]);
                Imp.T1_Time_2 = new ImpTime(rxBytes[pntr++], rxBytes[pntr++]);
                Imp.T3_Time_2 = new ImpTime(rxBytes[pntr++], rxBytes[pntr++]);
                Imp.T2_Time = new ImpTime(rxBytes[pntr++], rxBytes[pntr++]);
                //Показания - Текущие (12)

                uint E = rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                Imp.E_T1 = new ImpEnergyValue(E);
                E = rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                Imp.E_T2 = new ImpEnergyValue(E);
                E = rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                Imp.E_T3 = new ImpEnergyValue(E);
                //Показания - На начало суток (12)
                E = rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                Imp.E_T1_Start = new ImpEnergyValue(E);
                E = rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                Imp.E_T2_Start = new ImpEnergyValue(E);
                E = rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                Imp.E_T3_Start = new ImpEnergyValue(E);
                //Максимальная мощность
                Imp.Max_Power = (UInt16)(rxBytes[pntr++] << 8);
                Imp.Max_Power += rxBytes[pntr++];
                //Резервные параметры (на будущее)
                //4 байта
                byte reserv_ = rxBytes[pntr++];
                reserv_ = rxBytes[pntr++];
                reserv_ = rxBytes[pntr++];
                reserv_ = rxBytes[pntr++];
            }
            ImpNum impNum = (rxBytes[6] == '1') ? ImpNum.IMP1 : ImpNum.IMP2;
            DataContainer.Data = new ImpParamsForProtocol(Imp, impNum);
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Параметры IMP" + Convert.ToChar(rxBytes[6]) + " успешно прочитаны" + PingStr() });
        }

        //Запрос ЧТЕНИЕ ПАРАМЕТРОВ ИМПУЛЬСНЫХ ВХОДОВ
        private bool CMD_Read_Imp_Extra_Params(ImpNum imp_num)
        {
            Start_Add_Tx(Commands.Read_IMP_extra);
            byte imp_ = 0;
            if (imp_num == ImpNum.IMP1) imp_ = Convert.ToByte('1');
            if (imp_num == ImpNum.IMP2) imp_ = Convert.ToByte('2');
            //Параметры
            Add_Tx(imp_);
            //Отправляем запрос
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Чтение состояния IMP" + imp_num });
            return Request_Start();
        }
        //Обработка ответа
        private void CMD_Read_Imp_Extra_Params(byte[] bytes_buff)
        {
            Label label_;
            int pntr = 6;
            //if (bytes_buff[pntr] == '1') label_ = mainForm.label_Imp1_Extra;
            //else if (bytes_buff[pntr] == '2') label_ = mainForm.label_Imp2_Extra;
            //else return;
            pntr++;
            byte imp_Tarif = bytes_buff[pntr++];
            UInt32 imp_Counter = bytes_buff[pntr++];
            imp_Counter = (imp_Counter << 8) + bytes_buff[pntr++];
            UInt32 imp_last_imp_ms = bytes_buff[pntr++];
            imp_last_imp_ms = (imp_last_imp_ms << 8) + bytes_buff[pntr++];
            imp_last_imp_ms = (imp_last_imp_ms << 8) + bytes_buff[pntr++];
            imp_last_imp_ms = (imp_last_imp_ms << 8) + bytes_buff[pntr++];
            UInt32 imp_P = bytes_buff[pntr++];
            imp_P = (imp_P << 8) + bytes_buff[pntr++];
            imp_P = (imp_P << 8) + bytes_buff[pntr++];
            imp_P = (imp_P << 8) + bytes_buff[pntr++];
            //Отобразим
            //mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => {
            //    label_.Content = "Текущие параметры: \n";
            //    label_.Content += "Текущий тариф: " + imp_Tarif + "\n";
            //    label_.Content += "Импульсы: "+ imp_Counter+"\n";
            //    label_.Content += "Время импульса: " + (imp_last_imp_ms/1000f).ToString("#0.00") + " сек\n";
            //    label_.Content += "Нагрузка: " + imp_P + " Вт\n";
            //}));
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Мгновенные значения считаны" + PingStr() });
        }

        //Запрос ЗАПИСЬ ПАРАМЕТРОВ ИМПУЛЬСНЫХ ВХОДОВ
        private bool CMD_Write_Imp_Params(ImpParamsForProtocol imp)
        {
            //ImpsData Imp_;
            //if (imp_num == 1) Imp_ = mainForm.deviceConfig.Imp1;
            //else if (imp_num == 2) Imp_ = mainForm.deviceConfig.Imp2;
            //else return false;
            
            //Start_Add_Tx(Commands.Write_IMP);
            //Add_Tx((byte)(Convert.ToByte('0') + imp_num));
            ////Параметры
            //Add_Tx(Imp_.Is_Enable); //7
            //if (Imp_.Is_Enable == 1)
            //{
            //    //
            //    Add_Tx(Imp_.adrs_PLC);
            //    //Тип протокола
            //    Add_Tx(Imp_.ascue_protocol);
            //    //Адрес аскуэ
            //    Add_Tx((byte)(Imp_.ascue_adrs >> 8));
            //    Add_Tx((byte)Imp_.ascue_adrs);
            //    //Пароль для аскуэ (6)
            //    Add_Tx(Imp_.ascue_pass, 6);
            //    //Эмуляция переполнения (1)
            //    Add_Tx(Imp_.perepoln);
            //    //Передаточное число (2)
            //    Add_Tx((byte)(Imp_.A >> 8));
            //    Add_Tx((byte)Imp_.A);
            //    //Тарифы (11)
            //    Add_Tx(Imp_.T_qty);
            //    Add_Tx((byte)(Imp_.T1_Time_1 / 60));
            //    Add_Tx((byte)(Imp_.T1_Time_1 % 60));
            //    Add_Tx((byte)(Imp_.T3_Time_1 / 60));
            //    Add_Tx((byte)(Imp_.T3_Time_1 % 60));
            //    Add_Tx((byte)(Imp_.T1_Time_2 / 60));
            //    Add_Tx((byte)(Imp_.T1_Time_2 % 60));
            //    Add_Tx((byte)(Imp_.T3_Time_2 / 60));
            //    Add_Tx((byte)(Imp_.T3_Time_2 % 60));
            //    Add_Tx((byte)(Imp_.T2_Time / 60));
            //    Add_Tx((byte)(Imp_.T2_Time % 60));
            //    //Показания (12)
            //    Add_Tx((byte)(Imp_.E_T1 >> 24));
            //    Add_Tx((byte)(Imp_.E_T1 >> 16));
            //    Add_Tx((byte)(Imp_.E_T1 >> 8));
            //    Add_Tx((byte)Imp_.E_T1);
            //    Add_Tx((byte)(Imp_.E_T2 >> 24));
            //    Add_Tx((byte)(Imp_.E_T2 >> 16));
            //    Add_Tx((byte)(Imp_.E_T2 >> 8));
            //    Add_Tx((byte)Imp_.E_T2);
            //    Add_Tx((byte)(Imp_.E_T3 >> 24));
            //    Add_Tx((byte)(Imp_.E_T3 >> 16));
            //    Add_Tx((byte)(Imp_.E_T3 >> 8));
            //    Add_Tx((byte)Imp_.E_T3);
            //    //Максимальная нагрузка
            //    Add_Tx((byte)(Imp_.max_Power >> 8));
            //    Add_Tx((byte)Imp_.max_Power);
            //    //Резервные параметры (на будущее)
            //    Add_Tx(0);
            //    Add_Tx(0);
            //    Add_Tx(0);
            //    Add_Tx(0);
            //}
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Запись параметров IMP" + "num!!!" });
            return Request_Start();
        }
        //Обработка ответа
        private void CMD_Write_Imp_Params(byte[] bytes_buff)
        {
            if (bytes_buff[6] == 'O' && bytes_buff[7] == 'K') Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Параметры успешно записаны" + PingStr() });
            if (bytes_buff[6] == 'e' && bytes_buff[7] == 'r') Message(this, new MessageDataEventArgs() { MessageType = MessageType.Error, MessageString = "Ошибка при записи IMP" + PingStr() });
        }
        #endregion

        #region ЧТЕНИЕ ЖУРНАЛА СОБЫТИЙ
        //Запрос ЧТЕНИЕ ЖУРНАЛА СОБЫТИЙ
        private bool CMD_Read_Journal(Journal_type journal)
        {
            Start_Add_Tx(Commands.Read_Journal);
            //Тип журнала
            Add_Tx(Convert.ToByte(journal + 48)); //Передаем номер в виде ASCII символа
            string jName = "";
            if (journal == Journal_type.CONFIG) jName = "конфигурации";
            if (journal == Journal_type.INTERFACES) jName = "интерфейсов";
            if (journal == Journal_type.POWER) jName = "питания";
            if (journal == Journal_type.REQUESTS) jName = "PLC запросов";
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Чтение журнала "+ jName });
            return Request_Start();
        }
        //Обработка ответа
        private void CMD_Read_Journal(byte[] bytes_buff)
        {
            object dataGrid_journal = null;
            //if (bytes_buff[6] == '1') dataGrid_journal = mainForm.dataGrid_Log_Power;
            //if (bytes_buff[6] == '2') dataGrid_journal = mainForm.dataGrid_Log_Config;
            //if (bytes_buff[6] == '3') dataGrid_journal = mainForm.dataGrid_Log_Interfaces;
            //if (bytes_buff[6] == '4') dataGrid_journal = mainForm.dataGrid_Log_Requests;
            int events_count = bytes_buff[7];
            for (int i = 0; i < events_count; i++)
            {
                string event_name = "";
                DateTime time;
                string time_string, date_string;
                //Журнал Питание
                byte event_code = 1;
                if (bytes_buff[i * 7 + 8] == event_code++) event_name = "Включение";
                if (bytes_buff[i * 7 + 8] == event_code++) event_name = "Отключение";
                if (bytes_buff[i * 7 + 8] == event_code++) event_name = "Перезагрузка";
                if (bytes_buff[i * 7 + 8] == event_code++) event_name = "Вход 1 - Превышение максимальной мощности";
                if (bytes_buff[i * 7 + 8] == event_code++) event_name = "Вход 2 - Превышение максимальной мощности";
                //Журнал Конфигурация
                event_code = 51;
                if (bytes_buff[i * 7 + 8] == event_code++) event_name = "Записаны дата и время";
                if (bytes_buff[i * 7 + 8] == event_code++) event_name = "Записаны основные параметры (Концентратор А)";
                if (bytes_buff[i * 7 + 8] == event_code++) event_name = "Записаны основные параметры (Концентратор B)";
                if (bytes_buff[i * 7 + 8] == event_code++) event_name = "Записаны основные параметры (Концентратор C)";
                if (bytes_buff[i * 7 + 8] == event_code++) event_name = "Записаны основные параметры (Счетчик)";
                if (bytes_buff[i * 7 + 8] == event_code++) event_name = "Записаны параметры имп. входа 1";
                if (bytes_buff[i * 7 + 8] == event_code++) event_name = "Записаны параметры имп. входа 2";
                if (bytes_buff[i * 7 + 8] == event_code++) event_name = "Запись в таблицу маршрутов";
                if (bytes_buff[i * 7 + 8] == event_code++) event_name = "Запись паролей";
                //Журнал Интерфейсы
                event_code = 101;
                if (bytes_buff[i * 7 + 8] == event_code++) event_name = "Запрос по RS485";
                if (bytes_buff[i * 7 + 8] == event_code++) event_name = "Запрос по Bluetooth";
                if (bytes_buff[i * 7 + 8] == event_code++) event_name = "Запрос по USB";
                //Журнал Запросы
                event_code = 151;
                if (bytes_buff[i * 7 + 8] == event_code++) event_name = "Запрос по PLCv1";
                if (bytes_buff[i * 7 + 8] == event_code++) event_name = "Синхронизация времени";
                if (bytes_buff[i * 7 + 8] == event_code++) event_name = "Чтение серийного номера";
                if (bytes_buff[i * 7 + 8] == event_code++) event_name = "Чтение показаний - Начало суток";
                if (bytes_buff[i * 7 + 8] == event_code++) event_name = "Чтение показаний - Текущие";

                if (bytes_buff[6] == '4')
                {
                    byte steps = bytes_buff[i * 7 + 14];
                    byte adrs = bytes_buff[i * 7 + 13];
                    bool status = bytes_buff[i * 7 + 12] == 0? false : true;
                    //Количество ступеней ретрансляции в запросе
                    if (steps == 0)
                        event_name = "[Прямой запрос] " + event_name;
                    else
                        event_name = "[Запрос через "+ bytes_buff[i * 7 + 14] + " ступеней] " + event_name;

                    if (status) date_string = adrs + " - Успешно"; else date_string = adrs + " - Нет ответа";

                    time_string = bytes_buff[i * 7 + 11].ToString("00") + ":" +
                                        bytes_buff[i * 7 + 10].ToString("00") + ":" +
                                        bytes_buff[i * 7 + 9].ToString("00");
                }
                else
                {
                    try
                    {
                        time = new DateTime(bytes_buff[i * 7 + 14], bytes_buff[i * 7 + 13], bytes_buff[i * 7 + 12], bytes_buff[i * 7 + 11], bytes_buff[i * 7 + 10], bytes_buff[i * 7 + 9]);
                        date_string = time.ToString("dd.MM.yy");
                        time_string = time.ToString("HH:mm:ss");
                    }
                    catch (Exception)
                    {
                        time_string = bytes_buff[i * 7 + 11].ToString() + ":" +
                                        bytes_buff[i * 7 + 10].ToString() + ":" +
                                        bytes_buff[i * 7 + 9].ToString();
                        date_string = bytes_buff[i * 7 + 12].ToString() + "." +
                                        bytes_buff[i * 7 + 13].ToString() + "." +
                                        bytes_buff[i * 7 + 14].ToString();
                    }
                }
                DataGridRow_Log row = new DataGridRow_Log {Num = (i + 1).ToString(), Date = date_string, Time = time_string, Name = event_name };
                //mainForm.DataGrid_Log_Add_Row((DataGrid)dataGrid_journal, row);
            }
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Журнал успешно прочитан" + PingStr() });
        }
        #endregion

        #region ВРЕМЯ И ДАТА
        //Запрос ЧТЕНИЕ ВРЕМЕНИ И ДАТЫ
        private bool CMD_Read_DateTime()
        {
            Start_Add_Tx(Commands.Read_DateTime);
            //Отправляем запрос
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Чтение даты/времени" });
            return Request_Start();
        }
        //Обработка ответа
        private void CMD_Read_DateTime(byte[] bytes_buff)
        {
            
            try
            {
                DateTime datetime_ = new DateTime((int)(DateTime.Now.Year / 100) * 100 + bytes_buff[11], bytes_buff[10], bytes_buff[9], bytes_buff[8], bytes_buff[7], bytes_buff[6]);
                //mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => {
                //    mainForm.textBox_Date_in_device.Text = datetime_.ToString("dd.MM.yy");
                //    mainForm.textBox_Time_in_device.Text = datetime_.ToString("HH:mm:ss");
                //    System.TimeSpan diff = datetime_.Subtract(DateTime.Now);
                //    mainForm.textBox_Time_difference.Text = diff.ToString("g");
                //    mainForm.textBox_Date_in_pc.Text = DateTime.Now.ToString("dd.MM.yy");
                //    mainForm.textBox_Time_in_pc.Text = DateTime.Now.ToString("HH:mm:ss");
                //    //Покрасим пункт меню в зеленый
                //    mainForm.treeView_DateTime.Foreground = Brushes.Green;
                //}));
                Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Время/Дата успешно прочитаны" + PingStr() });
            }
            catch (Exception)
            {
                Message(this, new MessageDataEventArgs() { MessageType = MessageType.Warning, MessageString = "Неопределенный формат даты. " });
                Message(this, new MessageDataEventArgs() { MessageType = MessageType.Warning, MessageString = "Попробуйте записать время на устройство заново. " });
                Message(this, new MessageDataEventArgs() { MessageType = MessageType.Warning, MessageString = "Возможны проблемы с батареей. " + PingStr() });
            }
        }

        //Запрос ЗАПИСЬ ВРЕМЕНИ И ДАТЫ
        private bool CMD_Write_DateTime()
        {
            Start_Add_Tx(Commands.Write_DateTime);
            //Данные
            Add_Tx((byte)DateTime.Now.Second);
            Add_Tx((byte)DateTime.Now.Minute);
            Add_Tx((byte)DateTime.Now.Hour);
            Add_Tx((byte)DateTime.Now.Day);
            Add_Tx((byte)DateTime.Now.Month);
            int year_ = DateTime.Now.Year;
            while (year_ >= 100) year_ -= 100;
            Add_Tx((byte)year_);

            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Запись даты/времени (" + DateTime.Now + ")" });
            return Request_Start();
        }
        //Обработка ответа
        private void CMD_Write_DateTime(byte[] bytes_buff)
        {
            if (bytes_buff[6] == 'O' && bytes_buff[7] == 'K') Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Дата и время успешно записаны" + PingStr() });
            if (bytes_buff[6] == 'e' && bytes_buff[7] == 'r') Message(this, new MessageDataEventArgs() { MessageType = MessageType.Error, MessageString = "Ошибка при записи даты и времени. Возможно недопустимый формат даты." + PingStr() });
        }
        #endregion

        #region Таблица PLC
        //Запрос ЧТЕНИЕ Таблицы PLC - Активные адреса
        private bool CMD_Read_PLC_Table(byte[] adrs_massiv)
        {
            Start_Add_Tx(Commands.Read_PLC_Table);
            //Данные
            byte count_ = adrs_massiv[0];
            Add_Tx(count_);
            for (byte i = 0; i < count_; i++)
            {
                Add_Tx(adrs_massiv[i+1]);
            }
            //Отправляем запрос
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Чтение таблицы PLC - Активные адреса" });
            return Request_Start();
        }
        //Обработка ответа
        private void CMD_Read_PLC_Table(byte[] bytes_buff, int count)
        {
            int count_adrs;
            if (bytes_buff[6] == 0)
            {
                count_adrs = bytes_buff[7]; //Число адресов в ответе
                for (int i = 1; i <= count_adrs; i++)
                {
                    //mainForm.plc_table[bytes_buff[7 + i] - 1].Enable = true;
                }
                //mainForm.PLC_Table_Refresh();
                //Сообщение
                Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Найдено " + count_adrs + " активных адресов" + PingStr() });
                //Отправим запрос на данные Таблицы PLC
                if(CurrentCommand == Commands.Read_PLC_Table)
                {
                    //mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => {
                    //    byte[] buff_adrs = new byte[251];
                    //    byte count_ = 0;
                    //    //Выделить строки с галочками
                    //    foreach (DataGridRow_PLC item in mainForm.plc_table)
                    //    {
                    //        if (item.Enable) buff_adrs[++count_] = item.Adrs_PLC;
                    //    }
                    //    buff_adrs[0] = count_;
                    //    mainForm.PLC_Table_Send_Data_Request(Commands.Read_PLC_Table, buff_adrs);
                    //    mainForm.CMD_Buffer.Add_CMD(mainForm.link, this, (int)Commands.Close_Session, null, 0); //Переделать
                    //}));
                }
                //Отправим запрос на данные Показаний
                if (CurrentCommand == Commands.Read_E_Data)
                {
                    //mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => {
                    //    byte[] buff_adrs = new byte[251];
                    //    byte count_ = 0;
                    //    //Выделить строки с галочками
                    //    foreach (DataGridRow_PLC item in mainForm.plc_table)
                    //    {
                    //        if (item.Enable) buff_adrs[++count_] = item.Adrs_PLC;
                    //    }
                    //    buff_adrs[0] = count_;
                    //    mainForm.E_Data_Send_Data_Request(buff_adrs);
                    //    mainForm.CMD_Buffer.Add_CMD(mainForm.link, this, (int)Commands.Close_Session, null, 0);
                    //}));
                }
            }
            else
            {
                int pntr = 6;
                count_adrs = bytes_buff[pntr++];
                int active_adrss = 0; //количество включенных адресов
                string active_adrss_str = "";
                for (int i = 1; i <= count_adrs; i++)
                {
                    byte adrs_plc = bytes_buff[pntr++];
                    bool is_en = (bytes_buff[pntr++] == 0) ? false : true;
                    //mainForm.plc_table[adrs_plc - 1].Enable = is_en;
                    ////Статус связи
                    //mainForm.plc_table[adrs_plc - 1].Link = (bytes_buff[pntr++] == 0) ? false : true;
                    ////Дата последней успешной связи
                    //mainForm.plc_table[adrs_plc - 1].link_Day = bytes_buff[pntr++];
                    //mainForm.plc_table[adrs_plc - 1].link_Month = bytes_buff[pntr++];
                    //mainForm.plc_table[adrs_plc - 1].link_Year = bytes_buff[pntr++];
                    //mainForm.plc_table[adrs_plc - 1].link_Hours = bytes_buff[pntr++];
                    //mainForm.plc_table[adrs_plc - 1].link_Minutes = bytes_buff[pntr++];
                    
                    ////Тип протокола PLC
                    //mainForm.plc_table[adrs_plc - 1].type = bytes_buff[pntr++];
                    ////Качество связи
                    //mainForm.plc_table[adrs_plc - 1].quality = bytes_buff[pntr++];
                    //if (is_en)
                    //{
                    //    active_adrss++;
                    //    if(active_adrss > 1) active_adrss_str += ", ";
                    //    active_adrss_str += adrs_plc;
                    //    //Серийный номер
                    //    mainForm.plc_table[adrs_plc - 1].serial_bytes[0] = bytes_buff[pntr++];
                    //    mainForm.plc_table[adrs_plc - 1].serial_bytes[1] = bytes_buff[pntr++];
                    //    mainForm.plc_table[adrs_plc - 1].serial_bytes[2] = bytes_buff[pntr++];
                    //    mainForm.plc_table[adrs_plc - 1].serial_bytes[3] = bytes_buff[pntr++];
                    //    //Ретрансляция
                    //    mainForm.plc_table[adrs_plc - 1].N = bytes_buff[pntr++];
                    //    mainForm.plc_table[adrs_plc - 1].S1 = bytes_buff[pntr++];
                    //    mainForm.plc_table[adrs_plc - 1].S2 = bytes_buff[pntr++];
                    //    mainForm.plc_table[adrs_plc - 1].S3 = bytes_buff[pntr++];
                    //    mainForm.plc_table[adrs_plc - 1].S4 = bytes_buff[pntr++];
                    //    mainForm.plc_table[adrs_plc - 1].S5 = bytes_buff[pntr++];
                    //    //Тип протокола
                    //    mainForm.plc_table[adrs_plc - 1].Protocol_ASCUE = bytes_buff[pntr++];
                    //    //Адрес аскуэ
                    //    mainForm.plc_table[adrs_plc - 1].Adrs_ASCUE = bytes_buff[pntr++];
                    //    mainForm.plc_table[adrs_plc - 1].Adrs_ASCUE = (UInt16)((mainForm.plc_table[adrs_plc - 1].Adrs_ASCUE<<8) + bytes_buff[pntr++]);
                    //    //Пароль аскуэ
                    //    mainForm.plc_table[adrs_plc - 1].pass_bytes[0] = bytes_buff[pntr++];
                    //    mainForm.plc_table[adrs_plc - 1].pass_bytes[1] = bytes_buff[pntr++];
                    //    mainForm.plc_table[adrs_plc - 1].pass_bytes[2] = bytes_buff[pntr++];
                    //    mainForm.plc_table[adrs_plc - 1].pass_bytes[3] = bytes_buff[pntr++];
                    //    mainForm.plc_table[adrs_plc - 1].pass_bytes[4] = bytes_buff[pntr++];
                    //    mainForm.plc_table[adrs_plc - 1].pass_bytes[5] = bytes_buff[pntr++];
                        
                    //    //Байт ошибок
                    //    mainForm.plc_table[adrs_plc - 1].errors_byte = bytes_buff[pntr++];
                    //}
                }
                //Сообщение
                Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Прочитано " + count_adrs + " адресов, из них " + active_adrss + " вкл. [" + active_adrss_str + "]" + PingStr() });
            }
            
            //Отобразим
            //mainForm.PLC_Table_Refresh();
            
        }
        //Запрос ЗАПИСЬ в Таблицу PLC
        private bool CMD_Write_PLC_Table(byte[] adrs_massiv)
        {
            Start_Add_Tx(Commands.Write_PLC_Table);
            //Данные
            byte count_ = adrs_massiv[0];
            Add_Tx(count_);
            
            for (byte i = 0; i < count_; i++)
            {
                byte adrs_plc = adrs_massiv[i+1];
                Add_Tx(adrs_plc);
                adrs_plc--; //Адреса строк начинаются с 0
                //Add_Tx((mainForm.plc_table[adrs_plc].Enable) ? (byte)1 : (byte)0);
                //if(mainForm.plc_table[adrs_plc].Enable)
                //{
                //    //Серийный номер (4 байта)
                //    Add_Tx(mainForm.plc_table[adrs_plc].serial_bytes, 4);
                //    //Ретрансляция (6 байт)
                //    Add_Tx(mainForm.plc_table[adrs_plc].N);
                //    Add_Tx(mainForm.plc_table[adrs_plc].S1);
                //    Add_Tx(mainForm.plc_table[adrs_plc].S2);
                //    Add_Tx(mainForm.plc_table[adrs_plc].S3);
                //    Add_Tx(mainForm.plc_table[adrs_plc].S4);
                //    Add_Tx(mainForm.plc_table[adrs_plc].S5);
                //    //Тип протокола аскуэ (1 байт)
                //    Add_Tx(mainForm.plc_table[adrs_plc].Protocol_ASCUE);
                //    //Адрес аскуэ (2 байт)
                //    Add_Tx((byte)(mainForm.plc_table[adrs_plc].Adrs_ASCUE>>8));
                //    Add_Tx((byte)mainForm.plc_table[adrs_plc].Adrs_ASCUE);
                //    //Пароль аскуэ (6 байт)
                //    Add_Tx(mainForm.plc_table[adrs_plc].pass_bytes, 6);
                //    //Версия PLC
                //    Add_Tx(mainForm.plc_table[adrs_plc].type);
                //    //Байт ошибок
                //    //Статус связи
                //}
            }

            //Отправляем запрос
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Запись в таблицу PLC" });
            return Request_Start();
        }
        //Обработка ответа
        private void CMD_Write_PLC_Table(byte[] bytes_buff, int count)
        {
            if (bytes_buff[6] == 'O' && bytes_buff[7] == 'K') Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Строки успешно записаны в PLC таблицу" + PingStr() });
            if (bytes_buff[6] == 'e' && bytes_buff[7] == 'r') Message(this, new MessageDataEventArgs() { MessageType = MessageType.Error, MessageString = "Ошибка при записи в PLC таблицу." + PingStr() });
        }
        #endregion

        #region E показания
        //Запрос Чтение Текущих показаний
        private bool CMD_Read_E_Current(byte adrs_dev)
        {
            Start_Add_Tx(Commands.Read_E_Current);
            //Адрес устройства
            Add_Tx(adrs_dev);
            
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Чтение Показаний на момент последнего опроса" });
            return Request_Start();
        }
        //Запрос Чтение показаний на Начало суток
        private bool CMD_Read_E_Start_Day(byte adrs_dev)
        {
            Start_Add_Tx(Commands.Read_E_Start_Day);
            //Адрес устройства
            Add_Tx(adrs_dev);

            //Отправляем запрос
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Показания на начало суток" });
            return Request_Start();
        }
        //Обработка ответа
        private void CMD_Read_E(byte[] bytes_buff)
        {
            //Получаем данные
            UInt32 E_T1, E_T2, E_T3;
            bool E_Correct = false;
            int adrs_PLC = bytes_buff[7];
            int parametr = bytes_buff[8];
            int pntr = 9;
            E_T1 = bytes_buff[pntr++];
            E_T1 = (E_T1 << 8) + bytes_buff[pntr++];
            E_T1 = (E_T1 << 8) + bytes_buff[pntr++];
            E_T1 = (E_T1 << 8) + bytes_buff[pntr++];
            E_T2 = bytes_buff[pntr++];
            E_T2 = (E_T2 << 8) + bytes_buff[pntr++];
            E_T2 = (E_T2 << 8) + bytes_buff[pntr++];
            E_T2 = (E_T2 << 8) + bytes_buff[pntr++];
            E_T3 = bytes_buff[pntr++];
            E_T3 = (E_T3 << 8) + bytes_buff[pntr++];
            E_T3 = (E_T3 << 8) + bytes_buff[pntr++];
            E_T3 = (E_T3 << 8) + bytes_buff[pntr++];
            //4 байта запас на 4й тариф
            pntr += 4;
            E_Correct = (bytes_buff[pntr++] == 177) ? true : false;

            //Вставляем данные в таблицу
            string type_E = "";
            if(CurrentCommand == Commands.Read_E_Current)
            {
                type_E = "Последние показания";
                //mainForm.plc_table[adrs_PLC - 1].E_Current_T1 = E_T1.ToString();
                //mainForm.plc_table[adrs_PLC - 1].E_Current_T2 = E_T2.ToString();
                //mainForm.plc_table[adrs_PLC - 1].E_Current_T3 = E_T3.ToString();
                //mainForm.plc_table[adrs_PLC - 1].e_Current_Correct = E_Correct;
            }
            if (CurrentCommand == Commands.Read_E_Start_Day)
            {
                type_E = "Начало суток";
                //mainForm.plc_table[adrs_PLC - 1].E_StartDay_T1 = E_T1.ToString();
                //mainForm.plc_table[adrs_PLC - 1].E_StartDay_T2 = E_T2.ToString();
                //mainForm.plc_table[adrs_PLC - 1].E_StartDay_T3 = E_T3.ToString();
                //mainForm.plc_table[adrs_PLC - 1].e_StartDay_Correct = E_Correct;
            }

            //Сообщение
            if (E_Correct)
                Message(this, new MessageDataEventArgs() {
                    MessageType = MessageType.Good,
                    MessageString = "Прочитано - " + type_E + " " + adrs_PLC + ": T1 (" + (((float)E_T1) / 1000f).ToString() + "), T2 (" + (((float)E_T2) / 1000f).ToString() + "), T3 (" + (((float)E_T3) / 1000f).ToString() + ")" + PingStr()
                });
            else
                Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Прочитано - " + type_E + " " + adrs_PLC + ": Н/Д" + PingStr() });

            //mainForm.PLC_Table_Refresh();
        }
        #endregion

        #region PLC запросы
        //Команда Отправка запроса PLC
        private bool CMD_Request_PLC(byte[] param)
        {
            Start_Add_Tx(Commands.Request_PLC);
            //Тип запроса по PLC 1б
            //Адрес устройства 1б
            //Ретрансляция Количество ступеней 1б 
            //Адреса ступеней 5б
            Add_Tx(param, 8);

            //Отправляем запрос
            if (param[2] == 0)
                Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Ппрямой запрос PLC на " + param[0] });
            else
            {
                string steps_ = "";
                for (int i = 0; i < param[2]; i++) { steps_ += ", " + param[i+3]; }
                Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Запрос PLC на " + param[1] + " через " + steps_ });
            }
            int delay = 5000 * (param[2] + 1);
            return Request_Start();
        }
        //Обработка ответа
        private void CMD_Request_PLC(byte[] bytes_buff, int count)
        {
            //Получаем данные
            byte plc_cmd_code = bytes_buff[6];
            bool request_status = (bytes_buff[7] == 0) ? false : true;
            int adrs_PLC = bytes_buff[8];
            int plc_type = bytes_buff[9];
            
            string plc_v = "";
            if (plc_type == 11) plc_v = "PLCv1";
            if (plc_type == 22) plc_v = "PLCv2";
            
            //Сообщение
            if (request_status)
            {
                if (plc_cmd_code == 0)
                {
                    double E_ = (double)bytes_buff[13] * 256 * 256 * 256 + (double)bytes_buff[12] * 256 * 256 + (double)bytes_buff[11] * 256 + (double)bytes_buff[10];
                    Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Прочитано №" + adrs_PLC + ", Тип: " + plc_v + ", Тариф 1: " + (((double)E_) / 1000f).ToString() + " кВт" + PingStr() });
                }
                if (plc_cmd_code == 1)
                {
                    string errors_string = "Ошибки: ";
                    if ((bytes_buff[10] & 1) > 0) errors_string += "ОБ ";
                    if ((bytes_buff[10] & 2) > 0) errors_string += "ББ ";
                    if ((bytes_buff[10] & 4) > 0) errors_string += "П1 ";
                    if ((bytes_buff[10] & 8) > 0) errors_string += "П2 ";
                    if ((bytes_buff[10] & 16) > 0) errors_string += "ОП ";
                    if ((bytes_buff[10] & 32) > 0) errors_string += "ОВ ";
                    //!! добавить ошибки ДОДЕЛАТЬStringMessage
                    if (errors_string == "Ошибки: ") errors_string = "Нет ошибок";
                    Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Прочитано №" + adrs_PLC + " " + errors_string + PingStr() });
                }
                if (plc_cmd_code == 2)
                {
                    string serial_string = bytes_buff[10].ToString("00")+ bytes_buff[11].ToString("00")+ bytes_buff[12].ToString("00")+ bytes_buff[13].ToString("00");
                    Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Прочитано №" + adrs_PLC + " Серийный номер: " + serial_string + PingStr() });
                }
                if (plc_cmd_code == 3 || plc_cmd_code == 4)
                {
                    double E_1 = (double)bytes_buff[13] * 256 * 256 * 256 + (double)bytes_buff[12] * 256 * 256 + (double)bytes_buff[11] * 256 + (double)bytes_buff[10];
                    double E_2 = (double)bytes_buff[17] * 256 * 256 * 256 + (double)bytes_buff[16] * 256 * 256 + (double)bytes_buff[15] * 256 + (double)bytes_buff[14];
                    double E_3 = (double)bytes_buff[21] * 256 * 256 * 256 + (double)bytes_buff[20] * 256 * 256 + (double)bytes_buff[19] * 256 + (double)bytes_buff[18];

                    Message(this, new MessageDataEventArgs() {
                        MessageType = MessageType.Good,
                        MessageString = "Прочитано №" + adrs_PLC +
                        ", Тариф 1: " + (((float)E_1) / 1000f).ToString() + " кВт" +
                        ", Тариф 2: " + (((float)E_2) / 1000f).ToString() + " кВт" +
                        ", Тариф 3: " + (((float)E_3) / 1000f).ToString() + " кВт"});
                }
            }
            else Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Устройство №" + adrs_PLC + " не отвечает" + PingStr() });
            //mainForm.PLC_Table_Refresh();
        }
        #endregion
    }
}
