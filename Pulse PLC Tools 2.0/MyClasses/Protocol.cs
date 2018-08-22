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
    public enum PLC_Request : int { PLCv1, Time_Synchro, Serial_Num, E_Current, E_Start_Day } //Добавить начало месяца и тд
    public enum Journal_type : int { POWER = 1, CONFIG, INTERFACES, REQUESTS }
    public enum IMP_type : int { IMP1 = 1, IMP2 }
    //Комманды посылаемые на устройство
    public enum Command_type : int {
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
        Pass_Write
    }

    public class Protocol
    {
        //Буффер для передачи
        byte[] tx_buf = new byte[512];
        MainWindow mainForm;
        public Protocol(MainWindow mainForm_)
        {
            mainForm = mainForm_;
        }

        public bool Send_CMD(Command_type cmd, Link link, object param)
        {
            if (link.wait_data) return false;
            if (link.connection == Link_type.Not_connected) {
                mainForm.debug_Log_Add_Line("Нет открытого канала связи", DebugLog_Msg_Type.Warning);
                mainForm.Link_Tab_Select();
                return false;
            }
            try { mainForm.deviceConfig.Get_From_Form(); } catch { MessageBox.Show("Не все поля конфигурации заполнены должным образом"); return false; }

            if (cmd == Command_type.Check_Pass)     return CMD_Check_Pass(link);
            if (cmd == Command_type.Close_Session)  return CMD_Close_Session(link);
            if (cmd == Command_type.Search_Devices) return CMD_Search_Devices(link);
            //Доступ - Чтение
            if (mainForm.link.access_Type != Access_Type.Read && mainForm.link.access_Type != Access_Type.Write) {
                MessageBox.Show("Нет доступа к данным устройства. Сначала авторизуйтесь."); return false; }
            if (cmd == Command_type.Read_Journal)       return CMD_Read_Journal(link, (Journal_type)param);
            if (cmd == Command_type.Read_DateTime)      return CMD_Read_DateTime(link);
            if (cmd == Command_type.Read_Main_Params)   return CMD_Read_Main_Params(link);
            if (cmd == Command_type.Read_IMP)           return CMD_Read_Imp_Params(link, (int)param);
            if (cmd == Command_type.Read_IMP_extra)     return CMD_Read_Imp_Extra_Params(link, (int)param);
            if (cmd == Command_type.Read_PLC_Table || cmd == Command_type.Read_PLC_Table_En || cmd == Command_type.Read_E_Data) return CMD_Read_PLC_Table(cmd, link, (byte[])param);
            if (cmd == Command_type.Read_E_Current)     return CMD_Read_E_Current(link, (byte)param);
            if (cmd == Command_type.Read_E_Start_Day)   return CMD_Read_E_Start_Day(link, (byte)param);
            //Доступ - Запись
            if (mainForm.link.access_Type != Access_Type.Write) { MessageBox.Show("Нет доступа к записи параметров на устройство."); return false; }
            if (cmd == Command_type.Pass_Write)        return CMD_Pass_Write(link, (bool[])param);
            if (cmd == Command_type.EEPROM_Burn)        return CMD_EEPROM_BURN(link);
            if (cmd == Command_type.EEPROM_Read_Byte)   return CMD_EEPROM_Read_Byte(link, (UInt16)param);
            if (cmd == Command_type.Reboot)             return CMD_Reboot(link);
            if (cmd == Command_type.Clear_Errors)       return CMD_Clear_Errors(link);
            if (cmd == Command_type.Write_DateTime)     return CMD_Write_DateTime(link);
            if (cmd == Command_type.Write_Main_Params)  return CMD_Write_Main_Params(link);
            if (cmd == Command_type.Write_IMP)          return CMD_Write_Imp_Params(link, (int)param);
            if (cmd == Command_type.Write_PLC_Table)    return CMD_Write_PLC_Table(link, (byte[])param);
            if (cmd == Command_type.Request_PLC)        return CMD_Request_PLC(link, (byte[])param);
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
                    if (CMD_Name == 'p' && mainForm.link.command_ == Command_type.Check_Pass) { CMD_Check_Pass(bytes_buff, count); return 0; }
                }
                //Системные команды
                if (CMD_Type == 'S')
                {
                    if (CMD_Name == 'p' && mainForm.link.command_ == Command_type.Pass_Write) { CMD_Pass_Write(bytes_buff, count); return 0; }
                    if (CMD_Name == 'r' && mainForm.link.command_ == Command_type.Reboot) { CMD_Reboot(); return 0; }
                    if (CMD_Name == 'b' && mainForm.link.command_ == Command_type.EEPROM_Burn) { CMD_EEPROM_BURN(); return 0; }
                    if (CMD_Name == 'e' && mainForm.link.command_ == Command_type.EEPROM_Read_Byte) { CMD_EEPROM_Read_Byte(bytes_buff, count); return 0; }
                    if (CMD_Name == 'c' && mainForm.link.command_ == Command_type.Clear_Errors) { CMD_Clear_Errors(); return 0; }
                    if (CMD_Name == 'R' && mainForm.link.command_ == Command_type.Request_PLC) { CMD_Request_PLC(bytes_buff, count); return 0; }
                }
                //Команды чтения
                if (CMD_Type == 'R')
                {
                    //Поиск устройств
                    if (CMD_Name == 'L' && mainForm.link.command_ == Command_type.Search_Devices) { if (CMD_Search_Devices(bytes_buff, count)) return 0; else return 1; }
                    //Чтение журнала
                    if (CMD_Name == 'J' && mainForm.link.command_ == Command_type.Read_Journal) { CMD_Read_Journal(bytes_buff, count); return 0; }
                    //Чтение времени
                    if (CMD_Name == 'T' && mainForm.link.command_ == Command_type.Read_DateTime) { CMD_Read_DateTime(bytes_buff, count); return 0; }
                    //Чтение основных параметров
                    if (CMD_Name == 'M' && mainForm.link.command_ == Command_type.Read_Main_Params) { CMD_Read_Main_Params(bytes_buff, count); return 0; }
                    //Чтение основных параметров
                    if (CMD_Name == 'I' && mainForm.link.command_ == Command_type.Read_IMP) { CMD_Read_Imp_Params(bytes_buff, count); return 0; }
                    //Чтение основных параметров
                    if (CMD_Name == 'i' && mainForm.link.command_ == Command_type.Read_IMP_extra) { CMD_Read_Imp_Extra_Params(bytes_buff, count); return 0; }
                    //Чтение таблицы PLC - Активные адреса
                    if (CMD_Name == 'P' && (mainForm.link.command_ == Command_type.Read_PLC_Table ||
                                            mainForm.link.command_ == Command_type.Read_PLC_Table_En ||
                                            mainForm.link.command_ == Command_type.Read_E_Data)) { CMD_Read_PLC_Table(bytes_buff, count); return 0; }
                    //Чтение Показаний - Активные адреса
                    if (CMD_Name == 'E') {
                        if (bytes_buff[6] == 'c' && mainForm.link.command_ == Command_type.Read_E_Current) { CMD_Read_E(Command_type.Read_E_Current, bytes_buff, count); return 0; }
                        if (bytes_buff[6] == 'd' && mainForm.link.command_ == Command_type.Read_E_Start_Day) { CMD_Read_E(Command_type.Read_E_Start_Day, bytes_buff, count); return 0; }
                    }
                }
                //Команды записи
                if (CMD_Type == 'W')
                {
                    //Запись времени
                    if (CMD_Name == 'T' && mainForm.link.command_ == Command_type.Write_DateTime) { CMD_Write_DateTime(bytes_buff, count); return 0; }
                    //Запись основных параметров
                    if (CMD_Name == 'M' && mainForm.link.command_ == Command_type.Write_Main_Params) { CMD_Write_Main_Params(bytes_buff, count); return 0; }
                    //Запись параметров импульсных входов
                    if (CMD_Name == 'I' && mainForm.link.command_ == Command_type.Write_IMP) { CMD_Write_Imp_Params(bytes_buff, count); return 0; }
                    //Запись таблицы PLC
                    if (CMD_Name == 'P' && mainForm.link.command_ == Command_type.Write_PLC_Table) { CMD_Write_PLC_Table(bytes_buff, count); return 0; }
                }
            }
            return 2;
            
            //return
            //0 - обработка успешна (конец)
            //1 - оработка успешна (ожидаем следующее сообщение)
            //2 - неверный формат (нет такого кода команды)
        }

        
        //************************************************************************************ - ACCESS (доступ к данным устройства)
        //Запрос ПРОВЕРКА ПАРОЛЯ
        bool CMD_Check_Pass(Link link)
        {
            int len = 0;
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
            mainForm.debug_Log_Add_Line("Авторизация", DebugLog_Msg_Type.Normal);
            return link.Send_Data(tx_buf, len, Command_type.Check_Pass);
        }
        //Обработка запроса
        void CMD_Check_Pass(byte[] bytes_buff, int count)
        {
            string access = "_";
            string service_mode = bytes_buff[6] == 1 ? "[Sercvice mode]" : "";
            if (bytes_buff[7] == 's') { access = "Нет доступа "; mainForm.link.Set_Access_Type(Access_Type.Write); }
            if (bytes_buff[7] == 'r') { access = "Чтение "; mainForm.link.Set_Access_Type(Access_Type.Read); }
            if (bytes_buff[7] == 'w') { access = "Запись "; mainForm.link.Set_Access_Type(Access_Type.Write); }
                    mainForm.debug_Log_Add_Line("Доступ открыт: " + access + service_mode, DebugLog_Msg_Type.Good);
        }
        //Запрос ЗАКРЫТИЕ СЕССИИ (закрывает доступ к данным)
        bool CMD_Close_Session(Link link)
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
            link.access_time_ms = 0;
            mainForm.debug_Log_Add_Line("Команда - Закрыть сессию", DebugLog_Msg_Type.Normal);
            return link.Send_Data(tx_buf, len, Command_type.Close_Session);
        }
        //************************************************************************************ - EEPROM BURN (сброс к заводским настройкам)
        //Запрос EEPROM BURN
        bool CMD_EEPROM_BURN(Link link)
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
            mainForm.debug_Log_Add_Line("Отправка запроса - Инициализация памяти", DebugLog_Msg_Type.Normal);
            return link.Send_Data(tx_buf, len, Command_type.EEPROM_Burn);
        }
        //Обработка запроса
        void CMD_EEPROM_BURN()
        {
            mainForm.debug_Log_Add_Line("Ок. После перезагрузки устройство вернется к заводским настройкам.", DebugLog_Msg_Type.Good);
        }
        //************************************************************************************ - EEPROM BURN (сброс к заводским настройкам)
        //Запрос EEPROM BURN
        bool CMD_Pass_Write(Link link, bool[] params_)
        {
            int len = 0;
            //Первые байты по протоколу конфигурации
            tx_buf[len++] = 0;
            tx_buf[len++] = Convert.ToByte('P');
            tx_buf[len++] = Convert.ToByte('l');
            tx_buf[len++] = Convert.ToByte('s');
            tx_buf[len++] = Convert.ToByte('S');    //Уровень доступа чтение
            //Код функции Write_Pass
            tx_buf[len++] = Convert.ToByte('p');
            //Пароль на запись
            tx_buf[len++] = params_[0]? (byte)1 : (byte)0; //Флаг записи
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
            mainForm.debug_Log_Add_Line("Отправка запроса - Запись новых паролей", DebugLog_Msg_Type.Normal);
            return link.Send_Data(tx_buf, len, Command_type.Pass_Write);
        }
        //Обработка запроса
        void CMD_Pass_Write(byte[] bytes_buff, int count)
        {
            if (bytes_buff[6] == 'O' && bytes_buff[7] == 'K') mainForm.debug_Log_Add_Line("Пароли успешно записаны", DebugLog_Msg_Type.Good);
            //Отобразим
            mainForm.deviceConfig.Show_On_Form();

        }
        //************************************************************************************ - EEPROM Read Byte (чтение байта из памяти)
        //Запрос EEPROM READ BYTE
        bool CMD_EEPROM_Read_Byte(Link link, UInt16 eep_adrs)
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
            mainForm.debug_Log_Add_Line("Отправка запроса - Чтение байта из памяти", DebugLog_Msg_Type.Normal);
            return link.Send_Data(tx_buf, len, Command_type.EEPROM_Read_Byte);
        }
        //Обработка запроса
        void CMD_EEPROM_Read_Byte(byte[] bytes_buff, int count)
        {
            byte data = bytes_buff[6];
            mainForm.debug_Log_Add_Line("Байт прочитан int: " + data + ", ASCII: '" + Convert.ToChar(data)+"'", DebugLog_Msg_Type.Good);
        }
        //************************************************************************************ - ПЕРЕЗАГРУЗКА
        //Запрос ПЕРЕЗАГРУЗКА
        bool CMD_Reboot(Link link)
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
            mainForm.debug_Log_Add_Line("Отправка запроса - Перезагрузка.", DebugLog_Msg_Type.Normal);
            return link.Send_Data(tx_buf, len, Command_type.Reboot);
        }
        //Обработка запроса
        void CMD_Reboot()
        {
            mainForm.debug_Log_Add_Line("Устройство перезагружается..", DebugLog_Msg_Type.Good);
        }

        //************************************************************************************ - ОЧИСТИТЬ ФЛАГИ ОШИБОК
        //Запрос ПЕРЕЗАГРУЗКА
        bool CMD_Clear_Errors(Link link)
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
            mainForm.debug_Log_Add_Line("Отправка запроса - Очистка флагов ошибок.", DebugLog_Msg_Type.Normal);
            return link.Send_Data(tx_buf, len, Command_type.Clear_Errors);
        }
        //Обработка запроса
        void CMD_Clear_Errors()
        {
            mainForm.debug_Log_Add_Line("Флаги ошибок сброшены", DebugLog_Msg_Type.Good);
        }

        //************************************************************************************ - ЧТЕНИЕ СЕРИЙНОГО НОМЕРА - ТЕСТ СВЯЗИ
        //Запрос ЧТЕНИЕ СЕРИЙНОГО НОМЕРА (и режима работы)
        bool CMD_Search_Devices(Link link)
        {
            int len = 0;
            //Первые байты по протоколу конфигурации
            tx_buf[len++] = 0;
            tx_buf[len++] = Convert.ToByte('P');
            tx_buf[len++] = Convert.ToByte('l');
            tx_buf[len++] = Convert.ToByte('s');
            tx_buf[len++] = Convert.ToByte('R');    //Уровень доступа чтение
            //Код функции ТЕСТ СВЯЗИ
            tx_buf[len++] = Convert.ToByte('L');
            //Отправляем запрос
            mainForm.debug_Log_Add_Line("Отправка запроса - Тест связи", DebugLog_Msg_Type.Normal);
            //Очистим контрол
            mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { mainForm.comboBox_Serial.Items.Clear(); }));
            return link.Send_Data(tx_buf, len, Command_type.Search_Devices);
            
        }
        //Обработка ответа
        bool CMD_Search_Devices(byte[] bytes_buff, int count)
        {
            int mode = bytes_buff[6];
            string serial_num = bytes_buff[7].ToString() + bytes_buff[8].ToString() + bytes_buff[9].ToString() + bytes_buff[10].ToString();
            mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => {
                //mainForm.comboBox_Serial.Items.Clear();
                string mode_ = "";
                if (mode == 0) mode_ = " [Счетчик]";
                if (mode == 1) mode_ = " [Фаза А]";
                if (mode == 2) mode_ = " [Фаза B]";
                if (mode == 3) mode_ = " [Фаза C]";
                mainForm.comboBox_Serial.Items.Add(serial_num + mode_);
                mainForm.comboBox_Serial.SelectedIndex = 0;
                mainForm.debug_Log_Add_Line("Найдено устройство " + serial_num + mode_, DebugLog_Msg_Type.Good);
            }));
            if(mode == 0 || mode == 3)
                return true;    //заканчиваем
            else
                return false;   //ожидаем следующее сообщение
        }
        
        //************************************************************************************ - ОСНОВНЫЕ ПАРАМЕТРЫ (режимы работы)
        //Запрос ЧТЕНИЕ ОСНОВНЫХ ПАРАМЕТРОВ 
        bool CMD_Read_Main_Params(Link link)
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
            mainForm.debug_Log_Add_Line("Отправка запроса - Чтение основных параметров устройства", DebugLog_Msg_Type.Normal);
            return link.Send_Data(tx_buf, len, Command_type.Read_Main_Params);
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
            mainForm.debug_Log_Add_Line("Основные параметры успешно прочитаны", DebugLog_Msg_Type.Good);
        }
        //Запрос ЗАПИСЬ ОСНОВНЫХ ПАРАМЕТРОВ 
        bool CMD_Write_Main_Params(Link link)
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
            mainForm.debug_Log_Add_Line("Отправка запроса - Запись основных параметров устройства", DebugLog_Msg_Type.Normal);
            return link.Send_Data(tx_buf, len, Command_type.Write_Main_Params);
        }
        //Обработка ответа
        void CMD_Write_Main_Params(byte[] bytes_buff, int count)
        {
            if (bytes_buff[6] == 'O' && bytes_buff[7] == 'K') mainForm.debug_Log_Add_Line("Основные параметры успешно записаны", DebugLog_Msg_Type.Good);
            if (bytes_buff[6] == 'e' && bytes_buff[7] == 'r') mainForm.debug_Log_Add_Line("Ошибка при записи.", DebugLog_Msg_Type.Error);
        }

        //************************************************************************************- ПАРАМЕТРЫ ИМПУЛЬСНЫХ ВХОДОВ
        //Запрос ЧТЕНИЕ ПАРАМЕТРОВ ИМПУЛЬСНЫХ ВХОДОВ
        bool CMD_Read_Imp_Params(Link link, int imp_num)
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
            mainForm.debug_Log_Add_Line("Отправка запроса - Чтение настроек IMP"+ imp_num, DebugLog_Msg_Type.Normal);
            return link.Send_Data(tx_buf, len, Command_type.Read_IMP);
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
                    //Показания (12)
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
                mainForm.debug_Log_Add_Line("Параметры имп. входа " + Convert.ToChar(bytes_buff[6]) + " успешно прочитаны", DebugLog_Msg_Type.Good);
            }
        //Запрос ЧТЕНИЕ ПАРАМЕТРОВ ИМПУЛЬСНЫХ ВХОДОВ
        bool CMD_Read_Imp_Extra_Params(Link link, int imp_num)
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
            mainForm.debug_Log_Add_Line("Отправка запроса - Чтение параметров состояния IMP" + imp_num, DebugLog_Msg_Type.Normal);
            return link.Send_Data(tx_buf, len, Command_type.Read_IMP_extra);
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
            mainForm.debug_Log_Add_Line("Мгновенные значения считаны", DebugLog_Msg_Type.Good);
        }
        //Запрос ЗАПИСЬ ПАРАМЕТРОВ ИМПУЛЬСНЫХ ВХОДОВ
        bool CMD_Write_Imp_Params(Link link, int imp_num)
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
            mainForm.debug_Log_Add_Line("Отправка запроса - Запись параметров IMP" + imp_num, DebugLog_Msg_Type.Normal);
            return link.Send_Data(tx_buf, tx_len, Command_type.Write_IMP);
        }
        //Обработка ответа
        void CMD_Write_Imp_Params(byte[] bytes_buff, int count)
        {
            if (bytes_buff[6] == 'O' && bytes_buff[7] == 'K') mainForm.debug_Log_Add_Line("Параметры успешно записаны IMP", DebugLog_Msg_Type.Good);
            if (bytes_buff[6] == 'e' && bytes_buff[7] == 'r') mainForm.debug_Log_Add_Line("Ошибка при записи IMP", DebugLog_Msg_Type.Error);
        }

        //************************************************************************************ - ЧТЕНИЕ ЖУРНАЛА СОБЫТИЙ
        //Запрос ЧТЕНИЕ ЖУРНАЛА СОБЫТИЙ
        bool CMD_Read_Journal(Link link, Journal_type journal)
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
            mainForm.debug_Log_Add_Line("Отправка запроса - Чтение журнала событий", DebugLog_Msg_Type.Normal);
            return link.Send_Data(tx_buf, len, Command_type.Read_Journal);
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
            mainForm.debug_Log_Add_Line("Журнал событий успешно прочитан", DebugLog_Msg_Type.Good);
        }

        //************************************************************************************ - ВРЕМЯ И ДАТА
        //Запрос ЧТЕНИЕ ВРЕМЕНИ И ДАТЫ
        bool CMD_Read_DateTime(Link link)
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
            mainForm.debug_Log_Add_Line("Отправка запроса - Чтение даты/времени", DebugLog_Msg_Type.Normal);
            return link.Send_Data(tx_buf, len, Command_type.Read_DateTime);
        }
        //Обработка ответа
        void CMD_Read_DateTime(byte[] bytes_buff, int count)
        {
            mainForm.debug_Log_Add_Line("Время/Дата успешно прочитаны", DebugLog_Msg_Type.Good);
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
        bool CMD_Write_DateTime(Link link)
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
            mainForm.debug_Log_Add_Line("Отправка запроса - Запись даты/времени (" + DateTime.Now + ")", DebugLog_Msg_Type.Normal);
            return link.Send_Data(tx_buf, len, Command_type.Write_DateTime);
        }
        //Обработка ответа
        void CMD_Write_DateTime(byte[] bytes_buff, int count)
        {
            if (bytes_buff[6] == 'O' && bytes_buff[7] == 'K') mainForm.debug_Log_Add_Line("Дата и время успешно записаны", DebugLog_Msg_Type.Good);
            if (bytes_buff[6] == 'e' && bytes_buff[7] == 'r') mainForm.debug_Log_Add_Line("Ошибка при записи даты и времени. /n Возможно недопустимый формат даты.", DebugLog_Msg_Type.Error);
        }

        //************************************************************************************ - Таблица PLC
        //Запрос ЧТЕНИЕ Таблицы PLC - Активные адреса
        bool CMD_Read_PLC_Table(Command_type cmd ,Link link, byte[] adrs_massiv)
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
            mainForm.debug_Log_Add_Line("Отправка запроса - Чтение таблицы PLC - Активные адреса", DebugLog_Msg_Type.Normal);
            return link.Send_Data(tx_buf, len, cmd);
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
                mainForm.debug_Log_Add_Line("Найдено " + count_adrs + " активных адресов", DebugLog_Msg_Type.Good);
                //Отправим запрос на данные Таблицы PLC
                if(mainForm.link.command_ == Command_type.Read_PLC_Table)
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
                        mainForm.PLC_Table_Send_Data_Request(Command_type.Read_PLC_Table, buff_adrs);
                        mainForm.CMD_Buffer.Add_CMD(Command_type.Close_Session, mainForm.link, null, 0);
                    }));
                }
                //Отправим запрос на данные Показаний
                if (mainForm.link.command_ == Command_type.Read_E_Data)
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
                        mainForm.CMD_Buffer.Add_CMD(Command_type.Close_Session, mainForm.link, null, 0);
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
                mainForm.debug_Log_Add_Line("Прочитано " + count_adrs + " адресов, из них " + active_adrss + " вкл. [" + active_adrss_str + "]", DebugLog_Msg_Type.Good);
            }
            
            //Отобразим
            mainForm.PLC_Table_Refresh();
            
        }
        //Запрос ЗАПИСЬ в Таблицу PLC
        bool CMD_Write_PLC_Table(Link link, byte[] adrs_massiv)
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
            mainForm.debug_Log_Add_Line("Отправка запроса - Write_PLC_Table", DebugLog_Msg_Type.Normal);
            return link.Send_Data(tx_buf, len, Command_type.Write_PLC_Table);
        }
        //Обработка ответа
        void CMD_Write_PLC_Table(byte[] bytes_buff, int count)
        {
            if (bytes_buff[6] == 'O' && bytes_buff[7] == 'K') mainForm.debug_Log_Add_Line("Строки успешно записаны в PLC таблицу", DebugLog_Msg_Type.Good);
            if (bytes_buff[6] == 'e' && bytes_buff[7] == 'r') mainForm.debug_Log_Add_Line("Ошибка при записи в PLC таблицу.", DebugLog_Msg_Type.Error);
        }

        //************************************************************************************ - E показания
        //Запрос Чтение Текущих показаний
        private bool CMD_Read_E_Current(Link link, byte adrs_dev)
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
            mainForm.debug_Log_Add_Line("Отправка запроса - Чтение Показаний на момент последнего опроса", DebugLog_Msg_Type.Normal);
            return link.Send_Data(tx_buf, len, Command_type.Read_E_Current);
        }
        //Запрос Чтение показаний на Начало суток
        private bool CMD_Read_E_Start_Day(Link link, byte adrs_dev)
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
            mainForm.debug_Log_Add_Line("Отправка запроса - Начало суток", DebugLog_Msg_Type.Normal);
            return link.Send_Data(tx_buf, len, Command_type.Read_E_Start_Day);
        }
        //Обработка ответа
        private void CMD_Read_E(Command_type cmd, byte[] bytes_buff, int count)
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
            if(cmd == Command_type.Read_E_Current)
            {
                type_E = "Последние показания";
                mainForm.plc_table[adrs_PLC - 1].E_Current_T1 = E_T1.ToString();
                mainForm.plc_table[adrs_PLC - 1].E_Current_T2 = E_T2.ToString();
                mainForm.plc_table[adrs_PLC - 1].E_Current_T3 = E_T3.ToString();
                mainForm.plc_table[adrs_PLC - 1].e_Current_Correct = E_Correct;
            }
            if (cmd == Command_type.Read_E_Start_Day)
            {
                type_E = "Начало суток";
                mainForm.plc_table[adrs_PLC - 1].E_StartDay_T1 = E_T1.ToString();
                mainForm.plc_table[adrs_PLC - 1].E_StartDay_T2 = E_T2.ToString();
                mainForm.plc_table[adrs_PLC - 1].E_StartDay_T3 = E_T3.ToString();
                mainForm.plc_table[adrs_PLC - 1].e_StartDay_Correct = E_Correct;
            }

            //Сообщение
            if (E_Correct)
                mainForm.debug_Log_Add_Line("Прочитано - " + type_E + " " + adrs_PLC + ": T1 ("+ (((float)E_T1) / 1000f).ToString() + "), T2 (" + (((float)E_T2) / 1000f).ToString() + "), T3 (" + (((float)E_T3) / 1000f).ToString() + ")", DebugLog_Msg_Type.Good);
            else
                mainForm.debug_Log_Add_Line("Прочитано - " + type_E + " " + adrs_PLC + ": Н/Д", DebugLog_Msg_Type.Good);
            mainForm.PLC_Table_Refresh();
        }

        //************************************************************************************ - PLC запросы
        //Команда Отправка запроса PLC
        private bool CMD_Request_PLC(Link link, byte[] param)
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
                mainForm.debug_Log_Add_Line("Отправка прямого запроса по PLC на " + param[0], DebugLog_Msg_Type.Normal);
            else
            {
                string steps_ = "";
                for (int i = 0; i < param[2]; i++) { steps_ += ", " + param[i+3]; }
                mainForm.debug_Log_Add_Line("Отправка запроса по PLC на " + param[1] + " через " + steps_, DebugLog_Msg_Type.Normal);
            }
                
            return link.Send_Data(tx_buf, len, Command_type.Request_PLC);
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
                    mainForm.debug_Log_Add_Line("Прочитано №" + adrs_PLC + ", Тип: " + plc_v + ", Тариф 1: " + (((double)E_) / 1000f).ToString() + " кВт", DebugLog_Msg_Type.Good);
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
                    mainForm.debug_Log_Add_Line("Прочитано №" + adrs_PLC + " " + errors_string, DebugLog_Msg_Type.Good);
                }
                if (plc_cmd_code == 2)
                {
                    string serial_string = bytes_buff[10].ToString("00")+ bytes_buff[11].ToString("00")+ bytes_buff[12].ToString("00")+ bytes_buff[13].ToString("00");
                    mainForm.debug_Log_Add_Line("Прочитано №" + adrs_PLC + " Серийный номер: " + serial_string, DebugLog_Msg_Type.Good);
                }
                if (plc_cmd_code == 3 || plc_cmd_code == 4)
                {
                    double E_1 = (double)bytes_buff[13] * 256 * 256 * 256 + (double)bytes_buff[12] * 256 * 256 + (double)bytes_buff[11] * 256 + (double)bytes_buff[10];
                    double E_2 = (double)bytes_buff[17] * 256 * 256 * 256 + (double)bytes_buff[16] * 256 * 256 + (double)bytes_buff[15] * 256 + (double)bytes_buff[14];
                    double E_3 = (double)bytes_buff[21] * 256 * 256 * 256 + (double)bytes_buff[20] * 256 * 256 + (double)bytes_buff[19] * 256 + (double)bytes_buff[18];
                    mainForm.debug_Log_Add_Line("Прочитано №" + adrs_PLC + 
                        ", Тариф 1: " + (((float)E_1) / 1000f).ToString() + " кВт" + 
                        ", Тариф 2: " + (((float)E_2) / 1000f).ToString() + " кВт" +
                        ", Тариф 3: " + (((float)E_3) / 1000f).ToString() + " кВт", DebugLog_Msg_Type.Good);
                }
            }
            else mainForm.debug_Log_Add_Line("Устройство №" + adrs_PLC + " не отвечает", DebugLog_Msg_Type.Warning);
            mainForm.PLC_Table_Refresh();
        }
    }
}
