using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Pulse_PLC_Tools_2._0
{
    public enum Journal_type { POWER, CONFIG, INTERFACES }
    //Комманды посылаемые на устройство
    public enum Command_type : int {
        None,
        Read_Serial,
        Read_Journal,
        Read_DateTime,
        Write_DateTime
    }

    public class Protocol
    {
        //Буффер для передачи
        byte[] tx_buff = new byte[512];
        MainWindow mainForm;
        public Protocol(MainWindow mainForm_)
        {
            mainForm = mainForm_;
        }

        public bool Handle_Msg(byte[] bytes_buff, int count)
        {
            if(bytes_buff[0] == 0 &&  bytes_buff[1] == 'P' && bytes_buff[2] == 'l' && bytes_buff[3] == 's' )
            {
                if(bytes_buff[4] == 'R' && mainForm.link.command_ == Command_type.Read_Serial) { CMD_Read_Serial(bytes_buff, count); goto Handle_ok;   }
                if(bytes_buff[4] == 'J' && mainForm.link.command_ == Command_type.Read_Journal) { CMD_Read_Journal(bytes_buff, count); goto Handle_ok; }
                if(bytes_buff[4] == 'T' && bytes_buff[5] == 'R' && mainForm.link.command_ == Command_type.Read_DateTime) { CMD_Read_DateTime(bytes_buff, count); goto Handle_ok; }
                if (bytes_buff[4] == 'T' && bytes_buff[5] == 'W' && mainForm.link.command_ == Command_type.Write_DateTime) { CMD_Write_DateTime(bytes_buff, count); goto Handle_ok; }
            }
            return false;

            Handle_ok:
            mainForm.link.Request_Reset();
            return true;
        }

        //************************************************************************************-ЧТЕНИЕ СЕРИЙНОГО НОМЕРА
        //Запрос ЧТЕНИЕ СЕРИЙНОГО НОМЕРА (и реима работы)
        public void CMD_Read_Serial(Link link)
        {
            int len = 0;
            //Первые байты по протоколу конфигурации
            tx_buff[len++] = 0;
            tx_buff[len++] = Convert.ToByte('P');
            tx_buff[len++] = Convert.ToByte('l');
            tx_buff[len++] = Convert.ToByte('s');
            //Код функции чтения журнала
            tx_buff[len++] = Convert.ToByte('R');
            //Отправляем запрос
            link.Send_Data(tx_buff, len, Command_type.Read_Serial);
        }
        //Обработка ответа
        void CMD_Read_Serial(byte[] bytes_buff, int count)
        {
            int mode = bytes_buff[5];
            string serial_num = bytes_buff[6].ToString() + bytes_buff[7].ToString() + bytes_buff[8].ToString() + bytes_buff[9].ToString();
            mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => {
                mainForm.comboBox_Serial.Items.Clear();
                string mode_ = "";
                if (mode == 0) mode_ = " [Счетчик]";
                if (mode == 1) mode_ = " [Фаза А]";
                if (mode == 2) mode_ = " [Фаза B]";
                if (mode == 3) mode_ = " [Фаза C]";
                mainForm.comboBox_Serial.Items.Add(serial_num + mode_);
                mainForm.comboBox_Serial.SelectedIndex = 0;
            }));
           
        }

        //************************************************************************************-ЧТЕНИЕ ЖУРНАЛА СОБЫТИЙ
        //Запрос ЧТЕНИЕ ЖУРНАЛА СОБЫТИЙ
        //
        public void CMD_Read_Journal(Link link, Journal_type journal)
        {
            int len = 0;
            //Первые байты по протоколу конфигурации
            tx_buff[len++] = 0;
            tx_buff[len++] = Convert.ToByte('P');
            tx_buff[len++] = Convert.ToByte('l');
            tx_buff[len++] = Convert.ToByte('s');
            //Код функции чтения журнала
            tx_buff[len++] = Convert.ToByte('J');
            //Тип журнала
            if (journal == Journal_type.POWER)      tx_buff[len++] = Convert.ToByte('1');
            if (journal == Journal_type.CONFIG)     tx_buff[len++] = Convert.ToByte('2');
            if (journal == Journal_type.INTERFACES) tx_buff[len++] = Convert.ToByte('3');
            //Отправляем запрос
            link.Send_Data(tx_buff, len, Command_type.Read_Journal);
        }
        //Обработка ответа
        void CMD_Read_Journal(byte[] bytes_buff, int count)
        {
            object dataGrid_journal = null;
            if (bytes_buff[5] == '1') dataGrid_journal = mainForm.dataGrid_Log_Power;
            if (bytes_buff[5] == '2') dataGrid_journal = mainForm.dataGrid_Log_Config;
            if (bytes_buff[5] == '3') dataGrid_journal = mainForm.dataGrid_Log_Interfaces;

            int events_count = bytes_buff[6];
            for(int i = 0; i < events_count; i++)
            {
                string event_name = "";
                DateTime time;
                string time_string, date_string;
                if (bytes_buff[i * 7 + 7] == 1) event_name = "Включение";
                if (bytes_buff[i * 7 + 7] == 2) event_name = "Отключение";
                if (bytes_buff[i * 7 + 7] == 3) event_name = "Перезагрузка";
                if (bytes_buff[i * 7 + 7] == 4) event_name = "Записаны дата и время";
                if (bytes_buff[i * 7 + 7] == 5) event_name = "Записан режим работы Концентратор А";
                if (bytes_buff[i * 7 + 7] == 6) event_name = "Записан режим работы Концентратор B";
                if (bytes_buff[i * 7 + 7] == 7) event_name = "Записан режим работы Концентратор C";
                if (bytes_buff[i * 7 + 7] == 8) event_name = "Запись в таблицу маршрутов";
                if (bytes_buff[i * 7 + 7] == 9) event_name = "Записан режим работы Счетчик";
                if (bytes_buff[i * 7 + 7] == 10) event_name = "Запрос по RS485";
                if (bytes_buff[i * 7 + 7] == 11) event_name = "Запрос по Bluetooth";
                if (bytes_buff[i * 7 + 7] == 12) event_name = "Запрос по USB";
                try
                {
                    time = new DateTime(bytes_buff[i * 7 + 13], bytes_buff[i * 7 + 12], bytes_buff[i * 7 + 11], bytes_buff[i * 7 + 10], bytes_buff[i * 7 + 9], bytes_buff[i * 7 + 8]);
                    date_string = time.ToString("dd.MM.yy");
                    time_string = time.ToString("HH:mm:ss");
                }
                catch (Exception)
                {
                    time_string = bytes_buff[i * 7 + 10].ToString() + ":" +
                                    bytes_buff[i * 7 + 9].ToString() + ":" +
                                    bytes_buff[i * 7 + 8].ToString();
                    date_string = bytes_buff[i * 7 + 11].ToString() + "." +
                                    bytes_buff[i * 7 + 12].ToString() + "." +
                                    bytes_buff[i * 7 + 13].ToString();
                }
                
                DataGridRow_Log row = new DataGridRow_Log {Num = (i + 1).ToString(), Date = date_string, Time = time_string, Name = event_name };
                mainForm.DataGrid_Log_Add_Row((DataGrid)dataGrid_journal, row);
            }
        }

        //************************************************************************************-ЧТЕНИЕ ВРЕМЕНИ И ДАТЫ
        //Запрос ЧТЕНИЕ ВРЕМЕНИ И ДАТЫ
        public void CMD_Read_DateTime(Link link)
        {
            int len = 0;
            //Первые байты по протоколу конфигурации
            tx_buff[len++] = 0;
            tx_buff[len++] = Convert.ToByte('P');
            tx_buff[len++] = Convert.ToByte('l');
            tx_buff[len++] = Convert.ToByte('s');
            //Код функции чтения журнала
            tx_buff[len++] = Convert.ToByte('T');
            tx_buff[len++] = Convert.ToByte('R');
            //Отправляем запрос
            link.Send_Data(tx_buff, len, Command_type.Read_DateTime);
        }
        //Обработка ответа
        public void CMD_Read_DateTime(byte[] bytes_buff, int count)
        {
            DateTime datetime_ = new DateTime((int)(DateTime.Now.Year/100)*100 + bytes_buff[11], bytes_buff[10], bytes_buff[9], bytes_buff[8], bytes_buff[7], bytes_buff[6]);
            mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { mainForm.textBox_Date_in_device.Text = datetime_.ToString("dd.MM.yy"); }));
            mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { mainForm.textBox_Time_in_device.Text = datetime_.ToString("HH:mm:ss"); }));
            System.TimeSpan diff = datetime_.Subtract(DateTime.Now);
            mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { mainForm.textBox_Time_difference.Text = diff.ToString("g"); }));
            mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { mainForm.textBox_Date_in_pc.Text = DateTime.Now.ToString("dd.MM.yy"); }));
            mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { mainForm.textBox_Time_in_pc.Text = DateTime.Now.ToString("HH:mm:ss"); }));
            try
            {
                
            }
            catch (Exception)
            {
                MessageBox.Show("Неопределенный формат даты\nПопробуйте записать время на устройство заново\nВозможны проблемы с батареей");
            }
            
        }

        //Запрос ЗАПИСЬ ВРЕМЕНИ И ДАТЫ
        public void CMD_Write_DateTime(Link link)
        {
            int len = 0;
            //Первые байты по протоколу конфигурации
            tx_buff[len++] = 0;
            tx_buff[len++] = Convert.ToByte('P');
            tx_buff[len++] = Convert.ToByte('l');
            tx_buff[len++] = Convert.ToByte('s');
            //Код функции чтения журнала
            tx_buff[len++] = Convert.ToByte('T');
            tx_buff[len++] = Convert.ToByte('W');
            //Данные
            tx_buff[len++] = (byte)DateTime.Now.Second;
            tx_buff[len++] = (byte)DateTime.Now.Minute;
            tx_buff[len++] = (byte)DateTime.Now.Hour;
            tx_buff[len++] = (byte)DateTime.Now.Day;
            tx_buff[len++] = (byte)DateTime.Now.Month;
            int year_ = DateTime.Now.Year;
            while (year_ >= 100) year_ -= 100;
            tx_buff[len++] = (byte)year_;
            //Отправляем запрос
            link.Send_Data(tx_buff, len, Command_type.Write_DateTime);
        }
        //Обработка ответа
        public void CMD_Write_DateTime(byte[] bytes_buff, int count)
        {
            if (bytes_buff[6] == 'O' && bytes_buff[7] == 'K') MessageBox.Show("Дата и время успешно записаны");
            if (bytes_buff[6] == 'e' && bytes_buff[7] == 'r') MessageBox.Show("Ошибка при записи даты и времени. /n Возможно недопустимый формат даты.");
        }
    }
}
