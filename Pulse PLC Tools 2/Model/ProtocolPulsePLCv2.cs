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
    public enum IMP_type : int { IMP1 = 1, IMP2 }

    //Комманды посылаемые на устройство
    public enum PulsePLCv2Commands : int {
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

    public enum Access_Type : int { No_Access, Read, Write }

    public class PulsePLCv2LoginPass
    {
        public byte[] Login { get; }
        public byte[] Pass { get; }

        public PulsePLCv2LoginPass(byte[] serialNum, byte[] pass)
        {
            Login = serialNum;
            Pass = pass;
        }
    }

    public class PulsePLCv2Protocol : IProtocol, IMessage
    {
        //Пришел ответ на запрос
        public event EventHandler<ProtocolEventArgs> CommandEnd = delegate { };
        public event EventHandler<MessageDataEventArgs> Message = delegate { };

        //Выполняемая сейчас команда
        PulsePLCv2Commands currentCmd = PulsePLCv2Commands.None;
        //Доступ к выполнению команд на устройстве
        Access_Type access = Access_Type.No_Access;

        //Таймеры
        DispatcherTimer timer_Timeout;
        DispatcherTimer timer_Access;
        //Время отправки запроса (для подсчета времени ответа)
        Stopwatch ping = new Stopwatch();

        //Буффер для передачи
        byte[] tx_buf = new byte[512];
        int tx_len;
        //Буффер для приема
        byte[] bytes_buff = new byte[0];

        //Коды команд и их символьные представления для отправки на устройство
        private Dictionary<PulsePLCv2Commands, string> commandCodes = new Dictionary<PulsePLCv2Commands, string>(5);

        MainWindow mainForm;
        public PulsePLCv2Protocol(MainWindow mainForm_)
        {
            mainForm = mainForm_;

            //---Заполним коды команд---
            //Доступ
            commandCodes.Add(PulsePLCv2Commands.Check_Pass,       "Ap");
            commandCodes.Add(PulsePLCv2Commands.Close_Session,    "Ac");
            //Системные
            commandCodes.Add(PulsePLCv2Commands.Bootloader,       "Su");
            commandCodes.Add(PulsePLCv2Commands.SerialWrite,      "Ss");
            commandCodes.Add(PulsePLCv2Commands.EEPROM_Read_Byte, "Se");
            commandCodes.Add(PulsePLCv2Commands.EEPROM_Burn,      "Sb");
            commandCodes.Add(PulsePLCv2Commands.Clear_Errors,     "Sc");
            commandCodes.Add(PulsePLCv2Commands.Reboot,           "Sr");
            commandCodes.Add(PulsePLCv2Commands.Request_PLC,      "SR");
            //Чтение
            commandCodes.Add(PulsePLCv2Commands.Search_Devices,   "RL");
            commandCodes.Add(PulsePLCv2Commands.Read_Journal,     "RJ");
            commandCodes.Add(PulsePLCv2Commands.Read_DateTime,    "RT");
            commandCodes.Add(PulsePLCv2Commands.Read_Main_Params, "RM");
            commandCodes.Add(PulsePLCv2Commands.Read_IMP,         "RI");
            commandCodes.Add(PulsePLCv2Commands.Read_IMP_extra,   "Ri");
            commandCodes.Add(PulsePLCv2Commands.Read_PLC_Table,   "RP");
            commandCodes.Add(PulsePLCv2Commands.Read_PLC_Table_En,"RP");
            commandCodes.Add(PulsePLCv2Commands.Read_E_Current,   "REc");
            commandCodes.Add(PulsePLCv2Commands.Read_E_Start_Day, "REd");
            commandCodes.Add(PulsePLCv2Commands.Read_E_Month,     "REm");
            //Запись
            commandCodes.Add(PulsePLCv2Commands.Write_DateTime,   "WT");
            commandCodes.Add(PulsePLCv2Commands.Write_Main_Params,"WM");
            commandCodes.Add(PulsePLCv2Commands.Write_IMP,        "WI");
            commandCodes.Add(PulsePLCv2Commands.Write_PLC_Table,  "WP");
            commandCodes.Add(PulsePLCv2Commands.Pass_Write,       "Wp");
            //-------------

            //Ограничивает время ожидания ответа
            timer_Timeout = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 10) };
            timer_Timeout.Tick += Timer_Timeout_Tick;
            //Показывает есть ли доступ к устройству
            timer_Access = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 10) };
            timer_Access.Tick += Timer_Access_Tick;
        }

        public bool Send(int cmdCode, ILink link, object param)
        {
            if (link == null) return false;
            if (!link.IsConnected) return false;
            //Приведем код команды к типу PulsePLCv2Commands
            PulsePLCv2Commands cmd = (PulsePLCv2Commands)cmdCode;

            if (cmd == PulsePLCv2Commands.Check_Pass)     return CMD_Check_Pass(link, (PulsePLCv2LoginPass)param);
            if (cmd == PulsePLCv2Commands.Close_Session)  return CMD_Close_Session(link);
            if (cmd == PulsePLCv2Commands.Search_Devices) return CMD_Search_Devices(link);
            //Доступ - Чтение
            if (access != Access_Type.Read && access != Access_Type.Write) {
                Message(this, new MessageDataEventArgs() { MessageType = MessageType.MsgBox, MessageString = "Нет доступа к данным устройства. Сначала авторизуйтесь." }); return false; }
            if (cmd == PulsePLCv2Commands.Read_Journal)       return CMD_Read_Journal(link, (Journal_type)param);
            if (cmd == PulsePLCv2Commands.Read_DateTime)      return CMD_Read_DateTime(link);
            if (cmd == PulsePLCv2Commands.Read_Main_Params)   return CMD_Read_Main_Params(link);
            if (cmd == PulsePLCv2Commands.Read_IMP)           return CMD_Read_Imp_Params(link, (int)param);
            if (cmd == PulsePLCv2Commands.Read_IMP_extra)     return CMD_Read_Imp_Extra_Params(link, (int)param);
            if (cmd == PulsePLCv2Commands.Read_PLC_Table || cmd == PulsePLCv2Commands.Read_PLC_Table_En || cmd == PulsePLCv2Commands.Read_E_Data) return CMD_Read_PLC_Table(cmd, link, (byte[])param);
            if (cmd == PulsePLCv2Commands.Read_E_Current)     return CMD_Read_E_Current(link, (byte)param);
            if (cmd == PulsePLCv2Commands.Read_E_Start_Day)   return CMD_Read_E_Start_Day(link, (byte)param);
            //Доступ - Запись
            if (access != Access_Type.Write) { Message(this, new MessageDataEventArgs() { MessageType = MessageType.MsgBox, MessageString = "Нет доступа к записи параметров на устройство." }); return false; }
            if (cmd == PulsePLCv2Commands.Bootloader)         return CMD_BOOTLOADER(link);
            if (cmd == PulsePLCv2Commands.SerialWrite)        return CMD_SerialWrite(link, (byte[])param);
            if (cmd == PulsePLCv2Commands.Pass_Write)         return CMD_Pass_Write(link, (bool[])param);
            if (cmd == PulsePLCv2Commands.EEPROM_Burn)        return CMD_EEPROM_BURN(link);
            if (cmd == PulsePLCv2Commands.EEPROM_Read_Byte)   return CMD_EEPROM_Read_Byte(link, (UInt16)param);
            if (cmd == PulsePLCv2Commands.Reboot)             return CMD_Reboot(link);
            if (cmd == PulsePLCv2Commands.Clear_Errors)       return CMD_Clear_Errors(link);
            if (cmd == PulsePLCv2Commands.Write_DateTime)     return CMD_Write_DateTime(link);
            if (cmd == PulsePLCv2Commands.Write_Main_Params)  return CMD_Write_Main_Params(link);
            if (cmd == PulsePLCv2Commands.Write_IMP)          return CMD_Write_Imp_Params(link, (int)param);
            if (cmd == PulsePLCv2Commands.Write_PLC_Table)    return CMD_Write_PLC_Table(link, (byte[])param);
            if (cmd == PulsePLCv2Commands.Request_PLC)        return CMD_Request_PLC(link, (byte[])param);
            return false;
        }

        int Handle(byte[] bytes_buff, int length)
        {
            if(bytes_buff[0] == 0 &&  bytes_buff[1] == 'P' && bytes_buff[2] == 'l' && bytes_buff[3] == 's' )
            {
                byte CMD_Type = bytes_buff[4];
                byte CMD_Name = bytes_buff[5];

                //Доступ
                if (CMD_Type == 'A')
                {
                    if (CMD_Name == 'p' && currentCmd == PulsePLCv2Commands.Check_Pass) { CMD_Check_Pass(bytes_buff, length); return 0; }
                }
                //Системные команды
                if (CMD_Type == 'S')
                {
                    if (CMD_Name == 'u' && currentCmd == PulsePLCv2Commands.Bootloader) { CMD_BOOTLOADER(); return 0; }
                    if (CMD_Name == 's' && currentCmd == PulsePLCv2Commands.SerialWrite) { CMD_SerialWrite(bytes_buff, length); return 0; }
                    if (CMD_Name == 'r' && currentCmd == PulsePLCv2Commands.Reboot) { CMD_Reboot(); return 0; }
                    if (CMD_Name == 'b' && currentCmd == PulsePLCv2Commands.EEPROM_Burn) { CMD_EEPROM_BURN(); return 0; }
                    if (CMD_Name == 'e' && currentCmd == PulsePLCv2Commands.EEPROM_Read_Byte) { CMD_EEPROM_Read_Byte(bytes_buff, length); return 0; }
                    if (CMD_Name == 'c' && currentCmd == PulsePLCv2Commands.Clear_Errors) { CMD_Clear_Errors(); return 0; }
                    if (CMD_Name == 'R' && currentCmd == PulsePLCv2Commands.Request_PLC) { CMD_Request_PLC(bytes_buff, length); return 0; }
                }
                //Команды чтения
                if (CMD_Type == 'R')
                {
                    //Поиск устройств
                    if (CMD_Name == 'L' && currentCmd == PulsePLCv2Commands.Search_Devices) { if (CMD_Search_Devices(bytes_buff, length)) return 0; else return 1; }
                    //Чтение журнала
                    if (CMD_Name == 'J' && currentCmd == PulsePLCv2Commands.Read_Journal) { CMD_Read_Journal(bytes_buff, length); return 0; }
                    //Чтение времени
                    if (CMD_Name == 'T' && currentCmd == PulsePLCv2Commands.Read_DateTime) { CMD_Read_DateTime(bytes_buff, length); return 0; }
                    //Чтение основных параметров
                    if (CMD_Name == 'M' && currentCmd == PulsePLCv2Commands.Read_Main_Params) { CMD_Read_Main_Params(bytes_buff, length); return 0; }
                    //Чтение основных параметров
                    if (CMD_Name == 'I' && currentCmd == PulsePLCv2Commands.Read_IMP) { CMD_Read_Imp_Params(bytes_buff, length); return 0; }
                    //Чтение основных параметров
                    if (CMD_Name == 'i' && currentCmd == PulsePLCv2Commands.Read_IMP_extra) { CMD_Read_Imp_Extra_Params(bytes_buff, length); return 0; }
                    //Чтение таблицы PLC - Активные адреса
                    if (CMD_Name == 'P' && (currentCmd == PulsePLCv2Commands.Read_PLC_Table ||
                                            currentCmd == PulsePLCv2Commands.Read_PLC_Table_En ||
                                            currentCmd == PulsePLCv2Commands.Read_E_Data)) { CMD_Read_PLC_Table(bytes_buff, length); return 0; }
                    //Чтение Показаний - Активные адреса
                    if (CMD_Name == 'E') {
                        if (bytes_buff[6] == 'c' && currentCmd == PulsePLCv2Commands.Read_E_Current) { CMD_Read_E(PulsePLCv2Commands.Read_E_Current, bytes_buff, length); return 0; }
                        if (bytes_buff[6] == 'd' && currentCmd == PulsePLCv2Commands.Read_E_Start_Day) { CMD_Read_E(PulsePLCv2Commands.Read_E_Start_Day, bytes_buff, length); return 0; }
                    }
                }
                //Команды записи
                if (CMD_Type == 'W')
                {
                    if (CMD_Name == 'p' && currentCmd == PulsePLCv2Commands.Pass_Write) { CMD_Pass_Write(bytes_buff, length); return 0; }
                    //Запись времени
                    if (CMD_Name == 'T' && currentCmd == PulsePLCv2Commands.Write_DateTime) { CMD_Write_DateTime(bytes_buff, length); return 0; }
                    //Запись основных параметров
                    if (CMD_Name == 'M' && currentCmd == PulsePLCv2Commands.Write_Main_Params) { CMD_Write_Main_Params(bytes_buff, length); return 0; }
                    //Запись параметров импульсных входов
                    if (CMD_Name == 'I' && currentCmd == PulsePLCv2Commands.Write_IMP) { CMD_Write_Imp_Params(bytes_buff, length); return 0; }
                    //Запись таблицы PLC
                    if (CMD_Name == 'P' && currentCmd == PulsePLCv2Commands.Write_PLC_Table) { CMD_Write_PLC_Table(bytes_buff, length); return 0; }
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
                Array.Resize<byte>(ref bytes_buff, bytes_buff.Length + 1);
                bytes_buff[bytes_buff.Length - 1] = e.Buffer[i];
                if (CRC16.ComputeChecksum(bytes_buff, bytes_buff.Length) == 0) break;
            }

            //Проверяем CRC16
            if (CRC16.ComputeChecksum(bytes_buff, bytes_buff.Length) == 0)
            {
                int handle_code = Handle(bytes_buff, bytes_buff.Length);

                //Комманда выполнена успешно
                if (handle_code == 0)
                {
                    Message(this, new MessageDataEventArgs() { Data = bytes_buff, Length = bytes_buff.Length, MessageType = MessageType.ReceiveBytes });
                    Request_End(true);
                    //Обновляем таймер доступа (в устройстве он обновляется при получении команды по интерфейсу)
                    timer_Access.Stop();
                    timer_Access.Start();

                    return;
                }

                //Получилось обработать и ждем следующую часть сообщения
                if (handle_code == 1)
                {
                    Message(this, new MessageDataEventArgs() { Data = bytes_buff, Length = bytes_buff.Length, MessageType = MessageType.ReceiveBytes });
                    //ping_ms = 0;
                    return;
                }

                //Не верный формат сообщения
                if(handle_code == 2)
                {
                    //Отправим в Log окно 
                    Message(this, new MessageDataEventArgs() { Data = bytes_buff, Length = bytes_buff.Length, MessageType = MessageType.ReceiveBytes });
                    Message(this, new MessageDataEventArgs() { MessageType = MessageType.Error, MessageString = "Неверный формат ответа" });
                    Message(this, new MessageDataEventArgs() { MessageType = MessageType.ToolBarInfo, MessageString = "Неверный формат ответа. Попробуйте еще раз." });
                    Request_End(false);
                }
            }
            else //CRC16 != 0
            {
                //Request_End(false);

                //Отправим в Log окно 
                Message(this, new MessageDataEventArgs() { Data = bytes_buff, Length = bytes_buff.Length, MessageType = MessageType.ReceiveBytes });
                Message(this, new MessageDataEventArgs() { MessageType = MessageType.Error, MessageString = "Неверная контрольная сумма" });
                Message(this, new MessageDataEventArgs() { MessageType = MessageType.ToolBarInfo, MessageString = "Неверная контрольная сумма. Попробуйте еще раз." });
            }
        }

        private void Timer_Timeout_Tick(object sender, EventArgs e)
        {
            //Если ожидали данных но не дождались
            if (currentCmd != PulsePLCv2Commands.None)
            {
                if (currentCmd == PulsePLCv2Commands.Close_Session) { Request_End(true); return; }
                if (currentCmd == PulsePLCv2Commands.Search_Devices) { Request_End(true); return; }
                Message(this, new MessageDataEventArgs() { MessageType = MessageType.Error, MessageString = "Истекло время ожидания ответа" });
                //Комманда не выполнилась
                Request_End(false);
            }
        }

        private void Timer_Access_Tick(object sender, EventArgs e)
        {
            timer_Access.Stop();
            //mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            //{
            //    mainForm.connect_Access_timeout.Text = "Не авторизован";
            //}));
        }

        //Выставить флаги о том что запрос отправлен
        private bool Request_Start(ILink link, PulsePLCv2Commands cmd_, int timeout_ms)
        {
            //Текущая команда
            currentCmd = cmd_;
            //Запускаем таймер ожидания ответа
            mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => {
                timer_Timeout.Stop();
                timer_Timeout.Interval = new TimeSpan(0, 0, 0, 0, timeout_ms);
                timer_Timeout.Start();
            }));

            //Добавим контрольную сумму
            Add_Tx(CRC16.ComputeChecksumBytes(tx_buf, tx_len));

            //Время отправки запроса
            ping.Restart();

            if (link.Send(tx_buf, tx_len))
            {
                Message(this, new MessageDataEventArgs() { Data = tx_buf, Length = tx_len, MessageType = MessageType.SendBytes });
                return true;
            }
            return false;
        }

        //Сбросить флаги ожидания ответа на запрос
        private void Request_End(bool status)
        {
            bytes_buff = new byte[0];
            timer_Timeout.Stop();
            currentCmd = PulsePLCv2Commands.None;
            CommandEnd(this, new ProtocolEventArgs(status));
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
        private void Add_Tx(byte data) { tx_buf[tx_len++] = data; }
        private void Add_Tx(byte[] data) { Add_Tx(data, data.Length); }
        private void Add_Tx(byte[] data, int length) { for (int i = 0; i < length; i++) Add_Tx(data[i]); }
        private void Start_Add_Tx(PulsePLCv2Commands cmd)
        {
            Clear_Tx(); //Очистим буфер передачи
            Add_Tx(0);  //Добавим первый байт сообщение
            Add_Tx(Encoding.UTF8.GetBytes("Pls" + commandCodes[cmd]));//Добавим остальные байты начала сообщения
        }

        #region КАНАЛ
        //Запрос ПОИСК УСТРОЙСТВ в канале (и режима работы)
        private bool CMD_Search_Devices(ILink link)
        {
            Start_Add_Tx(PulsePLCv2Commands.Search_Devices);
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Поиск устройств" });

            //Очистим контрол
            mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { mainForm.comboBox_Serial.Items.Clear(); }));

            return Request_Start(link, PulsePLCv2Commands.Search_Devices, link.LinkDelay + 400);
        }
        //Обработка ответа
        private bool CMD_Search_Devices(byte[] bytes_buff, int count)
        {
            int mode = bytes_buff[6];
            string mode_ = "";
            if (mode == 0) mode_ = " [Счетчик]";
            if (mode == 1) mode_ = " [Фаза А]";
            if (mode == 2) mode_ = " [Фаза B]";
            if (mode == 3) mode_ = " [Фаза C]";
            string serial_num = bytes_buff[7].ToString("00") + bytes_buff[8].ToString("00") + bytes_buff[9].ToString("00") + bytes_buff[10].ToString("00");
            mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => {
                mainForm.comboBox_Serial.Items.Add(serial_num + mode_);
                mainForm.comboBox_Serial.SelectedIndex = 0;
            }));
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Ответил " + serial_num + mode_+ PingStr() });
            if (mode == 0 || mode == 3)
                return true;    //заканчиваем
            else
                return false;   //ожидаем следующее сообщение
        }

        //Запрос ПРОВЕРКА ПАРОЛЯ
        private bool CMD_Check_Pass(ILink link, PulsePLCv2LoginPass param)
        {
            Start_Add_Tx(PulsePLCv2Commands.Check_Pass);
            //Серийник
            byte[] serial = param.Login;
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
            return Request_Start(link, PulsePLCv2Commands.Check_Pass, link.LinkDelay + 100);
        }
        //Обработка запроса
        private void CMD_Check_Pass(byte[] bytes_buff, int count)
        {
            string accessStr = "_";
            string service_mode = bytes_buff[6] == 1 ? "[Sercvice mode]" : "";
            if (bytes_buff[7] == 's') { accessStr = "Нет доступа "; access = Access_Type.Write; }
            if (bytes_buff[7] == 'r') { accessStr = "Чтение "; access = Access_Type.Read; }
            if (bytes_buff[7] == 'w') { accessStr = "Запись "; access = Access_Type.Write; }
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Доступ открыт: " + accessStr + service_mode + PingStr() });
            //Таймер доступа ДОДЕЛАТЬ (добавить отображение значка доступа и сообщения типа "Запись/Чтение")
            timer_Access.Start();
        }

        //Запрос ЗАКРЫТИЕ СЕССИИ (закрывает доступ к данным)
        private bool CMD_Close_Session(ILink link)
        {
            //Первые байты по протоколу конфигурации
            Start_Add_Tx(PulsePLCv2Commands.Close_Session);
            access = Access_Type.No_Access;
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.NormalBold, MessageString = "Закрыть сессию" });
            return Request_Start(link, PulsePLCv2Commands.Close_Session, 200);
        }
        #endregion

        #region СЕРВИСНЫЕ КОМАНДЫ

        //Запрос BOOTLOADER (стирает сектор флеш памяти чтобы устройство загружалось в режиме bootloader)
        private bool CMD_BOOTLOADER(ILink link)
        {
            Start_Add_Tx(PulsePLCv2Commands.Bootloader);
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Включить режим обновления" });
            return Request_Start(link, PulsePLCv2Commands.Bootloader, link.LinkDelay + 100);
        }
        //Обработка запроса
        private void CMD_BOOTLOADER()
        {
            Message(this, new MessageDataEventArgs() {
                MessageType = MessageType.Good,
                MessageString = "Теперь отключи питание устройства и подключи по USB к компьютеру. Устройство должно определиться как флеш накопитель." + PingStr()
            });
        }

        //Запрос - Запись серийного номера
        private bool CMD_SerialWrite(ILink link, byte[] params_)
        {
            Start_Add_Tx(PulsePLCv2Commands.SerialWrite);
            Add_Tx(params_, 4); //Новый серийник 4 байта
            Message(this, new MessageDataEventArgs() {
                MessageType = MessageType.Normal,
                MessageString = "ЗАПИСЬ СЕРИЙНОГО НОМЕРА " + params_[0].ToString("00") + params_[1].ToString("00") + params_[2].ToString("00") + params_[3].ToString("00")
            });
            return Request_Start(link, PulsePLCv2Commands.SerialWrite, link.LinkDelay + 500);
        }
        //Обработка запроса
        private void CMD_SerialWrite(byte[] bytes_buff, int count)
        {
            if (bytes_buff[6] == 'O' && bytes_buff[7] == 'K')
                Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "СЕРИЙНЫЙ НОМЕР ЗАПИСАН" + PingStr() });
        }

        //Запрос EEPROM BURN (сброс к заводским настройкам)
        private bool CMD_EEPROM_BURN(ILink link)
        {
            Start_Add_Tx(PulsePLCv2Commands.EEPROM_Burn);
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Сброс к заводским параметрам" });
            return Request_Start(link, PulsePLCv2Commands.EEPROM_Burn, link.LinkDelay + 100);
        }
        //Обработка запроса
        private void CMD_EEPROM_BURN()
        {
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "После перезагрузки устройство запишет в память заводские настройки." + PingStr() });
        }

        //Запрос EEPROM READ BYTE (чтение байта из памяти)
        private bool CMD_EEPROM_Read_Byte(ILink link, UInt16 eep_adrs)
        {
            Start_Add_Tx(PulsePLCv2Commands.EEPROM_Read_Byte);
            Add_Tx((byte)(eep_adrs >> 8));  //Старший
            Add_Tx((byte)(eep_adrs));       //Младший
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Чтение байта EEPROM" });
            return Request_Start(link, PulsePLCv2Commands.EEPROM_Read_Byte, link.LinkDelay + 100);
        }
        //Обработка запроса
        private void CMD_EEPROM_Read_Byte(byte[] bytes_buff, int count)
        {
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Байт прочитан int: " + bytes_buff[6] + ", ASCII: '" + Convert.ToChar(bytes_buff[6]) + "'" + PingStr() });
        }

        //Запрос ПЕРЕЗАГРУЗКА
        private bool CMD_Reboot(ILink link)
        {
            Start_Add_Tx(PulsePLCv2Commands.Reboot);
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Перезагрузить" });
            return Request_Start(link, PulsePLCv2Commands.Reboot, link.LinkDelay + 100);
        }
        //Обработка запроса
        private void CMD_Reboot()
        {
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Устройство перезагружается.." + PingStr() });
        }
        #endregion

        #region ОСНОВНЫЕ ПАРАМЕТРЫ (режимы работы, ошибки, пароли)
        //Запрос - ЧТЕНИЕ ОСНОВНЫХ ПАРАМЕТРОВ 
        private bool CMD_Read_Main_Params(ILink link)
        {
            Start_Add_Tx(PulsePLCv2Commands.Read_Main_Params);
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Чтение основных параметров" });
            return Request_Start(link, PulsePLCv2Commands.Read_Main_Params, link.LinkDelay + 100);
        }
        //Обработка ответа
        private void CMD_Read_Main_Params(byte[] bytes_buff, int count)
        {
            mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => {
                //Версия прошивки
                string version_str = "Версия прошивки: v2." + bytes_buff[7] + "." + bytes_buff[8];
                version_str += "\n";
                version_str += "Разметка памяти: v1." + bytes_buff[6];
                mainForm.label_versions.Content = version_str;
                //Параметры
                mainForm.deviceConfig.Device.Work_mode = bytes_buff[9];
                mainForm.deviceConfig.Device.Mode_No_Battery = bytes_buff[10];
                mainForm.deviceConfig.Device.RS485_Work_Mode = bytes_buff[11];
                mainForm.deviceConfig.Device.Bluetooth_Work_Mode = bytes_buff[12];

                //Ошибки
                byte err_byte = bytes_buff[13];
                mainForm.listErrors.Items.Clear();
                if ((err_byte & 1) > 0) mainForm.listErrors.Items.Add("Проблема с батарейкой");
                if ((err_byte & 2) > 0) mainForm.listErrors.Items.Add("Режим без батарейки");
                if ((err_byte & 4) > 0) mainForm.listErrors.Items.Add("Переполнение IMP1");
                if ((err_byte & 8) > 0) mainForm.listErrors.Items.Add("Переполнение IMP2");
                if ((err_byte & 16) > 0) mainForm.listErrors.Items.Add("Проблема с памятью");
                if ((err_byte & 32) > 0) mainForm.listErrors.Items.Add("Ошибка времени");
                //!! добавить ошибки ДОДЕЛАТЬ
                if (mainForm.listErrors.Items.Count == 0) mainForm.listErrors.Items.Add("Нет ошибок");

                //Покрасим пункт меню в зеленый
                mainForm.treeView_Config_Main.Foreground = Brushes.Green;
            }));
            //Отобразим
            mainForm.deviceConfig.Show_On_Form();

            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Основные параметры успешно прочитаны" + PingStr() });
        }

        //Запрос - ЗАПИСЬ ОСНОВНЫХ ПАРАМЕТРОВ 
        private bool CMD_Write_Main_Params(ILink link)
        {
            Start_Add_Tx(PulsePLCv2Commands.Write_Main_Params);

            //Параметры
            Add_Tx(mainForm.deviceConfig.Device.Work_mode);
            Add_Tx(mainForm.deviceConfig.Device.Mode_No_Battery);
            Add_Tx(mainForm.deviceConfig.Device.RS485_Work_Mode);
            Add_Tx(mainForm.deviceConfig.Device.Bluetooth_Work_Mode);

            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Запись основных параметров" });
            return Request_Start(link, PulsePLCv2Commands.Write_Main_Params, link.LinkDelay + 500);
        }
        //Обработка ответа
        private void CMD_Write_Main_Params(byte[] bytes_buff, int count)
        {
            if (bytes_buff[6] == 'O' && bytes_buff[7] == 'K') Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Основные параметры успешно записаны" + PingStr() });
            if (bytes_buff[6] == 'e' && bytes_buff[7] == 'r') Message(this, new MessageDataEventArgs() { MessageType = MessageType.Error, MessageString = "Ошибка при записи." + PingStr() });
        }

        //Запрос - ОЧИСТИТЬ ФЛАГИ ОШИБОК
        private bool CMD_Clear_Errors(ILink link)
        {
            Start_Add_Tx(PulsePLCv2Commands.Clear_Errors);
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Очистка флагов ошибок." });
            return Request_Start(link, PulsePLCv2Commands.Clear_Errors, link.LinkDelay + 500);
        }
        //Обработка запроса
        private void CMD_Clear_Errors()
        {
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Флаги ошибок сброшены" + PingStr() });
        }

        //Запрос - Запись паролей
        private bool CMD_Pass_Write(ILink link, bool[] params_)
        {
            Start_Add_Tx(PulsePLCv2Commands.Pass_Write);
            //Пароль на запись
            Add_Tx(params_[0] ? (byte)1 : (byte)0);
            for (int i = 0; i < 6; i++)
            {
                Add_Tx(Convert.ToByte(mainForm.deviceConfig.Device.Pass_Write[i]));
            }
            //Пароль на чтение
            Add_Tx(params_[1] ? (byte)1 : (byte)0); //Флаг записи
            for (int i = 0; i < 6; i++)
            {
                Add_Tx(Convert.ToByte(mainForm.deviceConfig.Device.Pass_Read[i]));
            }

            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Запись новых паролей" });
            return Request_Start(link, PulsePLCv2Commands.Pass_Write, link.LinkDelay + 500);
        }
        //Обработка запроса
        private void CMD_Pass_Write(byte[] bytes_buff, int count)
        {
            if (bytes_buff[6] == 'O' && bytes_buff[7] == 'K') Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Пароли успешно записаны" + PingStr() });

            //Отобразим
            mainForm.deviceConfig.Show_On_Form();
        }
        #endregion

        #region ПАРАМЕТРЫ ИМПУЛЬСНЫХ ВХОДОВ
        //Запрос ЧТЕНИЕ ПАРАМЕТРОВ ИМПУЛЬСНЫХ ВХОДОВ
        private bool CMD_Read_Imp_Params(ILink link, int imp_num)
        {
            Start_Add_Tx(PulsePLCv2Commands.Read_IMP);
            byte imp_ = 0;
            if (imp_num == 1) imp_ = Convert.ToByte('1');
            if (imp_num == 2) imp_ = Convert.ToByte('2');
            Add_Tx(imp_);
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Чтение настроек IMP" + imp_num });
            return Request_Start(link, PulsePLCv2Commands.Read_IMP, link.LinkDelay + 100);
        }
        //Обработка ответа
        private void CMD_Read_Imp_Params(byte[] bytes_buff, int count)
        {
            ImpsData Imp_;
            int pntr = 6;
            if (bytes_buff[pntr] == '1') { Imp_ = mainForm.deviceConfig.Imp1; mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { mainForm.treeView_IMP1.Foreground = Brushes.Green; })); }
            else if (bytes_buff[pntr] == '2') { Imp_ = mainForm.deviceConfig.Imp2; mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { mainForm.treeView_IMP2.Foreground = Brushes.Green; }));
        }
            else return;
                pntr++;
                Imp_.Is_Enable = bytes_buff[pntr++];
                if (Imp_.Is_Enable != 0)
                {
                    Imp_.adrs_PLC = bytes_buff[pntr++];
                    //Тип протокола
                    Imp_.ascue_protocol = bytes_buff[pntr++];
                    //Адрес аскуэ
                    Imp_.ascue_adrs = (UInt16)(bytes_buff[pntr++] << 8);
                    Imp_.ascue_adrs += bytes_buff[pntr++];
                    //Пароль для аскуэ (6)
                    Imp_.ascue_pass[0] = bytes_buff[pntr++];
                    Imp_.ascue_pass[1] = bytes_buff[pntr++];
                    Imp_.ascue_pass[2] = bytes_buff[pntr++];
                    Imp_.ascue_pass[3] = bytes_buff[pntr++];
                    Imp_.ascue_pass[4] = bytes_buff[pntr++];
                    Imp_.ascue_pass[5] = bytes_buff[pntr++];
                    //Эмуляция переполнения (1)
                    Imp_.perepoln = bytes_buff[pntr++];
                    //Передаточное число (2)
                    Imp_.A = (UInt16)(bytes_buff[pntr++] << 8);
                    Imp_.A += bytes_buff[pntr++];
                    //Тарифы (11)
                    Imp_.T_qty = bytes_buff[pntr++];
                    Imp_.T1_Time_1 = (UInt16)(bytes_buff[pntr++] * 60);
                    Imp_.T1_Time_1 += bytes_buff[pntr++];
                    Imp_.T3_Time_1 = (UInt16)(bytes_buff[pntr++] * 60);
                    Imp_.T3_Time_1 += bytes_buff[pntr++];
                    Imp_.T1_Time_2 = (UInt16)(bytes_buff[pntr++] * 60);
                    Imp_.T1_Time_2 += bytes_buff[pntr++];
                    Imp_.T3_Time_2 = (UInt16)(bytes_buff[pntr++] * 60);
                    Imp_.T3_Time_2 += bytes_buff[pntr++];
                    Imp_.T2_Time = (UInt16)(bytes_buff[pntr++] * 60);
                    Imp_.T2_Time += bytes_buff[pntr++];
                    //Показания - Текущие (12)
                    Imp_.E_T1 = bytes_buff[pntr++];
                    Imp_.E_T1 = (Imp_.E_T1 << 8) + bytes_buff[pntr++];
                    Imp_.E_T1 = (Imp_.E_T1 << 8) + bytes_buff[pntr++];
                    Imp_.E_T1 = (Imp_.E_T1 << 8) + bytes_buff[pntr++];
                    Imp_.E_T2 = bytes_buff[pntr++];
                    Imp_.E_T2 = (Imp_.E_T2 << 8) + bytes_buff[pntr++];
                    Imp_.E_T2 = (Imp_.E_T2 << 8) + bytes_buff[pntr++];
                    Imp_.E_T2 = (Imp_.E_T2 << 8) + bytes_buff[pntr++];
                    Imp_.E_T3 = bytes_buff[pntr++];
                    Imp_.E_T3 = (Imp_.E_T3 << 8) + bytes_buff[pntr++];
                    Imp_.E_T3 = (Imp_.E_T3 << 8) + bytes_buff[pntr++];
                    Imp_.E_T3 = (Imp_.E_T3 << 8) + bytes_buff[pntr++];
                    //Показания - На начало суток (12)
                    Imp_.E_T1_Start = bytes_buff[pntr++];
                    Imp_.E_T1_Start = (Imp_.E_T1_Start << 8) + bytes_buff[pntr++];
                    Imp_.E_T1_Start = (Imp_.E_T1_Start << 8) + bytes_buff[pntr++];
                    Imp_.E_T1_Start = (Imp_.E_T1_Start << 8) + bytes_buff[pntr++];
                    Imp_.E_T2_Start = bytes_buff[pntr++];
                    Imp_.E_T2_Start = (Imp_.E_T2_Start << 8) + bytes_buff[pntr++];
                    Imp_.E_T2_Start = (Imp_.E_T2_Start << 8) + bytes_buff[pntr++];
                    Imp_.E_T2_Start = (Imp_.E_T2_Start << 8) + bytes_buff[pntr++];
                    Imp_.E_T3_Start = bytes_buff[pntr++];
                    Imp_.E_T3_Start = (Imp_.E_T3_Start << 8) + bytes_buff[pntr++];
                    Imp_.E_T3_Start = (Imp_.E_T3_Start << 8) + bytes_buff[pntr++];
                    Imp_.E_T3_Start = (Imp_.E_T3_Start << 8) + bytes_buff[pntr++];
                    //Максимальная мощность
                    Imp_.max_Power = (UInt16)(bytes_buff[pntr++] << 8);
                    Imp_.max_Power += bytes_buff[pntr++];
                    //Резервные параметры (на будущее)
                    //4 байта
                    byte reserv_ = bytes_buff[pntr++];
                    reserv_ = bytes_buff[pntr++];
                    reserv_ = bytes_buff[pntr++];
                    reserv_ = bytes_buff[pntr++];
                }
                //Отобразим
                mainForm.deviceConfig.Show_On_Form();
                Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Параметры IMP" + Convert.ToChar(bytes_buff[6]) + " успешно прочитаны" + PingStr() });
            }

        //Запрос ЧТЕНИЕ ПАРАМЕТРОВ ИМПУЛЬСНЫХ ВХОДОВ
        private bool CMD_Read_Imp_Extra_Params(ILink link, int imp_num)
        {
            Start_Add_Tx(PulsePLCv2Commands.Read_IMP_extra);
            byte imp_ = 0;
            if (imp_num == 1) imp_ = Convert.ToByte('1');
            if (imp_num == 2) imp_ = Convert.ToByte('2');
            //Параметры
            Add_Tx(imp_);
            //Отправляем запрос
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Чтение состояния IMP" + imp_num });
            return Request_Start(link, PulsePLCv2Commands.Read_IMP_extra, link.LinkDelay + 100);
        }
        //Обработка ответа
        private void CMD_Read_Imp_Extra_Params(byte[] bytes_buff, int count)
        {
            Label label_;
            int pntr = 6;
            if (bytes_buff[pntr] == '1') label_ = mainForm.label_Imp1_Extra;
            else if (bytes_buff[pntr] == '2') label_ = mainForm.label_Imp2_Extra;
            else return;
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
            mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => {
                label_.Content = "Текущие параметры: \n";
                label_.Content += "Текущий тариф: " + imp_Tarif + "\n";
                label_.Content += "Импульсы: "+ imp_Counter+"\n";
                label_.Content += "Время импульса: " + (imp_last_imp_ms/1000f).ToString("#0.00") + " сек\n";
                label_.Content += "Нагрузка: " + imp_P + " Вт\n";
            }));
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Мгновенные значения считаны" + PingStr() });
        }

        //Запрос ЗАПИСЬ ПАРАМЕТРОВ ИМПУЛЬСНЫХ ВХОДОВ
        private bool CMD_Write_Imp_Params(ILink link, int imp_num)
        {
            ImpsData Imp_;
            if (imp_num == 1) Imp_ = mainForm.deviceConfig.Imp1;
            else if (imp_num == 2) Imp_ = mainForm.deviceConfig.Imp2;
            else return false;
            
            Start_Add_Tx(PulsePLCv2Commands.Write_IMP);
            Add_Tx((byte)(Convert.ToByte('0') + imp_num));
            //Параметры
            Add_Tx(Imp_.Is_Enable); //7
            if (Imp_.Is_Enable == 1)
            {
                //
                Add_Tx(Imp_.adrs_PLC);
                //Тип протокола
                Add_Tx(Imp_.ascue_protocol);
                //Адрес аскуэ
                Add_Tx((byte)(Imp_.ascue_adrs >> 8));
                Add_Tx((byte)Imp_.ascue_adrs);
                //Пароль для аскуэ (6)
                Add_Tx(Imp_.ascue_pass, 6);
                //Эмуляция переполнения (1)
                Add_Tx(Imp_.perepoln);
                //Передаточное число (2)
                Add_Tx((byte)(Imp_.A >> 8));
                Add_Tx((byte)Imp_.A);
                //Тарифы (11)
                Add_Tx(Imp_.T_qty);
                Add_Tx((byte)(Imp_.T1_Time_1 / 60));
                Add_Tx((byte)(Imp_.T1_Time_1 % 60));
                Add_Tx((byte)(Imp_.T3_Time_1 / 60));
                Add_Tx((byte)(Imp_.T3_Time_1 % 60));
                Add_Tx((byte)(Imp_.T1_Time_2 / 60));
                Add_Tx((byte)(Imp_.T1_Time_2 % 60));
                Add_Tx((byte)(Imp_.T3_Time_2 / 60));
                Add_Tx((byte)(Imp_.T3_Time_2 % 60));
                Add_Tx((byte)(Imp_.T2_Time / 60));
                Add_Tx((byte)(Imp_.T2_Time % 60));
                //Показания (12)
                Add_Tx((byte)(Imp_.E_T1 >> 24));
                Add_Tx((byte)(Imp_.E_T1 >> 16));
                Add_Tx((byte)(Imp_.E_T1 >> 8));
                Add_Tx((byte)Imp_.E_T1);
                Add_Tx((byte)(Imp_.E_T2 >> 24));
                Add_Tx((byte)(Imp_.E_T2 >> 16));
                Add_Tx((byte)(Imp_.E_T2 >> 8));
                Add_Tx((byte)Imp_.E_T2);
                Add_Tx((byte)(Imp_.E_T3 >> 24));
                Add_Tx((byte)(Imp_.E_T3 >> 16));
                Add_Tx((byte)(Imp_.E_T3 >> 8));
                Add_Tx((byte)Imp_.E_T3);
                //Максимальная нагрузка
                Add_Tx((byte)(Imp_.max_Power >> 8));
                Add_Tx((byte)Imp_.max_Power);
                //Резервные параметры (на будущее)
                Add_Tx(0);
                Add_Tx(0);
                Add_Tx(0);
                Add_Tx(0);
            }
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Запись параметров IMP"+ imp_num });
            return Request_Start(link, PulsePLCv2Commands.Write_IMP, link.LinkDelay + 500);
        }
        //Обработка ответа
        private void CMD_Write_Imp_Params(byte[] bytes_buff, int count)
        {
            if (bytes_buff[6] == 'O' && bytes_buff[7] == 'K') Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Параметры успешно записаны" + PingStr() });
            if (bytes_buff[6] == 'e' && bytes_buff[7] == 'r') Message(this, new MessageDataEventArgs() { MessageType = MessageType.Error, MessageString = "Ошибка при записи IMP" + PingStr() });
        }
        #endregion

        #region ЧТЕНИЕ ЖУРНАЛА СОБЫТИЙ
        //Запрос ЧТЕНИЕ ЖУРНАЛА СОБЫТИЙ
        private bool CMD_Read_Journal(ILink link, Journal_type journal)
        {
            Start_Add_Tx(PulsePLCv2Commands.Read_Journal);
            //Тип журнала
            Add_Tx(Convert.ToByte(journal + 48)); //Передаем номер в виде ASCII символа
            string jName = "";
            if (journal == Journal_type.CONFIG) jName = "конфигурации";
            if (journal == Journal_type.INTERFACES) jName = "интерфейсов";
            if (journal == Journal_type.POWER) jName = "питания";
            if (journal == Journal_type.REQUESTS) jName = "PLC запросов";
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Чтение журнала "+ jName });
            return Request_Start(link, PulsePLCv2Commands.Read_Journal, link.LinkDelay + 500);
        }
        //Обработка ответа
        private void CMD_Read_Journal(byte[] bytes_buff, int count)
        {
            object dataGrid_journal = null;
            if (bytes_buff[6] == '1') dataGrid_journal = mainForm.dataGrid_Log_Power;
            if (bytes_buff[6] == '2') dataGrid_journal = mainForm.dataGrid_Log_Config;
            if (bytes_buff[6] == '3') dataGrid_journal = mainForm.dataGrid_Log_Interfaces;
            if (bytes_buff[6] == '4') dataGrid_journal = mainForm.dataGrid_Log_Requests;
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
                mainForm.DataGrid_Log_Add_Row((DataGrid)dataGrid_journal, row);
            }
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Журнал успешно прочитан" + PingStr() });
        }
        #endregion

        #region ВРЕМЯ И ДАТА
        //Запрос ЧТЕНИЕ ВРЕМЕНИ И ДАТЫ
        private bool CMD_Read_DateTime(ILink link)
        {
            Start_Add_Tx(PulsePLCv2Commands.Read_DateTime);
            //Отправляем запрос
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Чтение даты/времени" });
            return Request_Start(link, PulsePLCv2Commands.Read_DateTime, link.LinkDelay + 100);
        }
        //Обработка ответа
        private void CMD_Read_DateTime(byte[] bytes_buff, int count)
        {
            
            try
            {
                DateTime datetime_ = new DateTime((int)(DateTime.Now.Year / 100) * 100 + bytes_buff[11], bytes_buff[10], bytes_buff[9], bytes_buff[8], bytes_buff[7], bytes_buff[6]);
                mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => {
                    mainForm.textBox_Date_in_device.Text = datetime_.ToString("dd.MM.yy");
                    mainForm.textBox_Time_in_device.Text = datetime_.ToString("HH:mm:ss");
                    System.TimeSpan diff = datetime_.Subtract(DateTime.Now);
                    mainForm.textBox_Time_difference.Text = diff.ToString("g");
                    mainForm.textBox_Date_in_pc.Text = DateTime.Now.ToString("dd.MM.yy");
                    mainForm.textBox_Time_in_pc.Text = DateTime.Now.ToString("HH:mm:ss");
                    //Покрасим пункт меню в зеленый
                    mainForm.treeView_DateTime.Foreground = Brushes.Green;
                }));
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
        private bool CMD_Write_DateTime(ILink link)
        {
            Start_Add_Tx(PulsePLCv2Commands.Write_DateTime);
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
            return Request_Start(link, PulsePLCv2Commands.Write_DateTime, link.LinkDelay + 100);
        }
        //Обработка ответа
        private void CMD_Write_DateTime(byte[] bytes_buff, int count)
        {
            if (bytes_buff[6] == 'O' && bytes_buff[7] == 'K') Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Дата и время успешно записаны" + PingStr() });
            if (bytes_buff[6] == 'e' && bytes_buff[7] == 'r') Message(this, new MessageDataEventArgs() { MessageType = MessageType.Error, MessageString = "Ошибка при записи даты и времени. Возможно недопустимый формат даты." + PingStr() });
        }
        #endregion

        #region Таблица PLC
        //Запрос ЧТЕНИЕ Таблицы PLC - Активные адреса
        private bool CMD_Read_PLC_Table(PulsePLCv2Commands cmd ,ILink link, byte[] adrs_massiv)
        {
            Start_Add_Tx(PulsePLCv2Commands.Read_PLC_Table);
            //Данные
            byte count_ = adrs_massiv[0];
            Add_Tx(count_);
            for (byte i = 0; i < count_; i++)
            {
                Add_Tx(adrs_massiv[i+1]);
            }
            //Отправляем запрос
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Чтение таблицы PLC - Активные адреса" });
            return Request_Start(link, cmd, link.LinkDelay + 500);
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
                    mainForm.plc_table[bytes_buff[7 + i] - 1].Enable = true;
                }
                mainForm.PLC_Table_Refresh();
                //Сообщение
                Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Найдено " + count_adrs + " активных адресов" + PingStr() });
                //Отправим запрос на данные Таблицы PLC
                if(currentCmd == PulsePLCv2Commands.Read_PLC_Table)
                {
                    mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => {
                        byte[] buff_adrs = new byte[251];
                        byte count_ = 0;
                        //Выделить строки с галочками
                        foreach (DataGridRow_PLC item in mainForm.plc_table)
                        {
                            if (item.Enable) buff_adrs[++count_] = item.Adrs_PLC;
                        }
                        buff_adrs[0] = count_;
                        mainForm.PLC_Table_Send_Data_Request(PulsePLCv2Commands.Read_PLC_Table, buff_adrs);
                        mainForm.CMD_Buffer.Add_CMD(mainForm.link, this, (int)PulsePLCv2Commands.Close_Session, null, 0); //Переделать
                    }));
                }
                //Отправим запрос на данные Показаний
                if (currentCmd == PulsePLCv2Commands.Read_E_Data)
                {
                    mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => {
                        byte[] buff_adrs = new byte[251];
                        byte count_ = 0;
                        //Выделить строки с галочками
                        foreach (DataGridRow_PLC item in mainForm.plc_table)
                        {
                            if (item.Enable) buff_adrs[++count_] = item.Adrs_PLC;
                        }
                        buff_adrs[0] = count_;
                        mainForm.E_Data_Send_Data_Request(buff_adrs);
                        mainForm.CMD_Buffer.Add_CMD(mainForm.link, this, (int)PulsePLCv2Commands.Close_Session, null, 0);
                    }));
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
                    mainForm.plc_table[adrs_plc - 1].Enable = is_en;
                    //Статус связи
                    mainForm.plc_table[adrs_plc - 1].Link = (bytes_buff[pntr++] == 0) ? false : true;
                    //Дата последней успешной связи
                    mainForm.plc_table[adrs_plc - 1].link_Day = bytes_buff[pntr++];
                    mainForm.plc_table[adrs_plc - 1].link_Month = bytes_buff[pntr++];
                    mainForm.plc_table[adrs_plc - 1].link_Year = bytes_buff[pntr++];
                    mainForm.plc_table[adrs_plc - 1].link_Hours = bytes_buff[pntr++];
                    mainForm.plc_table[adrs_plc - 1].link_Minutes = bytes_buff[pntr++];
                    
                    //Тип протокола PLC
                    mainForm.plc_table[adrs_plc - 1].type = bytes_buff[pntr++];
                    //Качество связи
                    mainForm.plc_table[adrs_plc - 1].quality = bytes_buff[pntr++];
                    if (is_en)
                    {
                        active_adrss++;
                        if(active_adrss > 1) active_adrss_str += ", ";
                        active_adrss_str += adrs_plc;
                        //Серийный номер
                        mainForm.plc_table[adrs_plc - 1].serial_bytes[0] = bytes_buff[pntr++];
                        mainForm.plc_table[adrs_plc - 1].serial_bytes[1] = bytes_buff[pntr++];
                        mainForm.plc_table[adrs_plc - 1].serial_bytes[2] = bytes_buff[pntr++];
                        mainForm.plc_table[adrs_plc - 1].serial_bytes[3] = bytes_buff[pntr++];
                        //Ретрансляция
                        mainForm.plc_table[adrs_plc - 1].N = bytes_buff[pntr++];
                        mainForm.plc_table[adrs_plc - 1].S1 = bytes_buff[pntr++];
                        mainForm.plc_table[adrs_plc - 1].S2 = bytes_buff[pntr++];
                        mainForm.plc_table[adrs_plc - 1].S3 = bytes_buff[pntr++];
                        mainForm.plc_table[adrs_plc - 1].S4 = bytes_buff[pntr++];
                        mainForm.plc_table[adrs_plc - 1].S5 = bytes_buff[pntr++];
                        //Тип протокола
                        mainForm.plc_table[adrs_plc - 1].Protocol_ASCUE = bytes_buff[pntr++];
                        //Адрес аскуэ
                        mainForm.plc_table[adrs_plc - 1].Adrs_ASCUE = bytes_buff[pntr++];
                        mainForm.plc_table[adrs_plc - 1].Adrs_ASCUE = (UInt16)((mainForm.plc_table[adrs_plc - 1].Adrs_ASCUE<<8) + bytes_buff[pntr++]);
                        //Пароль аскуэ
                        mainForm.plc_table[adrs_plc - 1].pass_bytes[0] = bytes_buff[pntr++];
                        mainForm.plc_table[adrs_plc - 1].pass_bytes[1] = bytes_buff[pntr++];
                        mainForm.plc_table[adrs_plc - 1].pass_bytes[2] = bytes_buff[pntr++];
                        mainForm.plc_table[adrs_plc - 1].pass_bytes[3] = bytes_buff[pntr++];
                        mainForm.plc_table[adrs_plc - 1].pass_bytes[4] = bytes_buff[pntr++];
                        mainForm.plc_table[adrs_plc - 1].pass_bytes[5] = bytes_buff[pntr++];
                        
                        //Байт ошибок
                        mainForm.plc_table[adrs_plc - 1].errors_byte = bytes_buff[pntr++];
                    }
                }
                //Сообщение
                Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Прочитано " + count_adrs + " адресов, из них " + active_adrss + " вкл. [" + active_adrss_str + "]" + PingStr() });
            }
            
            //Отобразим
            mainForm.PLC_Table_Refresh();
            
        }
        //Запрос ЗАПИСЬ в Таблицу PLC
        private bool CMD_Write_PLC_Table(ILink link, byte[] adrs_massiv)
        {
            Start_Add_Tx(PulsePLCv2Commands.Write_PLC_Table);
            //Данные
            byte count_ = adrs_massiv[0];
            Add_Tx(count_);
            
            for (byte i = 0; i < count_; i++)
            {
                byte adrs_plc = adrs_massiv[i+1];
                Add_Tx(adrs_plc);
                adrs_plc--; //Адреса строк начинаются с 0
                Add_Tx((mainForm.plc_table[adrs_plc].Enable) ? (byte)1 : (byte)0);
                if(mainForm.plc_table[adrs_plc].Enable)
                {
                    //Серийный номер (4 байта)
                    Add_Tx(mainForm.plc_table[adrs_plc].serial_bytes, 4);
                    //Ретрансляция (6 байт)
                    Add_Tx(mainForm.plc_table[adrs_plc].N);
                    Add_Tx(mainForm.plc_table[adrs_plc].S1);
                    Add_Tx(mainForm.plc_table[adrs_plc].S2);
                    Add_Tx(mainForm.plc_table[adrs_plc].S3);
                    Add_Tx(mainForm.plc_table[adrs_plc].S4);
                    Add_Tx(mainForm.plc_table[adrs_plc].S5);
                    //Тип протокола аскуэ (1 байт)
                    Add_Tx(mainForm.plc_table[adrs_plc].Protocol_ASCUE);
                    //Адрес аскуэ (2 байт)
                    Add_Tx((byte)(mainForm.plc_table[adrs_plc].Adrs_ASCUE>>8));
                    Add_Tx((byte)mainForm.plc_table[adrs_plc].Adrs_ASCUE);
                    //Пароль аскуэ (6 байт)
                    Add_Tx(mainForm.plc_table[adrs_plc].pass_bytes, 6);
                    //Версия PLC
                    Add_Tx(mainForm.plc_table[adrs_plc].type);
                    //Байт ошибок
                    //Статус связи
                }
            }

            //Отправляем запрос
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Запись в таблицу PLC" });
            return Request_Start(link, PulsePLCv2Commands.Write_PLC_Table, link.LinkDelay + 1000);
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
        private bool CMD_Read_E_Current(ILink link, byte adrs_dev)
        {
            Start_Add_Tx(PulsePLCv2Commands.Read_E_Current);
            //Адрес устройства
            Add_Tx(adrs_dev);
            
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Чтение Показаний на момент последнего опроса" });
            return Request_Start(link, PulsePLCv2Commands.Read_E_Current, link.LinkDelay + 100);
        }
        //Запрос Чтение показаний на Начало суток
        private bool CMD_Read_E_Start_Day(ILink link, byte adrs_dev)
        {
            Start_Add_Tx(PulsePLCv2Commands.Read_E_Start_Day);
            //Адрес устройства
            Add_Tx(adrs_dev);

            //Отправляем запрос
            Message(this, new MessageDataEventArgs() { MessageType = MessageType.Normal, MessageString = "Показания на начало суток" });
            return Request_Start(link, PulsePLCv2Commands.Read_E_Start_Day, link.LinkDelay + 100);
        }
        //Обработка ответа
        private void CMD_Read_E(PulsePLCv2Commands cmd, byte[] bytes_buff, int count)
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
            if(cmd == PulsePLCv2Commands.Read_E_Current)
            {
                type_E = "Последние показания";
                mainForm.plc_table[adrs_PLC - 1].E_Current_T1 = E_T1.ToString();
                mainForm.plc_table[adrs_PLC - 1].E_Current_T2 = E_T2.ToString();
                mainForm.plc_table[adrs_PLC - 1].E_Current_T3 = E_T3.ToString();
                mainForm.plc_table[adrs_PLC - 1].e_Current_Correct = E_Correct;
            }
            if (cmd == PulsePLCv2Commands.Read_E_Start_Day)
            {
                type_E = "Начало суток";
                mainForm.plc_table[adrs_PLC - 1].E_StartDay_T1 = E_T1.ToString();
                mainForm.plc_table[adrs_PLC - 1].E_StartDay_T2 = E_T2.ToString();
                mainForm.plc_table[adrs_PLC - 1].E_StartDay_T3 = E_T3.ToString();
                mainForm.plc_table[adrs_PLC - 1].e_StartDay_Correct = E_Correct;
            }

            //Сообщение
            if (E_Correct)
                Message(this, new MessageDataEventArgs() {
                    MessageType = MessageType.Good,
                    MessageString = "Прочитано - " + type_E + " " + adrs_PLC + ": T1 (" + (((float)E_T1) / 1000f).ToString() + "), T2 (" + (((float)E_T2) / 1000f).ToString() + "), T3 (" + (((float)E_T3) / 1000f).ToString() + ")" + PingStr()
                });
            else
                Message(this, new MessageDataEventArgs() { MessageType = MessageType.Good, MessageString = "Прочитано - " + type_E + " " + adrs_PLC + ": Н/Д" + PingStr() });

            mainForm.PLC_Table_Refresh();
        }
        #endregion

        #region PLC запросы
        //Команда Отправка запроса PLC
        private bool CMD_Request_PLC(ILink link, byte[] param)
        {
            Start_Add_Tx(PulsePLCv2Commands.Request_PLC);
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
            return Request_Start(link, PulsePLCv2Commands.Request_PLC, link.LinkDelay + delay);
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
            mainForm.PLC_Table_Refresh();
        }
        #endregion
    }
}
