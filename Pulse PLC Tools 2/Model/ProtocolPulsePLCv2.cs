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
    public class JournalForProtocol
    {
        public List<DataGridRow_Log> Events { get; }
        public Journal_type Type { get; }

        public JournalForProtocol(Journal_type type)
        {
            Events = new List<DataGridRow_Log>();
            Type = type;
        }
    }
    public class PLCRequestParamsForProtocol
    {
        public PLC_Request Type { get; }
        public DataGridRow_PLC Device { get; set; }

        public PLCRequestParamsForProtocol(PLC_Request type)
        {
            Type = type;
        }
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

        class CommandProperties
        {
            public string Code { get; set; }
            public int Timeout { get; set; }
            public int MinLength { get; set; }
        }

        public enum HandleResult : int
        {
            Ok,
            Continue,
            Error
        }
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
        private Dictionary<Commands, CommandProperties> CommandProps;

        public PulsePLCv2Protocol()
        {
            InitCommandProperties();

            //Ограничивает время ожидания ответа
            timer_Timeout = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 10) };
            timer_Timeout.Tick += Timer_Timeout_Tick;
            //Показывает есть ли доступ к устройству
            timer_Access = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 30000) };
            timer_Access.Tick += Timer_Access_Tick;
        }

        void InitCommandProperties()
        {
            CommandProps = new Dictionary<Commands, CommandProperties>();
            //---Заполним коды команд---
            //Доступ
            CommandProps.Add(Commands.Check_Pass,       new CommandProperties() { Code = "Ap", MinLength = 0, Timeout = 100 });
            CommandProps.Add(Commands.Close_Session,    new CommandProperties() { Code = "Ac", MinLength = 0, Timeout = 100 });
            //Системные                                                                        
            CommandProps.Add(Commands.Bootloader,       new CommandProperties() { Code = "Su", MinLength = 0, Timeout = 100 });
            CommandProps.Add(Commands.SerialWrite,      new CommandProperties() { Code = "Ss", MinLength = 0, Timeout = 200 });
            CommandProps.Add(Commands.EEPROM_Read_Byte, new CommandProperties() { Code = "Se", MinLength = 0, Timeout = 100 });
            CommandProps.Add(Commands.EEPROM_Burn,      new CommandProperties() { Code = "Sb", MinLength = 0, Timeout = 100 });
            CommandProps.Add(Commands.Clear_Errors,     new CommandProperties() { Code = "Sc", MinLength = 0, Timeout = 100 });
            CommandProps.Add(Commands.Reboot,           new CommandProperties() { Code = "Sr", MinLength = 0, Timeout = 100 });
            CommandProps.Add(Commands.Request_PLC,      new CommandProperties() { Code = "SR", MinLength = 0, Timeout = 60000 });
            //Чтение                                                                           
            CommandProps.Add(Commands.Search_Devices,   new CommandProperties() { Code = "RL", MinLength = 0, Timeout = 500 });
            CommandProps.Add(Commands.Read_Journal,     new CommandProperties() { Code = "RJ", MinLength = 0, Timeout = 200 });
            CommandProps.Add(Commands.Read_DateTime,    new CommandProperties() { Code = "RT", MinLength = 0, Timeout = 100 });
            CommandProps.Add(Commands.Read_Main_Params, new CommandProperties() { Code = "RM", MinLength = 0, Timeout = 100 });
            CommandProps.Add(Commands.Read_IMP,         new CommandProperties() { Code = "RI", MinLength = 64, Timeout = 100 });
            CommandProps.Add(Commands.Read_IMP_extra,   new CommandProperties() { Code = "Ri", MinLength = 0, Timeout = 100 });
            CommandProps.Add(Commands.Read_PLC_Table,   new CommandProperties() { Code = "RP", MinLength = 0, Timeout = 200 });
            CommandProps.Add(Commands.Read_PLC_Table_En,new CommandProperties() { Code = "RP", MinLength = 0, Timeout = 200 });
            CommandProps.Add(Commands.Read_E_Current,   new CommandProperties() { Code = "REc", MinLength = 0, Timeout = 100 });
            CommandProps.Add(Commands.Read_E_Start_Day, new CommandProperties() { Code = "REd", MinLength = 0, Timeout = 100 });
            CommandProps.Add(Commands.Read_E_Month,     new CommandProperties() { Code = "REm", MinLength = 0, Timeout = 100 });
            //Запись
            CommandProps.Add(Commands.Write_DateTime,   new CommandProperties() { Code = "WT", MinLength = 0, Timeout = 500 });
            CommandProps.Add(Commands.Write_Main_Params, new CommandProperties() { Code = "WM", MinLength = 0, Timeout = 500 });
            CommandProps.Add(Commands.Write_IMP,        new CommandProperties() { Code = "WI", MinLength = 0, Timeout = 500 });
            CommandProps.Add(Commands.Write_PLC_Table,  new CommandProperties() { Code = "WP", MinLength = 0, Timeout = 2000 });
            CommandProps.Add(Commands.Pass_Write,       new CommandProperties() { Code = "Wp", MinLength = 0, Timeout = 500 });
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
                Message(this, new MessageDataEventArgs() { MessageType = MessageType.Error, MessageString = "Нет доступа к данным устройства. Сначала авторизуйтесь." }); return false; }
            if (CurrentCommand == Commands.Read_Journal)        return CMD_Read_Journal((Journal_type)param);
            if (CurrentCommand == Commands.Read_DateTime)       return CMD_Read_DateTime();
            if (CurrentCommand == Commands.Read_Main_Params)    return CMD_Read_Main_Params();
            if (CurrentCommand == Commands.Read_IMP)            return CMD_Read_Imp_Params((ImpNum)param);
            if (CurrentCommand == Commands.Read_IMP_extra)      return CMD_Read_Imp_Extra_Params((ImpNum)param);
            if (CurrentCommand == Commands.Read_PLC_Table_En)   return CMD_Read_PLC_Table(new List<DataGridRow_PLC>());
            if (CurrentCommand == Commands.Read_PLC_Table)      return CMD_Read_PLC_Table((List<DataGridRow_PLC>)param);
            if (CurrentCommand == Commands.Read_E_Current)      return CMD_Read_E_Current((byte)param);
            if (CurrentCommand == Commands.Read_E_Start_Day)    return CMD_Read_E_Start_Day((byte)param);
            //Доступ - Запись
            if (access != AccessType.Write) { Message(this, new MessageDataEventArgs() { MessageType = MessageType.Error, MessageString = "Нет доступа к записи параметров на устройство." }); return false; }
            if (CurrentCommand == Commands.Bootloader)         return CMD_BOOTLOADER();
            if (CurrentCommand == Commands.SerialWrite)        return CMD_SerialWrite((PulsePLCv2LoginPass)param);
            if (CurrentCommand == Commands.Pass_Write)         return CMD_Pass_Write((DeviceMainParams)param);
            if (CurrentCommand == Commands.EEPROM_Burn)        return CMD_EEPROM_BURN();
            if (CurrentCommand == Commands.EEPROM_Read_Byte)   return CMD_EEPROM_Read_Byte((UInt16)param);
            if (CurrentCommand == Commands.Reboot)             return CMD_Reboot();
            if (CurrentCommand == Commands.Clear_Errors)       return CMD_Clear_Errors();
            if (CurrentCommand == Commands.Write_DateTime)     return CMD_Write_DateTime((DateTime)param);
            if (CurrentCommand == Commands.Write_Main_Params)  return CMD_Write_Main_Params((DeviceMainParams)param);
            if (CurrentCommand == Commands.Write_IMP)          return CMD_Write_Imp_Params((ImpParams)param);
            if (CurrentCommand == Commands.Write_PLC_Table)    return CMD_Write_PLC_Table((List<DataGridRow_PLC>)param);
            if (CurrentCommand == Commands.Request_PLC)        return CMD_Request_PLC((PLCRequestParamsForProtocol)param);
            return false;
        }

        bool Check(byte commandName, Commands checkCommand)
        {
            //Проверим ждем ли ответ на эту команду
            if (CurrentCommand != checkCommand) return false;
            //Если ждем ответ то проверяем символ - код команды в ответе
            string cmdCode = CommandProps[checkCommand].Code;
            if (cmdCode[cmdCode.Length-1] == commandName) return true;
            return false;
        }

        HandleResult Handle(byte[] rxBytes, int length)
        {
            if(rxBytes[0] == 0 &&  rxBytes[1] == 'P' && rxBytes[2] == 'l' && rxBytes[3] == 's' )
            {
                byte CMD_Type = rxBytes[4];
                byte CMD_Name = rxBytes[5];

                //Доступ
                if (CMD_Type == 'A')
                {
                    if (Check(CMD_Name, Commands.Check_Pass)) { CMD_Check_Pass(rxBytes);    return HandleResult.Ok; }
                }
                //Системные команды
                if (CMD_Type == 'S')
                {
                    if (Check(CMD_Name, Commands.Bootloader))       { CMD_BOOTLOADER(rxBytes);          return HandleResult.Ok; }
                    if (Check(CMD_Name, Commands.SerialWrite))      { CMD_SerialWrite(rxBytes);         return HandleResult.Ok; }
                    if (Check(CMD_Name, Commands.Reboot))           { CMD_Reboot(rxBytes);              return HandleResult.Ok; }
                    if (Check(CMD_Name, Commands.EEPROM_Burn))      { CMD_EEPROM_BURN(rxBytes);         return HandleResult.Ok; }
                    if (Check(CMD_Name, Commands.EEPROM_Read_Byte)) { CMD_EEPROM_Read_Byte(rxBytes);    return HandleResult.Ok; }
                    if (Check(CMD_Name, Commands.Clear_Errors))     { CMD_Clear_Errors(rxBytes);        return HandleResult.Ok; }
                    if (Check(CMD_Name, Commands.Request_PLC))      { CMD_Request_PLC(rxBytes); return HandleResult.Ok; }
                }
                //Команды чтения
                if (CMD_Type == 'R')
                {
                    if (Check(CMD_Name, Commands.Search_Devices))   { if (CMD_Search_Devices(rxBytes))  return HandleResult.Ok; else return HandleResult.Continue; }
                    if (Check(CMD_Name, Commands.Read_Journal))     { CMD_Read_Journal(rxBytes);        return HandleResult.Ok; }
                    if (Check(CMD_Name, Commands.Read_DateTime))    { CMD_Read_DateTime(rxBytes);       return HandleResult.Ok; }
                    if (Check(CMD_Name, Commands.Read_Main_Params)) { CMD_Read_Main_Params(rxBytes);    return HandleResult.Ok; }
                    if (Check(CMD_Name, Commands.Read_IMP))         { CMD_Read_Imp_Params(rxBytes);     return HandleResult.Ok; }
                    if (Check(CMD_Name, Commands.Read_IMP_extra))   { CMD_Read_Imp_Extra_Params(rxBytes); return HandleResult.Ok; }
                    if (CMD_Name == 'P' && (CurrentCommand == Commands.Read_PLC_Table ||
                                            CurrentCommand == Commands.Read_PLC_Table_En)) { CMD_Read_PLC_Table(rxBytes); return HandleResult.Ok; }
                    if (CMD_Name == 'E')
                    {
                        if (Check(rxBytes[6], Commands.Read_E_Current)) { CMD_Read_E_Handle(rxBytes);      return HandleResult.Ok; }
                        if (Check(rxBytes[6], Commands.Read_E_Start_Day)) { CMD_Read_E_Handle(rxBytes);    return HandleResult.Ok; }
                    }
                }
                //Команды записи
                if (CMD_Type == 'W')
                {
                    if (Check(CMD_Name, Commands.Pass_Write))       { CMD_Pass_Write(rxBytes);          return HandleResult.Ok; }
                    if (Check(CMD_Name, Commands.Write_DateTime))   { CMD_Write_DateTime(rxBytes);      return HandleResult.Ok; }
                    if (Check(CMD_Name, Commands.Write_Main_Params)) { CMD_Write_Main_Params(rxBytes);  return HandleResult.Ok; }
                    if (Check(CMD_Name, Commands.Write_IMP))        { CMD_Write_Imp_Params(rxBytes);    return HandleResult.Ok; }
                    if (Check(CMD_Name, Commands.Write_PLC_Table))  { CMD_Write_PLC_Table(rxBytes);     return HandleResult.Ok; }
                }
            }
            return HandleResult.Error;
        }
        
        public void DateRecieved(object sender, LinkRxEventArgs e)
        {
            //Забираем данные
            for (int i = 0; i < e.Buffer.Length; i++)
            {
                Rx_Bytes = Rx_Bytes.Add(e.Buffer[i]);
                if (CRC16.ComputeChecksum(Rx_Bytes, Rx_Bytes.Length) == 0 && 
                    Rx_Bytes.Length >= CommandProps[CurrentCommand].MinLength) break;
            }

            //Проверяем CRC16
            if (CRC16.ComputeChecksum(Rx_Bytes, Rx_Bytes.Length) == 0)
            {
                HandleResult handle_code = Handle(Rx_Bytes, Rx_Bytes.Length);

                //Комманда выполнена успешно
                if (handle_code == HandleResult.Ok)
                {
                    Message(this, new MessageDataEventArgs() { Data = Rx_Bytes, Length = Rx_Bytes.Length, MessageType = MessageType.ReceiveBytes });
                    Request_End(true);
                    //Обновляем таймер доступа (в устройстве он обновляется при получении команды по интерфейсу)
                    timer_Access.Stop();
                    timer_Access.Start();
                    return;
                }

                //Получилось обработать и ждем следующую часть сообщения
                if (handle_code == HandleResult.Continue)
                {
                    Message(this, new MessageDataEventArgs() { Data = Rx_Bytes, Length = Rx_Bytes.Length, MessageType = MessageType.ReceiveBytes });
                    ping.Restart();
                    Rx_Bytes = new byte[0];
                    return;
                }

                //Не верный формат сообщения
                if(handle_code == HandleResult.Error)
                {
                    //Отправим в Log окно 
                    Message(this, new MessageDataEventArgs() { Data = Rx_Bytes, Length = Rx_Bytes.Length, MessageType = MessageType.ReceiveBytes });
                    Message(this, new MessageDataEventArgs() { MessageType = MessageType.Error, MessageString = "Неверный формат ответа" });
                    Message(this, new MessageDataEventArgs() { MessageType = MessageType.ToolBarInfo, MessageString = "Неверный формат ответа. Попробуйте еще раз." });
                    Request_End(false);
                }
            }
        }
        
        private void Timer_Timeout_Tick(object sender, EventArgs e)
        {
            //Если ожидали данных но не дождались
            if (CurrentCommand != Commands.None)
            {
                if (CurrentCommand == Commands.Close_Session) { Request_End(true); return; }
                if (CurrentCommand == Commands.Search_Devices) { Request_End(true); return; }
                if (CRC16.ComputeChecksum(Rx_Bytes, Rx_Bytes.Length) == 0 || Rx_Bytes.Length == 0)
                    Message(this, new MessageDataEventArgs() { MessageType = MessageType.Error, MessageString = "Истекло время ожидания ответа" });
                else
                {
                    //Отправим в Log окно
                    Message(this, new MessageDataEventArgs() { Data = Rx_Bytes, Length = Rx_Bytes.Length, MessageType = MessageType.ReceiveBytes });
                    Message(this, new MessageDataEventArgs() { MessageType = MessageType.Error, MessageString = "Неверная контрольная сумма" });
                }
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
            timer_Timeout.Interval = new TimeSpan(0, 0, 0, 0, CurrentLink.LinkDelay + CommandProps[CurrentCommand].Timeout);
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
        private void Add_Tx(ushort data)
        {
            Add_Tx((byte)(data >> 8));
            Add_Tx((byte)(data >> 0));
        }
        private void Add_Tx(uint data) {
            Add_Tx((byte)(data >> 24));
            Add_Tx((byte)(data >> 16));
            Add_Tx((byte)(data >> 8));
            Add_Tx((byte)(data >> 0));
        }
        private void Start_Add_Tx(Commands cmd)
        {
            Clear_Tx(); //Очистим буфер передачи
            Add_Tx(0);  //Добавим первый байт сообщение
            Add_Tx(Encoding.UTF8.GetBytes("Pls" + CommandProps[cmd].Code));//Добавим остальные байты начала сообщения
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
            Add_Tx(Convert.ToByte(((byte)imp_num).ToString()[0]));
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Чтение настроек " + imp_num });
            return Request_Start();
        }
        //Обработка ответа
        private void CMD_Read_Imp_Params(byte[] rxBytes)
        {
            ImpParams Imp = new ImpParams();
            if (rxBytes[6] != '1' && rxBytes[6] != '2') return;
            Imp.Num = (rxBytes[6] == '1') ? ImpNum.IMP1 : ImpNum.IMP2;
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
                Imp.Ascue_pass = new[] { rxBytes[pntr++], rxBytes[pntr++], rxBytes[pntr++], rxBytes[pntr++], rxBytes[pntr++], rxBytes[pntr++] };
                //Эмуляция переполнения (1)
                Imp.Perepoln = (ImpOverflowType)rxBytes[pntr++];
                //Передаточное число (2)
                Imp.A = (UInt16)((rxBytes[pntr++] << 8) + rxBytes[pntr++]);
                //Тарифы (11)
                Imp.T_qty = (ImpNumOfTarifs)rxBytes[pntr++];
                Imp.T1_Time_1 = new ImpTime(rxBytes[pntr++], rxBytes[pntr++]);
                Imp.T3_Time_1 = new ImpTime(rxBytes[pntr++], rxBytes[pntr++]);
                Imp.T1_Time_2 = new ImpTime(rxBytes[pntr++], rxBytes[pntr++]);
                Imp.T3_Time_2 = new ImpTime(rxBytes[pntr++], rxBytes[pntr++]);
                Imp.T2_Time = new ImpTime(rxBytes[pntr++], rxBytes[pntr++]);
                //Показания - Текущие (12)

                Imp.E_Current = new ImpEnergyGroup(true);
                uint E = rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                Imp.E_Current.E_T1.Value_Wt = E;
                E = rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                Imp.E_Current.E_T2.Value_Wt = E;
                E = rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                Imp.E_Current.E_T3.Value_Wt = E;
                //Показания - На начало суток (12)
                E = rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                Imp.E_StartDay.E_T1.Value_Wt = E;
                E = rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                Imp.E_StartDay.E_T2.Value_Wt = E;
                E = rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                E = (E << 8) + rxBytes[pntr++];
                Imp.E_StartDay.E_T3.Value_Wt = E;
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
            DataContainer.Data = Imp;
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
            Add_Tx((byte)imp_);
            //Отправляем запрос
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Чтение состояния " + imp_num });
            return Request_Start();
        }
        //Обработка ответа
        private void CMD_Read_Imp_Extra_Params(byte[] rxBytes)
        {
            ImpExParams ImpEx = new ImpExParams(ImpNum.IMP1);
            if (rxBytes[6] != '1' && rxBytes[6] != '2') return;
            if (rxBytes[6] == '2') ImpEx.Num = ImpNum.IMP2;
            int pntr = 7;
            ImpEx.CurrentTarif = rxBytes[pntr++];
            ImpEx.ImpCounter = new[] { rxBytes[pntr++], rxBytes[pntr++] }.ToUint16(false);
            ImpEx.MsFromLastImp = new[] { rxBytes[pntr++], rxBytes[pntr++], rxBytes[pntr++], rxBytes[pntr++] }.ToUint32(false);
            ImpEx.CurrentPower = new[] { rxBytes[pntr++], rxBytes[pntr++], rxBytes[pntr++], rxBytes[pntr++] }.ToUint32(false);
            ImpEx.ActualAtTime = DateTime.Now;
            //Отобразим
            DataContainer.Data = ImpEx;
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Мгновенные значения " + ImpEx.Num + " считаны" + PingStr() });
        }

        //Запрос ЗАПИСЬ ПАРАМЕТРОВ ИМПУЛЬСНЫХ ВХОДОВ
        private bool CMD_Write_Imp_Params(ImpParams imp)
        {
            Start_Add_Tx(Commands.Write_IMP);
            if (imp.Num == ImpNum.IMP1) Add_Tx("1");
            else if (imp.Num == ImpNum.IMP2) Add_Tx("2");
            else return false;
            ImpParams Imp_ = imp;

            //Параметры
            Add_Tx(Imp_.IsEnable); 
            if (Imp_.IsEnable == 1)
            {
                //
                Add_Tx(Imp_.Adrs_PLC);
                //Тип протокола
                Add_Tx(Imp_.Ascue_protocol);
                //Адрес аскуэ
                Add_Tx(Imp_.Ascue_adrs);
                //Пароль для аскуэ (6)
                Add_Tx(Imp_.Ascue_pass, 6);
                //Эмуляция переполнения (1)
                Add_Tx((byte)Imp_.Perepoln);
                //Передаточное число (2)
                Add_Tx(Imp_.A);
                //Тарифы (11)
                Add_Tx((byte)Imp_.T_qty);
                Add_Tx(Imp_.T1_Time_1.Hours);
                Add_Tx(Imp_.T1_Time_1.Minutes);
                Add_Tx(Imp_.T3_Time_1.Hours);
                Add_Tx(Imp_.T3_Time_1.Minutes);
                Add_Tx(Imp_.T1_Time_2.Hours);
                Add_Tx(Imp_.T1_Time_2.Minutes);
                Add_Tx(Imp_.T3_Time_2.Hours);
                Add_Tx(Imp_.T3_Time_2.Minutes);
                Add_Tx(Imp_.T2_Time.Hours);
                Add_Tx(Imp_.T2_Time.Minutes);
                //Показания (12)
                Add_Tx(Imp_.E_Current.E_T1.Value_Wt);
                Add_Tx(Imp_.E_Current.E_T2.Value_Wt);
                Add_Tx(Imp_.E_Current.E_T3.Value_Wt);
                //Максимальная нагрузка
                Add_Tx(Imp_.Max_Power);
                //Резервные параметры (на будущее)
                Add_Tx(0);
                Add_Tx(0);
                Add_Tx(0);
                Add_Tx(0);
            }
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Запись параметров "+imp.Num });
            return Request_Start();
        }
        //Обработка ответа
        private void CMD_Write_Imp_Params(byte[] bytes_buff)
        {
            if (bytes_buff[6] == 'O' && bytes_buff[7] == 'K') Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Параметры успешно записаны" + PingStr() });
            if (bytes_buff[6] == 'e' && bytes_buff[7] == 'r') Message(this, new MessageDataEventArgs() { MessageType = MessageType.Error, MessageString = "Ошибка при записи" + PingStr() });
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
            JournalForProtocol events = null;
            if (bytes_buff[6] == '1') events = new JournalForProtocol(Journal_type.POWER);
            if (bytes_buff[6] == '2') events = new JournalForProtocol(Journal_type.CONFIG);
            if (bytes_buff[6] == '3') events = new JournalForProtocol(Journal_type.INTERFACES);
            if (bytes_buff[6] == '4') events = new JournalForProtocol(Journal_type.REQUESTS);
            if(events == null)
            {
                Message(this, new MessageDataEventArgs() { MessageType = MessageType.Error, MessageString = "Неопределенный тип журнала" + PingStr() });
                return;
            }
                
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
                events.Events.Add(row);
            }
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Журнал успешно прочитан" + PingStr() });
            DataContainer.Data = events;
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
        private void CMD_Read_DateTime(byte[] rxBytes)
        {
            try
            {
                DataContainer.Data = new DateTime((int)(DateTime.Now.Year / 100) * 100 + rxBytes[11], rxBytes[10], rxBytes[9], rxBytes[8], rxBytes[7], rxBytes[6]);
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
        private bool CMD_Write_DateTime(DateTime dateTime)
        {
            Start_Add_Tx(Commands.Write_DateTime);
            //Данные
            Add_Tx((byte)dateTime.Second);
            Add_Tx((byte)dateTime.Minute);
            Add_Tx((byte)dateTime.Hour);
            Add_Tx((byte)dateTime.Day);
            Add_Tx((byte)dateTime.Month);
            int year_ = dateTime.Year;
            while (year_ >= 100) year_ -= 100;
            Add_Tx((byte)year_);

            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Запись даты/времени (" + dateTime + ")" });
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
        private bool CMD_Read_PLC_Table(List<DataGridRow_PLC> listRows)
        {
            Start_Add_Tx(Commands.Read_PLC_Table);
            //Данные
            byte count_ = listRows == null ? (byte)0 : (byte)listRows.Count;
            if(count_ > 10)
            {
                Message(this, new MessageDataEventArgs() { MessageType = MessageType.Error, MessageString = "Ошибка - Нельзя прочитать больше 10 адресов за 1 раз" });
                return false;
            }
            Add_Tx(count_);
            listRows.ForEach((item) => Add_Tx(item.Adrs_PLC));
            
            //Отправляем запрос
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Чтение таблицы PLC - Активные адреса" });
            return Request_Start();
        }
        //Обработка ответа
        private void CMD_Read_PLC_Table(byte[] rxBytes)
        {
            int count_adrs;
            if (rxBytes[6] == 0)
            {
                List<DataGridRow_PLC> rows = new List<DataGridRow_PLC>();
                count_adrs = rxBytes[7]; //Число адресов в ответе
                for (int i = 0; i < count_adrs; i++)
                {
                    rows.Add(new DataGridRow_PLC(rxBytes[8 + i]));
                }
                DataContainer.Data = rows;
                //Сообщение
                Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Найдено " + rows.Count + " активных адресов" + PingStr() });
            }
            else
            {
                int pntr = 6;
                count_adrs = rxBytes[pntr++];
                int active_adrss = 0; //количество включенных адресов
                string active_adrss_str = "";
                List<DataGridRow_PLC> list = new List<DataGridRow_PLC>();
                for (int i = 1; i <= count_adrs; i++)
                {
                    DataGridRow_PLC row = new DataGridRow_PLC(rxBytes[pntr++]);
                    row.IsEnable = (rxBytes[pntr++] == 0) ? false : true;
                    //Статус связи
                    row.LastPLCRequestStatus = (rxBytes[pntr++] == 0) ? false : true;
                    //Дата последней успешной связи
                    byte Day = rxBytes[pntr++];
                    byte Month = rxBytes[pntr++];
                    byte Year = rxBytes[pntr++];
                    byte Hours = rxBytes[pntr++];
                    byte Minutes = rxBytes[pntr++];
                    DateTime dateTime = DateTime.MinValue;
                    if (DateTime.TryParse(Day + "." + Month + "." + Year + " " + Hours + ":" + Minutes + ":00", out dateTime)) row.LastPLCRequestTime = dateTime;
                    //Тип протокола PLC
                    row.TypePLC = (PLCProtocolType)rxBytes[pntr++];
                    //Качество связи
                    row.Quality = rxBytes[pntr++];
                    if (row.IsEnable)
                    {
                        //For message
                        active_adrss++;
                        if (active_adrss > 1) active_adrss_str += ", ";
                        active_adrss_str += row.Adrs_PLC;

                        //Params
                        //Серийный номер
                        row.Serial = new[] { rxBytes[pntr++], rxBytes[pntr++], rxBytes[pntr++], rxBytes[pntr++] };
                        //Ретрансляция
                        row.N = rxBytes[pntr++];
                        row.S1 = rxBytes[pntr++];
                        row.S2 = rxBytes[pntr++];
                        row.S3 = rxBytes[pntr++];
                        row.S4 = rxBytes[pntr++];
                        row.S5 = rxBytes[pntr++];
                        //Тип протокола
                        row.Protocol_ASCUE = (ImpAscueProtocolType)rxBytes[pntr++];
                        //Адрес аскуэ
                        row.Adrs_ASCUE = rxBytes[pntr++];
                        row.Adrs_ASCUE = (UInt16)((row.Adrs_ASCUE<<8) + rxBytes[pntr++]);
                        //Пароль аскуэ
                        row.Pass_ASCUE = new[] { rxBytes[pntr++], rxBytes[pntr++], rxBytes[pntr++], rxBytes[pntr++], rxBytes[pntr++], rxBytes[pntr++] };
                        //Байт ошибок
                        row.ErrorsByte = rxBytes[pntr++];
                    }
                    list.Add(row);
                }
                DataContainer.Data = list;
                //Сообщение
                Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Прочитано " + count_adrs + " адресов, из них " + active_adrss + " вкл. [" + active_adrss_str + "]" + PingStr() });
            }
        }
        //Запрос ЗАПИСЬ в Таблицу PLC
        private bool CMD_Write_PLC_Table(List<DataGridRow_PLC> listRows)
        {
            if (listRows == null) return false;
            Start_Add_Tx(Commands.Write_PLC_Table);
            if (listRows.Count > 10)
            {
                Message(this, new MessageDataEventArgs() { MessageType = MessageType.Error, MessageString = "Ошибка - Нельзя записать больше 10 адресов за 1 раз" });
                return false;
            }
            //Add data to message
            Add_Tx((byte)listRows.Count);
            for (byte i = 0; i < listRows.Count; i++)
            {
                Add_Tx(listRows[i].Adrs_PLC);
                Add_Tx(listRows[i].IsEnable ? (byte)1 : (byte)0);
                if(listRows[i].IsEnable)
                {
                    //Серийный номер (4 байта)
                    Add_Tx(listRows[i].Serial, 4);
                    //Ретрансляция (6 байт)
                    Add_Tx(listRows[i].N);
                    Add_Tx(listRows[i].S1);
                    Add_Tx(listRows[i].S2);
                    Add_Tx(listRows[i].S3);
                    Add_Tx(listRows[i].S4);
                    Add_Tx(listRows[i].S5);
                    //Тип протокола аскуэ (1 байт)
                    Add_Tx((byte)listRows[i].Protocol_ASCUE);
                    //Адрес аскуэ (2 байт)
                    Add_Tx(listRows[i].Adrs_ASCUE);
                    //Пароль аскуэ (6 байт)
                    Add_Tx(listRows[i].Pass_ASCUE, 6);
                    //Версия PLC
                    Add_Tx((byte)listRows[i].TypePLC);
                    //Errors byte (only for read)
                    //Last link state (only for read)
                }
            }

            //Отправляем запрос
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Запись в таблицу PLC" });
            return Request_Start();
        }
        //Обработка ответа
        private void CMD_Write_PLC_Table(byte[] rxBytes)
        {
            if (rxBytes[6] == 'O' && rxBytes[7] == 'K') Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Строки успешно записаны в PLC таблицу" + PingStr() });
            if (rxBytes[6] == 'e' && rxBytes[7] == 'r') Message(this, new MessageDataEventArgs() { MessageType = MessageType.Error, MessageString = "Ошибка при записи в PLC таблицу." + PingStr() });
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
        private void CMD_Read_E_Handle(byte[] bytes_buff)
        {
            ImpEnergyGroup energy = new ImpEnergyGroup(true);
            DataGridRow_PLC row = new DataGridRow_PLC(bytes_buff[7]);
            //Получаем данные
            int parametr = bytes_buff[8]; //номер месяца
            int pntr = 9;
            energy.E_T1.Value_Wt =  new[] { bytes_buff[pntr++], bytes_buff[pntr++], bytes_buff[pntr++], bytes_buff[pntr++] }.ToUint32(false);
            energy.E_T2.Value_Wt =  new[] { bytes_buff[pntr++], bytes_buff[pntr++], bytes_buff[pntr++], bytes_buff[pntr++] }.ToUint32(false);
            energy.E_T3.Value_Wt =  new[] { bytes_buff[pntr++], bytes_buff[pntr++], bytes_buff[pntr++], bytes_buff[pntr++] }.ToUint32(false);
            //4 байта запас на 4й тариф
            pntr += 4;
            energy.IsCorrect = (bytes_buff[pntr++] == 177) ? true : false;

            string type_E = "";
            if (CurrentCommand == Commands.Read_E_Current)
            {
                type_E = "Текущие показания";
                row.E_Current = energy;
            }
            if (CurrentCommand == Commands.Read_E_Start_Day)
            {
                type_E = "Начало суток";
                row.E_StartDay = energy;
            }
            //Сообщение
            if (energy.IsCorrect)
                Message(this, new MessageDataEventArgs() {
                    MessageType = MessageType.Good,
                    MessageString = "Прочитано - " + type_E + " " + row.Adrs_PLC + 
                    ": T1 (" + energy.E_T1.Value_kWt + 
                    "), T2 (" + energy.E_T2.Value_kWt + 
                    "), T3 (" + energy.E_T3.Value_kWt + ")" + PingStr()
                });
            else
                Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Прочитано - " + type_E + " " + row.Adrs_PLC + ": Н/Д" + PingStr() });
            //Put data
            DataContainer.Data = row;
        }
        #endregion

        #region PLC запросы
        //Команда Отправка запроса PLC
        private bool CMD_Request_PLC(PLCRequestParamsForProtocol param)
        {
            Start_Add_Tx(Commands.Request_PLC);

            //Тип запроса по PLC 1б
            Add_Tx((byte)param.Type);
            //Адрес устройства 1б
            Add_Tx(param.Device.Adrs_PLC);
            //Ретрансляция Количество ступеней 1б 
            Add_Tx(param.Device.N);
            //Адреса ступеней 5б
            Add_Tx(param.Device.Steps, 5);

            //Отправляем запрос
            if (param.Device.N == 0)
                Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Прямой запрос ("+ param.Type + ") PLC на " + param.Device.Adrs_PLC });
            else
            {
                string steps_ = "";
                for (int i = 0; i < param.Device.N; i++) { steps_ += ", " + param.Device.Steps[i]; }
                Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Запрос PLC на " + param.Device.Adrs_PLC + " через " + steps_ });
            }
            return Request_Start();
        }
        //Обработка ответа
        private void CMD_Request_PLC(byte[] rxBytes)
        {
            //Получаем данные
            PLC_Request plc_cmd_code = (PLC_Request)rxBytes[6];
            bool request_status = (rxBytes[7] == 0) ? false : true;
            int adrs_PLC = rxBytes[8];
            int plc_type = rxBytes[9];
            
            string plc_v = "";
            if (plc_type == 11) plc_v = "PLCv1";
            if (plc_type == 22) plc_v = "PLCv2";
            
            //Сообщение
            if (request_status)
            {
                if (plc_cmd_code == PLC_Request.PLCv1)
                {
                    ImpEnergyValue E_ = new ImpEnergyValue(new[] { rxBytes[10], rxBytes[11], rxBytes[12], rxBytes[13] }.ToUint32(true));
                    Message(this, new MessageDataEventArgs() {
                        MessageType = MessageType.Good,
                        MessageString = "Прочитано №" + adrs_PLC + ", Тип: " + plc_v + ", Тариф 1: " + E_.Value_kWt + " кВт" + PingStr()
                    });
                }
                if (plc_cmd_code == PLC_Request.Time_Synchro)
                {
                    string errors_string = "Ошибки: ";
                    if ((rxBytes[10] & 1) > 0) errors_string += "ОБ ";
                    if ((rxBytes[10] & 2) > 0) errors_string += "ББ ";
                    if ((rxBytes[10] & 4) > 0) errors_string += "П1 ";
                    if ((rxBytes[10] & 8) > 0) errors_string += "П2 ";
                    if ((rxBytes[10] & 16) > 0) errors_string += "ОП ";
                    if ((rxBytes[10] & 32) > 0) errors_string += "ОВ ";
                    //!! добавить ошибки ДОДЕЛАТЬStringMessage
                    if (errors_string == "Ошибки: ") errors_string = "Нет ошибок";
                    Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Прочитано №" + adrs_PLC + " " + errors_string + PingStr() });
                }
                if (plc_cmd_code == PLC_Request.Serial_Num)
                {
                    string serial_string = rxBytes[10].ToString("00")+ rxBytes[11].ToString("00")+ rxBytes[12].ToString("00")+ rxBytes[13].ToString("00");
                    Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Прочитано №" + adrs_PLC + " Серийный номер: " + serial_string + PingStr() });
                }
                if (plc_cmd_code == PLC_Request.E_Current || plc_cmd_code == PLC_Request.E_Start_Day)
                {
                    ImpEnergyGroup energy = new ImpEnergyGroup(true);
                    energy.E_T1.Value_Wt = new[] { rxBytes[10], rxBytes[11], rxBytes[12], rxBytes[13] }.ToUint32(true);
                    energy.E_T2.Value_Wt = new[] { rxBytes[14], rxBytes[15], rxBytes[16], rxBytes[17] }.ToUint32(true);
                    energy.E_T3.Value_Wt = new[] { rxBytes[18], rxBytes[19], rxBytes[20], rxBytes[21] }.ToUint32(true);

                    Message(this, new MessageDataEventArgs() {
                        MessageType = MessageType.Good,
                        MessageString = "Прочитано №" + adrs_PLC +
                        ", Тариф 1: " + energy.E_T1.Value_kWt + " кВт" +
                        ", Тариф 2: " + energy.E_T2.Value_kWt + " кВт" +
                        ", Тариф 3: " + energy.E_T3.Value_kWt + " кВт" + PingStr()
                    });
                }
            }
            else Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Устройство №" + adrs_PLC + " не отвечает" + PingStr() });
        }
        #endregion
    }
}
