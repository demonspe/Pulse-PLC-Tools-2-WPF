using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;



namespace Pulse_PLC_Tools_2._0
{
    public class ProtocolEventArgs : EventArgs
    {
        public bool IsHaveAnswer { get; set; }
    }

    public enum PLC_Request : int { PLCv1, Time_Synchro, Serial_Num, E_Current, E_Start_Day } //Добавить начало месяца и тд
    public enum Journal_type : int { POWER = 1, CONFIG, INTERFACES, REQUESTS }
    public enum IMP_type : int { IMP1 = 1, IMP2 }
    //Комманды посылаемые на устройство
    public enum Command : int {
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

    public class Protocol
    {
        //Пришел ответ на запрос
        public event EventHandler<ProtocolEventArgs> CommandAnswer = delegate { };
        public event EventHandler<LinkMessageEventArgs> LinkMessage = delegate { };
        public event EventHandler<StringMessageEventArgs> StringMessage = delegate { };

        //Выполняемая сейчас команда
        Command currentCmd = Command.None;
        //Доступ к выполнению команд на устройстве
        Access_Type access = Access_Type.No_Access;

        //Таймеры
        DispatcherTimer timer_Timeout;
        DispatcherTimer timer_Access;
        //Время отправки запроса (для подсчета времени ответа)
        DateTime timeStartRequest;

        //Буффер для передачи
        byte[] tx_buf = new byte[512];
        int len;
        //Буффер для приема
        byte[] bytes_buff = new byte[0];

        MainWindow mainForm;
        public Protocol(MainWindow mainForm_)
        {
            mainForm = mainForm_;
            
            //Ограничивает время ожидания ответа
            timer_Timeout = new DispatcherTimer();
            timer_Timeout.Interval = new TimeSpan(0, 0, 0, 0, 10);
            timer_Timeout.Tick += timer_Timeout_Tick;
            //Показывает есть ли доступ к устройству
            timer_Access = new DispatcherTimer();
            timer_Access.Interval = new TimeSpan(0, 0, 0, 0, 10);
            timer_Access.Tick += timer_Access_Tick;
        }
        

        public bool Send_CMD(Command cmd, ILink link, object param)
        {
            if (link == null) return false;
            if (!link.IsConnected) return false;

            try { mainForm.deviceConfig.Get_From_Form(); } catch { MessageBox.Show("Не все поля конфигурации заполнены должным образом"); return false; }

            if (cmd == Command.Check_Pass)     return CMD_Check_Pass(link);
            if (cmd == Command.Close_Session)  return CMD_Close_Session(link);
            if (cmd == Command.Search_Devices) return CMD_Search_Devices(link);
            //Доступ - Чтение
            if (access != Access_Type.Read && access != Access_Type.Write) {
                MessageBox.Show("Нет доступа к данным устройства. Сначала авторизуйтесь."); return false; }
            if (cmd == Command.Read_Journal)       return CMD_Read_Journal(link, (Journal_type)param);
            if (cmd == Command.Read_DateTime)      return CMD_Read_DateTime(link);
            if (cmd == Command.Read_Main_Params)   return CMD_Read_Main_Params(link);
            if (cmd == Command.Read_IMP)           return CMD_Read_Imp_Params(link, (int)param);
            if (cmd == Command.Read_IMP_extra)     return CMD_Read_Imp_Extra_Params(link, (int)param);
            if (cmd == Command.Read_PLC_Table || cmd == Command.Read_PLC_Table_En || cmd == Command.Read_E_Data) return CMD_Read_PLC_Table(cmd, link, (byte[])param);
            if (cmd == Command.Read_E_Current)     return CMD_Read_E_Current(link, (byte)param);
            if (cmd == Command.Read_E_Start_Day)   return CMD_Read_E_Start_Day(link, (byte)param);
            //Доступ - Запись
            if (access != Access_Type.Write) { MessageBox.Show("Нет доступа к записи параметров на устройство."); return false; }
            if (cmd == Command.Bootloader)         return CMD_BOOTLOADER(link);
            if (cmd == Command.SerialWrite)        return CMD_SerialWrite(link, (byte[])param);
            if (cmd == Command.Pass_Write)         return CMD_Pass_Write(link, (bool[])param);
            if (cmd == Command.EEPROM_Burn)        return CMD_EEPROM_BURN(link);
            if (cmd == Command.EEPROM_Read_Byte)   return CMD_EEPROM_Read_Byte(link, (UInt16)param);
            if (cmd == Command.Reboot)             return CMD_Reboot(link);
            if (cmd == Command.Clear_Errors)       return CMD_Clear_Errors(link);
            if (cmd == Command.Write_DateTime)     return CMD_Write_DateTime(link);
            if (cmd == Command.Write_Main_Params)  return CMD_Write_Main_Params(link);
            if (cmd == Command.Write_IMP)          return CMD_Write_Imp_Params(link, (int)param);
            if (cmd == Command.Write_PLC_Table)    return CMD_Write_PLC_Table(link, (byte[])param);
            if (cmd == Command.Request_PLC)        return CMD_Request_PLC(link, (byte[])param);
            return false;
        }

        public int Handle_Msg(byte[] bytes_buff, int count)
        {
            if(bytes_buff[0] == 0 &&  bytes_buff[1] == 'P' && bytes_buff[2] == 'l' && bytes_buff[3] == 's' )
            {
                byte CMD_Type = bytes_buff[4];
                byte CMD_Name = bytes_buff[5];

                //Доступ
                if (CMD_Type == 'A')
                {
                    if (CMD_Name == 'p' && currentCmd == Command.Check_Pass) { CMD_Check_Pass(bytes_buff, count); return 0; }
                }
                //Системные команды
                if (CMD_Type == 'S')
                {
                    if (CMD_Name == 'u' && currentCmd == Command.Bootloader) { CMD_BOOTLOADER(); return 0; }
                    if (CMD_Name == 's' && currentCmd == Command.SerialWrite) { CMD_SerialWrite(bytes_buff, count); return 0; }
                    if (CMD_Name == 'r' && currentCmd == Command.Reboot) { CMD_Reboot(); return 0; }
                    if (CMD_Name == 'b' && currentCmd == Command.EEPROM_Burn) { CMD_EEPROM_BURN(); return 0; }
                    if (CMD_Name == 'e' && currentCmd == Command.EEPROM_Read_Byte) { CMD_EEPROM_Read_Byte(bytes_buff, count); return 0; }
                    if (CMD_Name == 'c' && currentCmd == Command.Clear_Errors) { CMD_Clear_Errors(); return 0; }
                    if (CMD_Name == 'R' && currentCmd == Command.Request_PLC) { CMD_Request_PLC(bytes_buff, count); return 0; }
                }
                //Команды чтения
                if (CMD_Type == 'R')
                {
                    //Поиск устройств
                    if (CMD_Name == 'L' && currentCmd == Command.Search_Devices) { if (CMD_Search_Devices(bytes_buff, count)) return 0; else return 1; }
                    //Чтение журнала
                    if (CMD_Name == 'J' && currentCmd == Command.Read_Journal) { CMD_Read_Journal(bytes_buff, count); return 0; }
                    //Чтение времени
                    if (CMD_Name == 'T' && currentCmd == Command.Read_DateTime) { CMD_Read_DateTime(bytes_buff, count); return 0; }
                    //Чтение основных параметров
                    if (CMD_Name == 'M' && currentCmd == Command.Read_Main_Params) { CMD_Read_Main_Params(bytes_buff, count); return 0; }
                    //Чтение основных параметров
                    if (CMD_Name == 'I' && currentCmd == Command.Read_IMP) { CMD_Read_Imp_Params(bytes_buff, count); return 0; }
                    //Чтение основных параметров
                    if (CMD_Name == 'i' && currentCmd == Command.Read_IMP_extra) { CMD_Read_Imp_Extra_Params(bytes_buff, count); return 0; }
                    //Чтение таблицы PLC - Активные адреса
                    if (CMD_Name == 'P' && (currentCmd == Command.Read_PLC_Table ||
                                            currentCmd == Command.Read_PLC_Table_En ||
                                            currentCmd == Command.Read_E_Data)) { CMD_Read_PLC_Table(bytes_buff, count); return 0; }
                    //Чтение Показаний - Активные адреса
                    if (CMD_Name == 'E') {
                        if (bytes_buff[6] == 'c' && currentCmd == Command.Read_E_Current) { CMD_Read_E(Command.Read_E_Current, bytes_buff, count); return 0; }
                        if (bytes_buff[6] == 'd' && currentCmd == Command.Read_E_Start_Day) { CMD_Read_E(Command.Read_E_Start_Day, bytes_buff, count); return 0; }
                    }
                }
                //Команды записи
                if (CMD_Type == 'W')
                {
                    if (CMD_Name == 'p' && currentCmd == Command.Pass_Write) { CMD_Pass_Write(bytes_buff, count); return 0; }
                    //Запись времени
                    if (CMD_Name == 'T' && currentCmd == Command.Write_DateTime) { CMD_Write_DateTime(bytes_buff, count); return 0; }
                    //Запись основных параметров
                    if (CMD_Name == 'M' && currentCmd == Command.Write_Main_Params) { CMD_Write_Main_Params(bytes_buff, count); return 0; }
                    //Запись параметров импульсных входов
                    if (CMD_Name == 'I' && currentCmd == Command.Write_IMP) { CMD_Write_Imp_Params(bytes_buff, count); return 0; }
                    //Запись таблицы PLC
                    if (CMD_Name == 'P' && currentCmd == Command.Write_PLC_Table) { CMD_Write_PLC_Table(bytes_buff, count); return 0; }
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
            }

            //Проверяем CRC16
            if (CRC16.ComputeChecksum(bytes_buff, bytes_buff.Length) == 0)
            {
                int handle_code = Handle_Msg(bytes_buff, bytes_buff.Length);

                //Комманда выполнена успешно
                if (handle_code == 0)
                {
                    LinkMessage(this, new LinkMessageEventArgs() { Data = bytes_buff, Length = bytes_buff.Length, Direction = Msg_Direction.Receive });
                    Request_End(true);
                    //Обновляем таймер доступа (в устройстве он обновляется при получении команды по интерфейсу)
                    timer_Access.Stop();
                    timer_Access.Start();

                    return;
                }

                //Получилось обработать и ждем следующую часть сообщения
                if (handle_code == 1)
                {
                    LinkMessage(this, new LinkMessageEventArgs() { Data = bytes_buff, Length = bytes_buff.Length, Direction = Msg_Direction.Receive });
                    //ping_ms = 0;
                    return;
                }

                //Не верный формат сообщения
                if(handle_code == 2)
                {
                    //Отправим в Log окно 
                    LinkMessage(this, new LinkMessageEventArgs() { Data = bytes_buff, Length = bytes_buff.Length, Direction = Msg_Direction.Receive });
                    StringMessage(this, new StringMessageEventArgs() { MessageType = Msg_Type.Error, MessageString = "Неверный формат ответа" });
                    StringMessage(this, new StringMessageEventArgs() { MessageType = Msg_Type.ToolBarInfo, MessageString = "Неверный формат ответа. Попробуйте еще раз." });
                    Request_End(false);
                }
            }
            else //CRC16 != 0
            {
                //Request_End(false);

                //Отправим в Log окно 
                LinkMessage(this, new LinkMessageEventArgs() { Data = bytes_buff, Length = bytes_buff.Length, Direction = Msg_Direction.Receive });
                StringMessage(this, new StringMessageEventArgs() { MessageType = Msg_Type.Error, MessageString = "Неверная контрольная сумма" });
                StringMessage(this, new StringMessageEventArgs() { MessageType = Msg_Type.ToolBarInfo, MessageString = "Неверная контрольная сумма. Попробуйте еще раз." });
            }
        }

        private void timer_Timeout_Tick(object sender, EventArgs e)
        {
                mainForm.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                { mainForm.connect_status_timeout.Text = ""; }));
                //Если ожидали данных но не дождались
                if (currentCmd != Command.None)
                {
                    if (currentCmd == Command.Close_Session) { Request_End(true); return; }
                    if (currentCmd == Command.Search_Devices) { Request_End(true); return; }
                    mainForm.Log_Add_Line("Истекло время ожидания ответа", Msg_Type.Error);
                    mainForm.msg("Истекло время ожидания ответа");
                    //Комманда не выполнилась
                    Request_End(false);
                }
        }

        private void timer_Access_Tick(object sender, EventArgs e)
        {
            timer_Access.Stop();
            mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {
                mainForm.connect_Access_timeout.Text = "Не авторизован";
            }));
        }

        //Выставить флаги о том что запрос отправлен
        bool Request_Start(ILink link, Command cmd_, int timeout_ms)
        {
            currentCmd = cmd_;
            mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => {
                timer_Timeout.Stop();
                timer_Timeout.Interval = new TimeSpan(0, 0, 0, 0, timeout_ms);
                timer_Timeout.Start();
            }));

            //Добавим контрольную сумму
            UInt16 crc_ = CRC16.ComputeChecksum(tx_buf, len);
            tx_buf[len++] = (byte)(crc_);
            tx_buf[len++] = (byte)(crc_ >> 8);

            if (link.Send(tx_buf, len))
            {
                LinkMessage(this, new LinkMessageEventArgs() { Data = tx_buf, Length = len, Direction = Msg_Direction.Send });
                return true;
            }
            return false;
        }

        //Сбросить флаги ожидания ответа на запрос
        public void Request_End(bool status)
        {
            //Разблокируем все вкладки
            //mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => {
            //    mainForm.tabControl_main.IsEnabled = true;
            //}));
            bytes_buff = new byte[0];
            timer_Timeout.Stop();
            currentCmd = Command.None;
            CommandAnswer(this, new ProtocolEventArgs() { IsHaveAnswer = status });
        }



        #region КАНАЛ
        //Запрос ПОИСК УСТРОЙСТВ в канале (и режима работы)
        bool CMD_Search_Devices(ILink link)
        {
            len = 0;
            //Первые байты по протоколу конфигурации
            tx_buf[len++] = 0;
            tx_buf[len++] = Convert.ToByte('P');
            tx_buf[len++] = Convert.ToByte('l');
            tx_buf[len++] = Convert.ToByte('s');
            tx_buf[len++] = Convert.ToByte('R');    //Уровень доступа чтение
            //Код функции ТЕСТ СВЯЗИ
            tx_buf[len++] = Convert.ToByte('L');
            //Отправляем запрос
            mainForm.Log_Add_Line("Отправка запроса - Тест связи", Msg_Type.Normal);
            //Очистим контрол
            mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { mainForm.comboBox_Serial.Items.Clear(); }));

            return Request_Start(link, Command.Search_Devices, 5000);
        }
        //Обработка ответа
        bool CMD_Search_Devices(byte[] bytes_buff, int count)
        {
            int mode = bytes_buff[6];
            string serial_num = bytes_buff[7].ToString("00") + bytes_buff[8].ToString("00") + bytes_buff[9].ToString("00") + bytes_buff[10].ToString("00");
            mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => {
                string mode_ = "";
                if (mode == 0) mode_ = " [Счетчик]";
                if (mode == 1) mode_ = " [Фаза А]";
                if (mode == 2) mode_ = " [Фаза B]";
                if (mode == 3) mode_ = " [Фаза C]";
                mainForm.comboBox_Serial.Items.Add(serial_num + mode_);
                mainForm.comboBox_Serial.SelectedIndex = 0;
                mainForm.Log_Add_Line("Найдено устройство " + serial_num + mode_, Msg_Type.Good);
            }));
            if (mode == 0 || mode == 3)
                return true;    //заканчиваем
            else
                return false;   //ожидаем следующее сообщение
        }

        //Запрос ПРОВЕРКА ПАРОЛЯ
        bool CMD_Check_Pass(ILink link)
        {
            len = 0;
            //Первые байты по протоколу конфигурации
            tx_buf[len++] = 0;
            tx_buf[len++] = Convert.ToByte('P');
            tx_buf[len++] = Convert.ToByte('l');
            tx_buf[len++] = Convert.ToByte('s');
            tx_buf[len++] = Convert.ToByte('A');  
            //Код функции Check Pass
            tx_buf[len++] = Convert.ToByte('p');
            //Серийник
            tx_buf[len++] = mainForm.deviceConfig.Device.serial_num[0];
            tx_buf[len++] = mainForm.deviceConfig.Device.serial_num[1];
            tx_buf[len++] = mainForm.deviceConfig.Device.serial_num[2];
            tx_buf[len++] = mainForm.deviceConfig.Device.serial_num[3];
            //Пароль
            string pass_ = "";
            mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { pass_ = mainForm.textBox_Pass.Text; }));
            int length_ = pass_.Length <= 6 ? pass_.Length : 6;
            for (int i = 0; i < 6; i++)
            {
                if(i < length_)
                    tx_buf[len++] = Convert.ToByte(Convert.ToChar(pass_.Substring(i, 1)));
                else
                    tx_buf[len++] = 0xFF;
            }
            //Отправляем запрос
            mainForm.Log_Add_Line("Авторизация", Msg_Type.Normal);

            return Request_Start(link, Command.Check_Pass, 5000);
        }
        //Обработка запроса
        void CMD_Check_Pass(byte[] bytes_buff, int count)
        {
            string accessStr = "_";
            string service_mode = bytes_buff[6] == 1 ? "[Sercvice mode]" : "";
            if (bytes_buff[7] == 's') { accessStr = "Нет доступа "; access = Access_Type.Write; }
            if (bytes_buff[7] == 'r') { accessStr = "Чтение "; access = Access_Type.Read; }
            if (bytes_buff[7] == 'w') { accessStr = "Запись "; access = Access_Type.Write; }
            mainForm.Log_Add_Line("Доступ открыт: " + accessStr + service_mode, Msg_Type.Good);
            timer_Access.Start();
        }

        //Запрос ЗАКРЫТИЕ СЕССИИ (закрывает доступ к данным)
        bool CMD_Close_Session(ILink link)
        {
            int len = 0;
            //Первые байты по протоколу конфигурации
            tx_buf[len++] = 0;
            tx_buf[len++] = Convert.ToByte('P');
            tx_buf[len++] = Convert.ToByte('l');
            tx_buf[len++] = Convert.ToByte('s');
            tx_buf[len++] = Convert.ToByte('A');
            //Код функции Check Pass
            tx_buf[len++] = Convert.ToByte('c');

            access = Access_Type.No_Access;

            mainForm.Log_Add_Line("Команда - Закрыть сессию", Msg_Type.Normal);
            return Request_Start(link, Command.Close_Session, 100);
        }
        #endregion

        #region СЕРВИСНЫЕ КОМАНДЫ

        //Запрос BOOTLOADER (стирает сектор флеш памяти чтобы устройство загружалось в режиме bootloader)
        bool CMD_BOOTLOADER(ILink link)
        {
            int len = 0;
            //Первые байты по протоколу конфигурации
            tx_buf[len++] = 0;
            tx_buf[len++] = Convert.ToByte('P');
            tx_buf[len++] = Convert.ToByte('l');
            tx_buf[len++] = Convert.ToByte('s');
            tx_buf[len++] = Convert.ToByte('S');
            //Код функции BOOTLOADER
            tx_buf[len++] = Convert.ToByte('u');
            //Отправляем запрос
            mainForm.Log_Add_Line("Отправка запроса - Флаг загрузки BOOT", Msg_Type.Normal);
            return Request_Start(link, Command.Bootloader, 5000);
        }
        //Обработка запроса
        void CMD_BOOTLOADER()
        {
            mainForm.Log_Add_Line("Ок. При следующем включении, устройство запустится в режиме BOOTLOADER", Msg_Type.Good);
        }

        //Запрос - Запись серийного номера
        bool CMD_SerialWrite(ILink link, byte[] params_)
        {
            int len = 0;
            //Первые байты по протоколу конфигурации
            tx_buf[len++] = 0;
            tx_buf[len++] = Convert.ToByte('P');
            tx_buf[len++] = Convert.ToByte('l');
            tx_buf[len++] = Convert.ToByte('s');
            tx_buf[len++] = Convert.ToByte('S');    //Уровень доступа
            //Код функции Serial Write
            tx_buf[len++] = Convert.ToByte('s');
            //Пароль на запись
            tx_buf[len++] = params_[0];
            tx_buf[len++] = params_[1];
            tx_buf[len++] = params_[2];
            tx_buf[len++] = params_[3];

            //Отправляем запрос
            mainForm.Log_Add_Line("ЗАПИСЬ СЕРИЙНОГО НОМЕРА", Msg_Type.Normal);
            return Request_Start(link, Command.SerialWrite, 5000);
        }
        //Обработка запроса
        void CMD_SerialWrite(byte[] bytes_buff, int count)
        {
            if (bytes_buff[6] == 'O' && bytes_buff[7] == 'K') mainForm.Log_Add_Line("СЕРИЙНЫЙ НОМЕР ЗАПИСАН", Msg_Type.Good);

        }

        //Запрос EEPROM BURN (сброс к заводским настройкам)
        bool CMD_EEPROM_BURN(ILink link)
        {
            int len = 0;
            //Первые байты по протоколу конфигурации
            tx_buf[len++] = 0;
            tx_buf[len++] = Convert.ToByte('P');
            tx_buf[len++] = Convert.ToByte('l');
            tx_buf[len++] = Convert.ToByte('s');
            tx_buf[len++] = Convert.ToByte('S');    //Уровень доступа чтение
            //Код функции EEPROM BURN
            tx_buf[len++] = Convert.ToByte('b');
            //Отправляем запрос
            mainForm.Log_Add_Line("Отправка запроса - Инициализация памяти", Msg_Type.Normal);
            return Request_Start(link, Command.EEPROM_Burn, 5000);
        }
        //Обработка запроса
        void CMD_EEPROM_BURN()
        {
            mainForm.Log_Add_Line("Ок. После перезагрузки устройство вернется к заводским настройкам.", Msg_Type.Good);
        }

        //Запрос EEPROM READ BYTE (чтение байта из памяти)
        bool CMD_EEPROM_Read_Byte(ILink link, UInt16 eep_adrs)
        {
            int len = 0;
            //Первые байты по протоколу конфигурации
            tx_buf[len++] = 0;
            tx_buf[len++] = Convert.ToByte('P');
            tx_buf[len++] = Convert.ToByte('l');
            tx_buf[len++] = Convert.ToByte('s');
            tx_buf[len++] = Convert.ToByte('S');    //Уровень доступа чтение
            //Код функции EEPROM Read Byte
            tx_buf[len++] = Convert.ToByte('e');
            tx_buf[len++] = (byte)(eep_adrs >> 8);
            tx_buf[len++] = (byte)(eep_adrs);
            //Отправляем запрос
            mainForm.Log_Add_Line("Отправка запроса - Чтение байта из памяти", Msg_Type.Normal);
            return Request_Start(link, Command.EEPROM_Read_Byte, 5000);
        }
        //Обработка запроса
        void CMD_EEPROM_Read_Byte(byte[] bytes_buff, int count)
        {
            byte data = bytes_buff[6];
            mainForm.Log_Add_Line("Байт прочитан int: " + data + ", ASCII: '" + Convert.ToChar(data)+"'", Msg_Type.Good);
        }

        //Запрос ПЕРЕЗАГРУЗКА
        bool CMD_Reboot(ILink link)
        {
            int len = 0;
            //Первые байты по протоколу конфигурации
            tx_buf[len++] = 0;
            tx_buf[len++] = Convert.ToByte('P');
            tx_buf[len++] = Convert.ToByte('l');
            tx_buf[len++] = Convert.ToByte('s');
            tx_buf[len++] = Convert.ToByte('S');    //Уровень доступа чтение
            //Код функции EEPROM BURN
            tx_buf[len++] = Convert.ToByte('r');
            //Отправляем запрос
            mainForm.Log_Add_Line("Отправка запроса - Перезагрузка.", Msg_Type.Normal);
            return Request_Start(link, Command.Reboot, 5000);
        }
        //Обработка запроса
        void CMD_Reboot()
        {
            mainForm.Log_Add_Line("Устройство перезагружается..", Msg_Type.Good);
        }
#endregion

        #region ОСНОВНЫЕ ПАРАМЕТРЫ (режимы работы, ошибки, пароли)
        //Запрос - ЧТЕНИЕ ОСНОВНЫХ ПАРАМЕТРОВ 
        bool CMD_Read_Main_Params(ILink link)
        {
            int len = 0;
            //Первые байты по протоколу конфигурации
            tx_buf[len++] = 0;
            tx_buf[len++] = Convert.ToByte('P');
            tx_buf[len++] = Convert.ToByte('l');
            tx_buf[len++] = Convert.ToByte('s');
            tx_buf[len++] = Convert.ToByte('R');    //Уровень доступа чтение
            //Код функции чтения основных параметров
            tx_buf[len++] = Convert.ToByte('M');
            //Отправляем запрос
            mainForm.Log_Add_Line("Отправка запроса - Чтение основных параметров устройства", Msg_Type.Normal);
            return Request_Start(link, Command.Read_Main_Params, 5000);
        }
        //Обработка ответа
        void CMD_Read_Main_Params(byte[] bytes_buff, int count)
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
            mainForm.Log_Add_Line("Основные параметры успешно прочитаны", Msg_Type.Good);
        }

        //Запрос - ЗАПИСЬ ОСНОВНЫХ ПАРАМЕТРОВ 
        bool CMD_Write_Main_Params(ILink link)
        {
            int len = 0;
            //Первые байты по протоколу конфигурации
            tx_buf[len++] = 0;
            tx_buf[len++] = Convert.ToByte('P');
            tx_buf[len++] = Convert.ToByte('l');
            tx_buf[len++] = Convert.ToByte('s');
            tx_buf[len++] = Convert.ToByte('W');    //Уровень доступа Запись
            //Код функции чтения основных параметров
            tx_buf[len++] = Convert.ToByte('M');

            //Параметры
            tx_buf[len++] = mainForm.deviceConfig.Device.Work_mode;
            tx_buf[len++] = mainForm.deviceConfig.Device.Mode_No_Battery;
            tx_buf[len++] = mainForm.deviceConfig.Device.RS485_Work_Mode;
            tx_buf[len++] = mainForm.deviceConfig.Device.Bluetooth_Work_Mode;
            //Отправляем запрос
            mainForm.Log_Add_Line("Отправка запроса - Запись основных параметров устройства", Msg_Type.Normal);
            return Request_Start(link, Command.Write_Main_Params, 5000);
        }
        //Обработка ответа
        void CMD_Write_Main_Params(byte[] bytes_buff, int count)
        {
            if (bytes_buff[6] == 'O' && bytes_buff[7] == 'K') mainForm.Log_Add_Line("Основные параметры успешно записаны", Msg_Type.Good);
            if (bytes_buff[6] == 'e' && bytes_buff[7] == 'r') mainForm.Log_Add_Line("Ошибка при записи.", Msg_Type.Error);
        }

        //Запрос - ОЧИСТИТЬ ФЛАГИ ОШИБОК
        bool CMD_Clear_Errors(ILink link)
        {
            int len = 0;
            //Первые байты по протоколу конфигурации
            tx_buf[len++] = 0;
            tx_buf[len++] = Convert.ToByte('P');
            tx_buf[len++] = Convert.ToByte('l');
            tx_buf[len++] = Convert.ToByte('s');
            tx_buf[len++] = Convert.ToByte('S');    //Уровень доступа запись
            //Код функции EEPROM BURN
            tx_buf[len++] = Convert.ToByte('c');
            //Отправляем запрос
            mainForm.Log_Add_Line("Отправка запроса - Очистка флагов ошибок.", Msg_Type.Normal);
            return Request_Start(link, Command.Clear_Errors, 5000);
        }
        //Обработка запроса
        void CMD_Clear_Errors()
        {
            mainForm.Log_Add_Line("Флаги ошибок сброшены", Msg_Type.Good);
        }

        //Запрос - Запись паролей
        bool CMD_Pass_Write(ILink link, bool[] params_)
        {
            int len = 0;
            //Первые байты по протоколу конфигурации
            tx_buf[len++] = 0;
            tx_buf[len++] = Convert.ToByte('P');
            tx_buf[len++] = Convert.ToByte('l');
            tx_buf[len++] = Convert.ToByte('s');
            tx_buf[len++] = Convert.ToByte('W');
            //Код функции Write_Pass
            tx_buf[len++] = Convert.ToByte('p');
            //Пароль на запись
            tx_buf[len++] = params_[0] ? (byte)1 : (byte)0; //Флаг записи
            for (int i = 0; i < 6; i++)
            {
                tx_buf[len++] = Convert.ToByte(mainForm.deviceConfig.Device.Pass_Write[i]);
            }
            //Пароль на чтение
            tx_buf[len++] = params_[1] ? (byte)1 : (byte)0; //Флаг записи
            for (int i = 0; i < 6; i++)
            {
                tx_buf[len++] = Convert.ToByte(mainForm.deviceConfig.Device.Pass_Read[i]);
            }
            //Отправляем запрос
            mainForm.Log_Add_Line("Отправка запроса - Запись новых паролей", Msg_Type.Normal);
            return Request_Start(link, Command.Pass_Write, 5000);
        }
        //Обработка запроса
        void CMD_Pass_Write(byte[] bytes_buff, int count)
        {
            if (bytes_buff[6] == 'O' && bytes_buff[7] == 'K') mainForm.Log_Add_Line("Пароли успешно записаны", Msg_Type.Good);
            //Отобразим
            mainForm.deviceConfig.Show_On_Form();

        }
        #endregion

        #region ПАРАМЕТРЫ ИМПУЛЬСНЫХ ВХОДОВ
        //Запрос ЧТЕНИЕ ПАРАМЕТРОВ ИМПУЛЬСНЫХ ВХОДОВ
        bool CMD_Read_Imp_Params(ILink link, int imp_num)
        {
            int len = 0;
            //Первые байты по протоколу конфигурации
            tx_buf[len++] = 0;
            tx_buf[len++] = Convert.ToByte('P');
            tx_buf[len++] = Convert.ToByte('l');
            tx_buf[len++] = Convert.ToByte('s');
            tx_buf[len++] = Convert.ToByte('R');    //Уровень доступа чтение
            //Код функции чтения основных параметров
            tx_buf[len++] = Convert.ToByte('I');
            byte imp_ = 1;
            if (imp_num == 1) imp_ = Convert.ToByte('1');
            if (imp_num == 2) imp_ = Convert.ToByte('2');
            //Параметры
            tx_buf[len++] = imp_;
            //Отправляем запрос
            mainForm.Log_Add_Line("Отправка запроса - Чтение настроек IMP"+ imp_num, Msg_Type.Normal);
            return Request_Start(link, Command.Read_IMP, 5000);
        }
        //Обработка ответа
        void CMD_Read_Imp_Params(byte[] bytes_buff, int count)
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
                mainForm.Log_Add_Line("Параметры имп. входа " + Convert.ToChar(bytes_buff[6]) + " успешно прочитаны", Msg_Type.Good);
            }
        //Запрос ЧТЕНИЕ ПАРАМЕТРОВ ИМПУЛЬСНЫХ ВХОДОВ
        bool CMD_Read_Imp_Extra_Params(ILink link, int imp_num)
        {
            int len = 0;
            //Первые байты по протоколу конфигурации
            tx_buf[len++] = 0;
            tx_buf[len++] = Convert.ToByte('P');
            tx_buf[len++] = Convert.ToByte('l');
            tx_buf[len++] = Convert.ToByte('s');
            tx_buf[len++] = Convert.ToByte('R');    //Уровень доступа чтение
            //Код функции чтения основных параметров
            tx_buf[len++] = Convert.ToByte('i');
            byte imp_ = 1;
            if (imp_num == 1) imp_ = Convert.ToByte('1');
            if (imp_num == 2) imp_ = Convert.ToByte('2');
            //Параметры
            tx_buf[len++] = imp_;
            //Отправляем запрос
            mainForm.Log_Add_Line("Отправка запроса - Чтение параметров состояния IMP" + imp_num, Msg_Type.Normal);
            return Request_Start(link, Command.Read_IMP_extra, 5000);
        }
        //Обработка ответа
        void CMD_Read_Imp_Extra_Params(byte[] bytes_buff, int count)
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
            mainForm.Log_Add_Line("Мгновенные значения считаны", Msg_Type.Good);
        }
        //Запрос ЗАПИСЬ ПАРАМЕТРОВ ИМПУЛЬСНЫХ ВХОДОВ
        bool CMD_Write_Imp_Params(ILink link, int imp_num)
        {
            ImpsData Imp_;
            if (imp_num == 1) Imp_ = mainForm.deviceConfig.Imp1;
            else if (imp_num == 2) Imp_ = mainForm.deviceConfig.Imp2;
            else return false;

            int tx_len = 0;
            //Первые байты по протоколу конфигурации
            tx_buf[tx_len++] = 0;
            tx_buf[tx_len++] = Convert.ToByte('P');
            tx_buf[tx_len++] = Convert.ToByte('l');
            tx_buf[tx_len++] = Convert.ToByte('s');
            tx_buf[tx_len++] = Convert.ToByte('W');    //Уровень доступа Запись
            //Код функции чтения основных параметров
            tx_buf[tx_len++] = Convert.ToByte('I');
            tx_buf[tx_len++] = (byte)(Convert.ToByte('0') + imp_num);
            //Параметры
            tx_buf[tx_len++] = Imp_.Is_Enable; //7
            if (Imp_.Is_Enable == 1)
            {
                //
                tx_buf[tx_len++] = Imp_.adrs_PLC;
                //Тип протокола
                tx_buf[tx_len++] = Imp_.ascue_protocol;
                //Адрес аскуэ
                tx_buf[tx_len++] = (byte)(Imp_.ascue_adrs >> 8);
                tx_buf[tx_len++] = (byte)Imp_.ascue_adrs;
                //Пароль для аскуэ (6)
                tx_buf[tx_len++] = Imp_.ascue_pass[0];
                tx_buf[tx_len++] = Imp_.ascue_pass[1];
                tx_buf[tx_len++] = Imp_.ascue_pass[2];
                tx_buf[tx_len++] = Imp_.ascue_pass[3];
                tx_buf[tx_len++] = Imp_.ascue_pass[4];
                tx_buf[tx_len++] = Imp_.ascue_pass[5];
                //Эмуляция переполнения (1)
                tx_buf[tx_len++] = Imp_.perepoln;
                //Передаточное число (2)
                tx_buf[tx_len++] = (byte)(Imp_.A >> 8);
                tx_buf[tx_len++] = (byte)Imp_.A;
                //Тарифы (11)
                tx_buf[tx_len++] = Imp_.T_qty;
                tx_buf[tx_len++] = (byte)(Imp_.T1_Time_1 / 60);
                tx_buf[tx_len++] = (byte)(Imp_.T1_Time_1 % 60);
                tx_buf[tx_len++] = (byte)(Imp_.T3_Time_1 / 60);
                tx_buf[tx_len++] = (byte)(Imp_.T3_Time_1 % 60);
                tx_buf[tx_len++] = (byte)(Imp_.T1_Time_2 / 60);
                tx_buf[tx_len++] = (byte)(Imp_.T1_Time_2 % 60);
                tx_buf[tx_len++] = (byte)(Imp_.T3_Time_2 / 60);
                tx_buf[tx_len++] = (byte)(Imp_.T3_Time_2 % 60);
                tx_buf[tx_len++] = (byte)(Imp_.T2_Time / 60);
                tx_buf[tx_len++] = (byte)(Imp_.T2_Time % 60);
                //Показания (12)
                tx_buf[tx_len++] = (byte)(Imp_.E_T1 >> 24);
                tx_buf[tx_len++] = (byte)(Imp_.E_T1 >> 16);
                tx_buf[tx_len++] = (byte)(Imp_.E_T1 >> 8);
                tx_buf[tx_len++] = (byte)Imp_.E_T1;
                tx_buf[tx_len++] = (byte)(Imp_.E_T2 >> 24);
                tx_buf[tx_len++] = (byte)(Imp_.E_T2 >> 16);
                tx_buf[tx_len++] = (byte)(Imp_.E_T2 >> 8);
                tx_buf[tx_len++] = (byte)Imp_.E_T2;
                tx_buf[tx_len++] = (byte)(Imp_.E_T3 >> 24);
                tx_buf[tx_len++] = (byte)(Imp_.E_T3 >> 16);
                tx_buf[tx_len++] = (byte)(Imp_.E_T3 >> 8);
                tx_buf[tx_len++] = (byte)Imp_.E_T3;
                //Максимальная нагрузка
                tx_buf[tx_len++] = (byte)(Imp_.max_Power >> 8);
                tx_buf[tx_len++] = (byte)Imp_.max_Power;
                //Резервные параметры (на будущее)
                tx_buf[tx_len++] = 0;
                tx_buf[tx_len++] = 0;
                tx_buf[tx_len++] = 0;
                tx_buf[tx_len++] = 0;
            }
            //Отправляем запрос
            mainForm.Log_Add_Line("Отправка запроса - Запись параметров IMP" + imp_num, Msg_Type.Normal);
            return Request_Start(link, Command.Write_IMP, 5000);
        }
        //Обработка ответа
        void CMD_Write_Imp_Params(byte[] bytes_buff, int count)
        {
            if (bytes_buff[6] == 'O' && bytes_buff[7] == 'K') mainForm.Log_Add_Line("Параметры успешно записаны IMP", Msg_Type.Good);
            if (bytes_buff[6] == 'e' && bytes_buff[7] == 'r') mainForm.Log_Add_Line("Ошибка при записи IMP", Msg_Type.Error);
        }
        #endregion

        #region ЧТЕНИЕ ЖУРНАЛА СОБЫТИЙ
        //Запрос ЧТЕНИЕ ЖУРНАЛА СОБЫТИЙ
        bool CMD_Read_Journal(ILink link, Journal_type journal)
        {
            int len = 0;
            //Первые байты по протоколу конфигурации
            tx_buf[len++] = 0;
            tx_buf[len++] = Convert.ToByte('P');
            tx_buf[len++] = Convert.ToByte('l');
            tx_buf[len++] = Convert.ToByte('s');
            tx_buf[len++] = Convert.ToByte('R');    //Уровень доступа чтение
            //Код функции чтения журнала
            tx_buf[len++] = Convert.ToByte('J');
            //Тип журнала
            tx_buf[len++] = Convert.ToByte(journal + 48); //Передаем номер в виде ASCII символа
            //Отправляем запрос
            mainForm.Log_Add_Line("Отправка запроса - Чтение журнала событий", Msg_Type.Normal);
            return Request_Start(link, Command.Read_Journal, 5000);
        }
        //Обработка ответа
        void CMD_Read_Journal(byte[] bytes_buff, int count)
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
            mainForm.Log_Add_Line("Журнал событий успешно прочитан", Msg_Type.Good);
        }
        #endregion

        #region ВРЕМЯ И ДАТА
        //Запрос ЧТЕНИЕ ВРЕМЕНИ И ДАТЫ
        bool CMD_Read_DateTime(ILink link)
        {
            int len = 0;
            //Первые байты по протоколу конфигурации
            tx_buf[len++] = 0;
            tx_buf[len++] = Convert.ToByte('P');
            tx_buf[len++] = Convert.ToByte('l');
            tx_buf[len++] = Convert.ToByte('s');
            tx_buf[len++] = Convert.ToByte('R');    //Уровень доступа чтение
            //Код функции чтения журнала
            tx_buf[len++] = Convert.ToByte('T');
            //Отправляем запрос
            mainForm.Log_Add_Line("Отправка запроса - Чтение даты/времени", Msg_Type.Normal);
            return Request_Start(link, Command.Read_DateTime, 5000);
        }
        //Обработка ответа
        void CMD_Read_DateTime(byte[] bytes_buff, int count)
        {
            mainForm.Log_Add_Line("Время/Дата успешно прочитаны", Msg_Type.Good);
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
            }
            catch (Exception)
            {
                MessageBox.Show("Неопределенный формат даты\nПопробуйте записать время на устройство заново\nВозможны проблемы с батареей");
            }
        }

        //Запрос ЗАПИСЬ ВРЕМЕНИ И ДАТЫ
        bool CMD_Write_DateTime(ILink link)
        {
            int len = 0;
            //Первые байты по протоколу конфигурации
            tx_buf[len++] = 0;
            tx_buf[len++] = Convert.ToByte('P');
            tx_buf[len++] = Convert.ToByte('l');
            tx_buf[len++] = Convert.ToByte('s');
            tx_buf[len++] = Convert.ToByte('W');    //Уровень доступа Запись
            //Код функции чтения журнала
            tx_buf[len++] = Convert.ToByte('T');
            //Данные
            tx_buf[len++] = (byte)DateTime.Now.Second;
            tx_buf[len++] = (byte)DateTime.Now.Minute;
            tx_buf[len++] = (byte)DateTime.Now.Hour;
            tx_buf[len++] = (byte)DateTime.Now.Day;
            tx_buf[len++] = (byte)DateTime.Now.Month;
            int year_ = DateTime.Now.Year;
            while (year_ >= 100) year_ -= 100;
            tx_buf[len++] = (byte)year_;
            //Отправляем запрос
            mainForm.Log_Add_Line("Отправка запроса - Запись даты/времени (" + DateTime.Now + ")", Msg_Type.Normal);
            return Request_Start(link, Command.Write_DateTime, 5000);
        }
        //Обработка ответа
        void CMD_Write_DateTime(byte[] bytes_buff, int count)
        {
            if (bytes_buff[6] == 'O' && bytes_buff[7] == 'K') mainForm.Log_Add_Line("Дата и время успешно записаны", Msg_Type.Good);
            if (bytes_buff[6] == 'e' && bytes_buff[7] == 'r') mainForm.Log_Add_Line("Ошибка при записи даты и времени. /n Возможно недопустимый формат даты.", Msg_Type.Error);
        }
        #endregion

        #region Таблица PLC
        //Запрос ЧТЕНИЕ Таблицы PLC - Активные адреса
        bool CMD_Read_PLC_Table(Command cmd ,ILink link, byte[] adrs_massiv)
        {
            int len = 0;
            //Первые байты по протоколу конфигурации
            tx_buf[len++] = 0;
            tx_buf[len++] = Convert.ToByte('P');
            tx_buf[len++] = Convert.ToByte('l');
            tx_buf[len++] = Convert.ToByte('s');
            tx_buf[len++] = Convert.ToByte('R');    //Уровень доступа 
            //Код функции
            tx_buf[len++] = Convert.ToByte('P');
            //Данные
            byte count_ = adrs_massiv[0];
            tx_buf[len++] = count_;
            for (byte i = 0; i < count_; i++)
            {
                tx_buf[len++] = adrs_massiv[i+1];
            }
            //Отправляем запрос
            mainForm.Log_Add_Line("Отправка запроса - Чтение таблицы PLC - Активные адреса", Msg_Type.Normal);
            return Request_Start(link, cmd, 5000);
        }
        //Обработка ответа
        void CMD_Read_PLC_Table(byte[] bytes_buff, int count)
        {
            int count_adrs;
            if (bytes_buff[6] == 0)
            {
                count_adrs = bytes_buff[7]; //Число адресов в ответе
                for (int i = 1; i <= count_adrs; i++)
                {
                    mainForm.plc_table[bytes_buff[7 + i] - 1].Enable = true;
                    //mainForm.e_data_table[bytes_buff[7 + i] - 1].Enable = true;
                }
                mainForm.PLC_Table_Refresh();
                //Сообщение
                mainForm.Log_Add_Line("Найдено " + count_adrs + " активных адресов", Msg_Type.Good);
                //Отправим запрос на данные Таблицы PLC
                if(currentCmd == Command.Read_PLC_Table)
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
                        mainForm.PLC_Table_Send_Data_Request(Command.Read_PLC_Table, buff_adrs);
                        mainForm.CMD_Buffer.Add_CMD(Command.Close_Session, mainForm.link, null, 0);
                    }));
                }
                //Отправим запрос на данные Показаний
                if (currentCmd == Command.Read_E_Data)
                {
                    mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => {
                        byte[] buff_adrs = new byte[251];
                        byte count_ = 0;
                        //Выделить строки с галочками
                        foreach (DataGridRow_PLC item in mainForm.plc_table)
                        //foreach (DataGridRow_E_Data item in mainForm.e_data_table)
                        {
                            if (item.Enable) buff_adrs[++count_] = item.Adrs_PLC;
                        }
                        buff_adrs[0] = count_;
                        mainForm.E_Data_Send_Data_Request(buff_adrs);
                        mainForm.CMD_Buffer.Add_CMD(Command.Close_Session, mainForm.link, null, 0);
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
                mainForm.Log_Add_Line("Прочитано " + count_adrs + " адресов, из них " + active_adrss + " вкл. [" + active_adrss_str + "]", Msg_Type.Good);
            }
            
            //Отобразим
            mainForm.PLC_Table_Refresh();
            
        }
        //Запрос ЗАПИСЬ в Таблицу PLC
        bool CMD_Write_PLC_Table(ILink link, byte[] adrs_massiv)
        {
            int len = 0;
            //Первые байты по протоколу конфигурации
            tx_buf[len++] = 0;
            tx_buf[len++] = Convert.ToByte('P');
            tx_buf[len++] = Convert.ToByte('l');
            tx_buf[len++] = Convert.ToByte('s');
            tx_buf[len++] = Convert.ToByte('W');    //Уровень доступа Запись
            //Код функции чтения журнала
            tx_buf[len++] = Convert.ToByte('P');
            //Данные
            byte count_ = adrs_massiv[0];
            tx_buf[len++] = count_;
            
            for (byte i = 0; i < count_; i++)
            {
                byte adrs_plc = adrs_massiv[i+1];
                tx_buf[len++] = adrs_plc;
                adrs_plc--; //Адреса строк начинаются с 0
                tx_buf[len++] = (mainForm.plc_table[adrs_plc].Enable) ? (byte)1 : (byte)0;
                if(mainForm.plc_table[adrs_plc].Enable)
                {
                    //Серийный номер (4 байта)
                    tx_buf[len++] = mainForm.plc_table[adrs_plc].serial_bytes[0];
                    tx_buf[len++] = mainForm.plc_table[adrs_plc].serial_bytes[1];
                    tx_buf[len++] = mainForm.plc_table[adrs_plc].serial_bytes[2];
                    tx_buf[len++] = mainForm.plc_table[adrs_plc].serial_bytes[3];
                    //Ретрансляция (6 байт)
                    tx_buf[len++] = mainForm.plc_table[adrs_plc].N;
                    tx_buf[len++] = mainForm.plc_table[adrs_plc].S1;
                    tx_buf[len++] = mainForm.plc_table[adrs_plc].S2;
                    tx_buf[len++] = mainForm.plc_table[adrs_plc].S3;
                    tx_buf[len++] = mainForm.plc_table[adrs_plc].S4;
                    tx_buf[len++] = mainForm.plc_table[adrs_plc].S5;
                    //Тип протокола аскуэ (1 байт)
                    tx_buf[len++] = mainForm.plc_table[adrs_plc].Protocol_ASCUE;
                    //Адрес аскуэ (2 байт)
                    tx_buf[len++] = (byte)(mainForm.plc_table[adrs_plc].Adrs_ASCUE>>8);
                    tx_buf[len++] = (byte)mainForm.plc_table[adrs_plc].Adrs_ASCUE;
                    //Пароль аскуэ (6 байт)
                    tx_buf[len++] = mainForm.plc_table[adrs_plc].pass_bytes[0];
                    tx_buf[len++] = mainForm.plc_table[adrs_plc].pass_bytes[1];
                    tx_buf[len++] = mainForm.plc_table[adrs_plc].pass_bytes[2];
                    tx_buf[len++] = mainForm.plc_table[adrs_plc].pass_bytes[3];
                    tx_buf[len++] = mainForm.plc_table[adrs_plc].pass_bytes[4];
                    tx_buf[len++] = mainForm.plc_table[adrs_plc].pass_bytes[5];
                    //Версия PLC
                    tx_buf[len++] = mainForm.plc_table[adrs_plc].type;
                    //Байт ошибок
                    //Статус связи
                }
            }
            
            //Отправляем запрос
            mainForm.Log_Add_Line("Отправка запроса - Write_PLC_Table", Msg_Type.Normal);
            return Request_Start(link, Command.Write_PLC_Table, 5000);
        }
        //Обработка ответа
        void CMD_Write_PLC_Table(byte[] bytes_buff, int count)
        {
            if (bytes_buff[6] == 'O' && bytes_buff[7] == 'K') mainForm.Log_Add_Line("Строки успешно записаны в PLC таблицу", Msg_Type.Good);
            if (bytes_buff[6] == 'e' && bytes_buff[7] == 'r') mainForm.Log_Add_Line("Ошибка при записи в PLC таблицу.", Msg_Type.Error);
        }
        #endregion

        #region E показания
        //Запрос Чтение Текущих показаний
        private bool CMD_Read_E_Current(ILink link, byte adrs_dev)
        {
            int len = 0;
            //Первые байты по протоколу конфигурации
            tx_buf[len++] = 0;
            tx_buf[len++] = Convert.ToByte('P');
            tx_buf[len++] = Convert.ToByte('l');
            tx_buf[len++] = Convert.ToByte('s');
            tx_buf[len++] = Convert.ToByte('R');    //Уровень доступа 
            //Код функции
            tx_buf[len++] = Convert.ToByte('E');
            tx_buf[len++] = Convert.ToByte('c');
            //Адрес устройства
            tx_buf[len++] = adrs_dev;

            //Отправляем запрос
            mainForm.Log_Add_Line("Отправка запроса - Чтение Показаний на момент последнего опроса", Msg_Type.Normal);
            return Request_Start(link, Command.Read_E_Current, 5000);
        }
        //Запрос Чтение показаний на Начало суток
        private bool CMD_Read_E_Start_Day(ILink link, byte adrs_dev)
        {
            int len = 0;
            //Первые байты по протоколу конфигурации
            tx_buf[len++] = 0;
            tx_buf[len++] = Convert.ToByte('P');
            tx_buf[len++] = Convert.ToByte('l');
            tx_buf[len++] = Convert.ToByte('s');
            tx_buf[len++] = Convert.ToByte('R');    //Уровень доступа 
            //Код функции
            tx_buf[len++] = Convert.ToByte('E');
            tx_buf[len++] = Convert.ToByte('d');
            //Адрес устройства
            tx_buf[len++] = adrs_dev;

            //Отправляем запрос
            mainForm.Log_Add_Line("Отправка запроса - Начало суток", Msg_Type.Normal);
            return Request_Start(link, Command.Read_E_Start_Day, 5000);
        }
        //Обработка ответа
        private void CMD_Read_E(Command cmd, byte[] bytes_buff, int count)
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
            if(cmd == Command.Read_E_Current)
            {
                type_E = "Последние показания";
                mainForm.plc_table[adrs_PLC - 1].E_Current_T1 = E_T1.ToString();
                mainForm.plc_table[adrs_PLC - 1].E_Current_T2 = E_T2.ToString();
                mainForm.plc_table[adrs_PLC - 1].E_Current_T3 = E_T3.ToString();
                mainForm.plc_table[adrs_PLC - 1].e_Current_Correct = E_Correct;
            }
            if (cmd == Command.Read_E_Start_Day)
            {
                type_E = "Начало суток";
                mainForm.plc_table[adrs_PLC - 1].E_StartDay_T1 = E_T1.ToString();
                mainForm.plc_table[adrs_PLC - 1].E_StartDay_T2 = E_T2.ToString();
                mainForm.plc_table[adrs_PLC - 1].E_StartDay_T3 = E_T3.ToString();
                mainForm.plc_table[adrs_PLC - 1].e_StartDay_Correct = E_Correct;
            }

            //Сообщение
            if (E_Correct)
                mainForm.Log_Add_Line("Прочитано - " + type_E + " " + adrs_PLC + ": T1 ("+ (((float)E_T1) / 1000f).ToString() + "), T2 (" + (((float)E_T2) / 1000f).ToString() + "), T3 (" + (((float)E_T3) / 1000f).ToString() + ")", Msg_Type.Good);
            else
                mainForm.Log_Add_Line("Прочитано - " + type_E + " " + adrs_PLC + ": Н/Д", Msg_Type.Good);
            mainForm.PLC_Table_Refresh();
        }
        #endregion

        #region PLC запросы
        //Команда Отправка запроса PLC
        private bool CMD_Request_PLC(ILink link, byte[] param)
        {
            int len = 0;
            //Первые байты по протоколу конфигурации
            tx_buf[len++] = 0;
            tx_buf[len++] = Convert.ToByte('P');
            tx_buf[len++] = Convert.ToByte('l');
            tx_buf[len++] = Convert.ToByte('s');
            tx_buf[len++] = Convert.ToByte('S');    //Уровень доступа 
            //Код функции
            tx_buf[len++] = Convert.ToByte('R');
            //Тип запроса по PLC
            tx_buf[len++] = param[0];
            //Адрес устройства
            tx_buf[len++] = param[1];
            //Ретрансляция
            tx_buf[len++] = param[2]; //Количество ступеней
            //Адреса ступеней
            tx_buf[len++] = param[3];   //
            tx_buf[len++] = param[4];
            tx_buf[len++] = param[5];
            tx_buf[len++] = param[6];
            tx_buf[len++] = param[7];
            //Отправляем запрос
            if(param[2] == 0)
                mainForm.Log_Add_Line("Отправка прямого запроса по PLC на " + param[0], Msg_Type.Normal);
            else
            {
                string steps_ = "";
                for (int i = 0; i < param[2]; i++) { steps_ += ", " + param[i+3]; }
                mainForm.Log_Add_Line("Отправка запроса по PLC на " + param[1] + " через " + steps_, Msg_Type.Normal);
            }

            return Request_Start(link, Command.Request_PLC, 5000);
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
                    mainForm.Log_Add_Line("Прочитано №" + adrs_PLC + ", Тип: " + plc_v + ", Тариф 1: " + (((double)E_) / 1000f).ToString() + " кВт", Msg_Type.Good);
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
                    //!! добавить ошибки ДОДЕЛАТЬ
                    if (errors_string == "Ошибки: ") errors_string = "Нет ошибок";
                    mainForm.Log_Add_Line("Прочитано №" + adrs_PLC + " " + errors_string, Msg_Type.Good);
                }
                if (plc_cmd_code == 2)
                {
                    string serial_string = bytes_buff[10].ToString("00")+ bytes_buff[11].ToString("00")+ bytes_buff[12].ToString("00")+ bytes_buff[13].ToString("00");
                    mainForm.Log_Add_Line("Прочитано №" + adrs_PLC + " Серийный номер: " + serial_string, Msg_Type.Good);
                }
                if (plc_cmd_code == 3 || plc_cmd_code == 4)
                {
                    double E_1 = (double)bytes_buff[13] * 256 * 256 * 256 + (double)bytes_buff[12] * 256 * 256 + (double)bytes_buff[11] * 256 + (double)bytes_buff[10];
                    double E_2 = (double)bytes_buff[17] * 256 * 256 * 256 + (double)bytes_buff[16] * 256 * 256 + (double)bytes_buff[15] * 256 + (double)bytes_buff[14];
                    double E_3 = (double)bytes_buff[21] * 256 * 256 * 256 + (double)bytes_buff[20] * 256 * 256 + (double)bytes_buff[19] * 256 + (double)bytes_buff[18];
                    mainForm.Log_Add_Line("Прочитано №" + adrs_PLC + 
                        ", Тариф 1: " + (((float)E_1) / 1000f).ToString() + " кВт" + 
                        ", Тариф 2: " + (((float)E_2) / 1000f).ToString() + " кВт" +
                        ", Тариф 3: " + (((float)E_3) / 1000f).ToString() + " кВт", Msg_Type.Good);
                }
            }
            else mainForm.Log_Add_Line("Устройство №" + adrs_PLC + " не отвечает", Msg_Type.Warning);
            mainForm.PLC_Table_Refresh();
        }
        #endregion
    }
}
