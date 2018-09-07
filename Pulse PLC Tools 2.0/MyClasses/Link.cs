using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Net;
using System.Net.Sockets;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Pulse_PLC_Tools_2._0
{
    public enum Link_type : int { Not_connected, COM_port, TCP }
    

    public class MyLink
    {
        //Таймеры
        System.Windows.Forms.Timer timer_timeout;
        System.Windows.Forms.Timer timer_Access;

        int timeout_request_ms = 1000;  //Таймаут ответа при запросе
        public int ping_ms = 0;        //Время ожидания ответа
        public int access_time_ms = 0; //Время доступа к устройству после подтверждения пароля

        //Ссылка на форму с контролами
        MainWindow mainForm;

        //Имя канала
        public string link_name = "";
        public Access_Type access_Type = Access_Type.No_Access;
        //TCP
        TcpClient newClient;
        NetworkStream tcpStream;
        IPAddress ipAddr;
        UInt16 port_tcp;

        //COM
        public SerialPort serialPort;
        public Link_type connection;

        //Общее
        public bool wait_data = false;
        public Command command_ = Command.None;

        

        public MyLink(MainWindow mainForm_)
        {
            mainForm = mainForm_;

            timer_timeout = new System.Windows.Forms.Timer() { Enabled = true, Interval = 10 };
            timer_timeout.Tick += new System.EventHandler(this.timer_Link_Timeout_Tick);
            timer_timeout.Stop();

            timer_Access = new System.Windows.Forms.Timer() { Enabled = true, Interval = 10 };
            timer_Access.Tick += new System.EventHandler(this.timer_Link_Access_Tick);
            timer_Access.Stop();

            //Проерка соединения и отображение значком и текста на контролах
            Thread check_connection = new Thread(Check_Connection_Handler);
            check_connection.IsBackground = true;
            check_connection.Start(mainForm_);

            //Значение переменных по умолчанию
            connection = Link_type.Not_connected;
            serialPort = new SerialPort(" ", 9600, Parity.None, 8, StopBits.One);
            serialPort.ReadBufferSize = 1024;
            serialPort.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(serialPort_DataReceived);
        }

        public void Access_Timer_Start()
        {
            if (access_Type == Access_Type.No_Access) return;
            //Запустим таймер
            access_time_ms = 30000;
            timer_Access.Start();
        }

        //Установить тип доступа к устройству
        public void Set_Access_Type(Access_Type accessType)
        {
            mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => {
                access_Type = accessType;
                //Запустим таймер
                Access_Timer_Start();
            }));
        }
        //Таймер доступа
        private void timer_Link_Access_Tick(object sender, EventArgs e)
        {
            access_time_ms -= timer_Access.Interval;
            
            mainForm.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => 
            {
                string access_str = "";
                if (access_Type == Access_Type.Read) access_str = "Чтение";
                if (access_Type == Access_Type.Write) access_str = "Чтение/Запись";
                mainForm.connect_Access_timeout.Text = access_str + " (" + (access_time_ms / 1000f).ToString("#0.00") + " s)";
            }));
            if (access_time_ms <= 0)
            {
                Set_Access_Type(Access_Type.No_Access);
                timer_Access.Stop();
                mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                {
                    mainForm.connect_Access_timeout.Text = "Не авторизован";
                }));
            }
        }

        //Таймер ожидания ответа
        private void timer_Link_Timeout_Tick(object sender, EventArgs e)
        {
            if (timeout_request_ms > 0)
            {
                timeout_request_ms -= timer_timeout.Interval;
                ping_ms += timer_timeout.Interval;
            }

            mainForm.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            { mainForm.connect_status_timeout.Text = "Timeout: " + timeout_request_ms + " ms"; })); //Зависает при передвижении окна ДОДЕЛАТЬ
            if (timeout_request_ms <= 0)
            {
                mainForm.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                { mainForm.connect_status_timeout.Text = ""; }));
                //Если ожидали данных но не дождались
                if (wait_data)
                {
                    if (command_ == Command.Close_Session) { Request_Reset(true, false); return; }
                    if (command_ == Command.Search_Devices) { Request_Reset(true, false); return; }
                    mainForm.Log_Add_Line("Истекло время ожидания ответа", Msg_Type.Error);
                    mainForm.msg("Истекло время ожидания ответа");
                    //Комманда не выполнилась
                    Request_Reset(false, false);
                }
            }
        }

        //Проверка состояния соединения (фоновый поток)
        void Check_Connection_Handler(object mainForm_)
        {
            while(true)
            {
                return;
                Status_Img img_ = Status_Img.Connected;
                switch (connection)
                {
                    case Link_type.Not_connected:
                        mainForm.Set_Connection_StatusBar(Status_Img.Disconnected, "");
                        
                        mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                        {
                            mainForm.Update_COM_List();
                            mainForm.groupBox_link_type.IsEnabled = true;
                            //mainForm.grid_Access_Input.IsEnabled = false;
                            mainForm.groupBox_dateTime.IsEnabled = false;
                            mainForm.button_open_com.Content = "Открыть канал связи";
                        }));
                        
                        break;
                    case Link_type.COM_port:
                        if (access_Type == Access_Type.Read) img_ = Status_Img.Access_Read;
                        if (access_Type == Access_Type.Write) img_ = Status_Img.Access_Write;
                       // mainForm.Set_Connection_StatusBar(img_, mainForm.link.serialPort.PortName);
                        mainForm.button_open_com.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                        {
                            mainForm.groupBox_link_type.IsEnabled = false;
                            //mainForm.grid_Access_Input.IsEnabled = true;
                            mainForm.groupBox_dateTime.IsEnabled = true;
                            mainForm.button_open_com.Content = "Разорвать соединение";
                        }));
                        break;
                    case Link_type.TCP:
                        if (access_Type == Access_Type.Read) img_ = Status_Img.Access_Read;
                        if (access_Type == Access_Type.Write) img_ = Status_Img.Access_Write;
                        mainForm.Set_Connection_StatusBar(img_, "Тут должен быть IP:port");
                        mainForm.button_open_com.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                        {
                            mainForm.button_open_com.Content = "Разорвать соединение";
                        }));
                        break;
                    default:
                        break;
                }

                Thread.Sleep(500);  //Проверка каналов каждые 500 мс
            }

        }

        //Открыть соединение по COM порту
        public bool Open_connection_COM(string port)
        {
            if (connection == Link_type.TCP)
            {
                MessageBox.Show("В данный момент открыт канал связи по TCP");
                //Закрыть канал для связи по COM?
                //
                //и тд
                return false;
            }

            if(connection == Link_type.Not_connected)
            {
                if (port != "")
                {
                    serialPort.BaudRate = 9600;
                    serialPort.Parity = Parity.None;
                    serialPort.Encoding = Encoding.Default;
                    serialPort.PortName = port;
                    try
                    {
                        if (!serialPort.IsOpen) serialPort.Open();
                        connection = Link_type.COM_port;
                        //Установим значок и имя порта в статус баре
                        //mainForm.Set_Connection_StatusBar(Status_Img.Connected, mainForm.link.serialPort.PortName);
                        //Отправим сообщение в статус бар
                        mainForm.msg("Открыт последовательный порт [" + serialPort.PortName + "]");
                        mainForm.Log_Add_Line("Открыт последовательный порт [" + serialPort.PortName + "]", Msg_Type.Normal);
                        
                        return true;
                    }
                    catch
                    {
                        mainForm.Log_Add_Line("Порт занят", Msg_Type.Warning);
                    }
                }
                else
                {
                    mainForm.Log_Add_Line("Порт не выбран", Msg_Type.Warning);
                }
            }
            return false;
        }

        //Открыть соединение по TCP
        public bool Open_connection_TCP(string ip_adrs, string port)
        {
            if (connection == Link_type.COM_port)
            {
                MessageBox.Show("В данный момент открыт канал связи по COM порту");
                //Закрыть канал COM?
                //
                //и тд
                return false;
            }
            if(connection == Link_type.Not_connected)
            {
                // Устанавливаем удаленную точку для сокета
                if (IPAddress.TryParse(ip_adrs, out ipAddr) && UInt16.TryParse(port, out port_tcp))
                {
                    //valid ip
                    try
                    {
                        // Создаем новый экземпляр TcpClient
                        newClient = new TcpClient();
                        // Соединяемся с хостом
                        //msg("Попытка подключения к " + ipAddr.ToString() + "...");
                        //this.Update(); //Обновим форму для отображения текста
                        newClient.Connect(ipAddr, port_tcp);
                        // открываем поток
                        tcpStream = newClient.GetStream();
                        //msg(ipAddr.ToString() + " подключено.");
                        //Переключаем в режим обмена по TCP
                        connection = Link_type.TCP;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Не удалось подключиться к узлу. \r\n\r\n" + ex.ToString());
                    }
                }
                else
                {
                    //is not valid ip
                    MessageBox.Show("Неверный формат IP адреса или порта");
                }
            }
            return false;
        }  
        
        //Закрыть канал связи
        public bool Close_connections()
        {
            //Сбрасываем общие переменные и статусы
            Request_Reset(false, true);
            access_time_ms = 0;
            //Закрываем TCP
            if (connection == Link_type.TCP)
            {
                //timer_TCP_wait.Stop();
                //msg("Чтение завершено (TCP/IP).");
                return false;
            }

            //Закрываем COM
            if (connection == Link_type.COM_port)
            {
                //Закрываем порт
                if (serialPort.IsOpen)
                {
                    serialPort.DiscardInBuffer();  //Очистим входной буффер
                }
                try { serialPort.Close(); } catch { mainForm.Update_COM_List(); }
                connection = Link_type.Not_connected;
                //Изменим значок
                mainForm.Set_Connection_StatusBar(Status_Img.Disconnected, "");
                //Сообщение о закрытии
                mainForm.msg("Порт закрыт");
                mainForm.Log_Add_Line("Канал связи закрыт", Msg_Type.Normal);
                return true;
            }
            return false;
        }

        //Выставить флаги о том что запрос отправлен
        void Request_Start(Command cmd_, int timeout_ms)
        {
            wait_data = true;
            command_ = cmd_;
            mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => {
                mainForm.tabControl_main.IsEnabled = false;
                timer_timeout.Stop();
                timeout_request_ms = timeout_ms;
                ping_ms = 0;
                timer_timeout.Start();
            }));

            
        }

        //Сбросить флаги ожидания ответа на запрос
        public void Request_Reset(bool cmd_Is_Complete, bool clear_CMD_Buffer)
        {
            //Разблокируем все вкладки
            mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => {
                mainForm.tabControl_main.IsEnabled = true; }));
            
            timer_timeout.Stop();
            wait_data = false;              //Выключаем ожидание данных
            command_ = Command.None;   //обнулить команду

            if (connection == Link_type.TCP)
            {
                //timer_TCP_wait.Stop();
            }

            if (connection == Link_type.COM_port)
            {
                if (serialPort.IsOpen) serialPort.DiscardInBuffer();  //Очистим входной буффер
            }
            //Комманды выполнена успешно
            //mainForm.CMD_Buffer.End_Command(cmd_Is_Complete);
            if (clear_CMD_Buffer) mainForm.CMD_Buffer.Clear_Buffer();
        }

        //Отправить данные по каналу связи
        public bool Send_Data(byte[] data, int count, Command cmd)
        {
            if (connection == Link_type.Not_connected || wait_data) return false;

            //Добавим контрольную сумму
            int len = count;
            UInt16 crc_ = CRC16.ComputeChecksum(data, count);
            data[len++] = (byte)(crc_);
            data[len++] = (byte)(crc_ >> 8);

            //Добавим сообщение в Log окно
            mainForm.debug_Log_Add_Line(data, len, Msg_Direction.Send);

            //Соединений через последовательный порт
            if (connection == Link_type.COM_port)
            {
                if (serialPort.IsOpen)
                {
                    try
                    {
                        int timeout = 1000;
                        if (cmd == Command.Close_Session) timeout = 200;
                        if (cmd == Command.Write_PLC_Table) { timeout = 50*len; }
                        if (cmd == Command.Request_PLC) { timeout = (data[7]+1)*3000; }
                        Request_Start(cmd, timeout);
                        serialPort.Write(data, 0, len);
                        return true;
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Порт больше не доступен.\nВозможно был отсоединен кабель.");
                        mainForm.Link_Tab_Select();
                        Close_connections();
                    }
                }
                else
                {
                    MessageBox.Show("Порт закрыт. Возможно он занят другим процессом либо завис. Попробуйте открыть его заново");
                    mainForm.Link_Tab_Select();
                    //connection = (int)Link_type.Not_connected;
                    //Сбросим буффер команд
                    Request_Reset(false, true);
                    return false;
                }
            }

            //Соединение по TCP
            if (connection == Link_type.TCP)
            {
                //**************************

            }
            //Сбросим буффер команд
            Request_Reset(false, true);
            return false;
        }

        //Получены данные
        void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //Если не ждали ответа, то очищаем буфер приема и выходим
            if (!wait_data)
            {
                if (serialPort.IsOpen) serialPort.DiscardInBuffer();  //Очистим входной буффер
                return;
            }

            //Если ожидали ответа, то считываем буфер приема и отправляем на обработку
            byte[] bytes_buff = new byte[512];
            int i = 0;
            bool crc_ = false;
            try
            {
                do
                {
                    //Получаем байт из буффера
                    bytes_buff[i++] = (byte)serialPort.ReadByte();
                    //Посчитаем контрольную сумму сообщения
                    if (CRC16.ComputeChecksum(bytes_buff, i) == 0)
                    {
                        crc_ = true;

                        int handle_code = mainForm.protocol.Handle_Msg(bytes_buff, i);
                        //Если сообщение получится обработать то выходим
                        if (handle_code == 0)
                        {
                            mainForm.debug_Log_Add_Line(bytes_buff, i, Msg_Direction.Receive);
                            //Комманда выполнена успешно
                            Request_Reset(true, false);
                            //Обновляем таймер доступа (в устройстве он обновляется при получении команды по интерфейсу)
                            Access_Timer_Start();
                            mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                            { mainForm.connect_status_timeout.Text = "Ping: " + ping_ms + " ms"; }));
                            return;
                        }
                        //Получилось обработать и ждем следующую часть сообщения
                        if (handle_code == 1)
                        {
                            mainForm.debug_Log_Add_Line(bytes_buff, i, Msg_Direction.Receive);
                            if (serialPort.BytesToRead == 0) return;
                            ping_ms = 0;
                            i = 0;
                        }
                    }
                    else { crc_ = false; }
                    if (serialPort.BytesToRead == 0) Thread.Sleep(50);     //Время ожидания байта-------------------------!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                } while (serialPort.BytesToRead != 0);
            }
            catch (IOException)
            {
                // Code to handle the exception goes here.
            }
            //Отправим в Log окно 
            mainForm.debug_Log_Add_Line(bytes_buff, i, Msg_Direction.Receive);

            //Обработаем ошибку (если сообщение не прошло в протоколах)
            if (crc_)
            {
                //Если сошлось crc16, но не получилось обработать сообщение
                Request_Reset(false, false);
                mainForm.Log_Add_Line("Неверный формат ответа", Msg_Type.Error);
                mainForm.msg("Неверный формат ответа. Попробуйте еще раз.");
            }
            else
            {
                Request_Reset(false, false);
                //не сошлось crc16
                mainForm.Log_Add_Line("Неверная контрольная сумма", Msg_Type.Error);
                mainForm.msg("Неверная контрольная сумма. Попробуйте еще раз.");
            }
        }
    }
}
