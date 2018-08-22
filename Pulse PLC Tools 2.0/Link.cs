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

    public class Link
    {
        MainWindow mainForm;

        //Имя канала
        public string link_name = "";

        //TCP
        TcpClient newClient;
        NetworkStream tcpStream;
        IPAddress ipAddr;
        UInt16 port_tcp;

        //COM
        public SerialPort serialPort;
        public Link_type connection;

        //Общее
        CRC16 crc16_o;
        bool wait_data = false;
        public Command_type command_ = Command_type.None;

        public Link(MainWindow mainForm_)
        {
            mainForm = mainForm_;

            //Проерка соединения и отображение значком и текста на контролах
            Thread check_connection = new Thread(Check_Connection_Handler);
            check_connection.IsBackground = true;
            check_connection.Start(mainForm_);

            //Значение переменных по умолчанию
            connection = Link_type.Not_connected;
            serialPort = new SerialPort(" ", 9600, Parity.None, 8, StopBits.One);
            serialPort.ReadBufferSize = 1024;
            serialPort.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(serialPort_DataReceived);
            crc16_o = new CRC16();
        }

        void Check_Connection_Handler(object mainForm_)
        {
            while(true)
            {
                switch (connection)
                {
                    case Link_type.Not_connected:
                        mainForm.Set_Connection_StatusBar(false, "");
                        mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                        {
                            mainForm.groupBox_link_type.IsEnabled = true;
                            mainForm.groupBox_access.IsEnabled = false;
                            mainForm.groupBox_dateTime.IsEnabled = false;
                            mainForm.button_open_com.Content = "Открыть канал связи";
                        }));
                        
                        break;
                    case Link_type.COM_port:
                        mainForm.Set_Connection_StatusBar(true, mainForm.link.serialPort.PortName);
                        mainForm.button_open_com.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                        {
                            mainForm.groupBox_link_type.IsEnabled = false;
                            mainForm.groupBox_access.IsEnabled = true;
                            mainForm.groupBox_dateTime.IsEnabled = true;
                            mainForm.button_open_com.Content = "Разорвать соединение";
                        }));
                        break;
                    case Link_type.TCP:
                        mainForm.Set_Connection_StatusBar(true, "Тут должен быть IP:port");
                        mainForm.button_open_com.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
                        {
                            mainForm.button_open_com.Content = "Разорвать соединение";
                        }));
                        break;
                    default:
                        break;
                }

                Thread.Sleep(500);
            }

        }

        public void Debug_Show_Array(string user_msg, byte[] bytes_buff, int count)
        {
            string str_msg_debug_HEX = "HEX: ";
            for (int k_ = 0; k_ < count; k_++) { str_msg_debug_HEX += bytes_buff[k_] + " "; }
            string str_msg_debug_ASCII = "\nASCII: ";
            for (int k_ = 0; k_ < count; k_++) { str_msg_debug_ASCII += Convert.ToChar(bytes_buff[k_]); }
            MessageBox.Show(str_msg_debug_HEX + str_msg_debug_ASCII, user_msg);
        }

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
                    if (crc16_o.ComputeChecksum(bytes_buff, i) == 0)
                    {
                        crc_ = true;
                        //Если сообщение получится обработать то выходим
                        if (mainForm.protocol.Handle_Msg(bytes_buff, i)) { mainForm.debug_Log_Add_Line(bytes_buff, i, false, this); return; }
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
            mainForm.debug_Log_Add_Line(bytes_buff, i, false, this);

            //Обработаем ошибку (если сообщение не прошло в протоколах)
            if (crc_)
            {
                //Если сошлось crc16, но не получилось обработать сообщение
                Request_Reset();
                mainForm.msg("Неверный формат ответа. Попробуйте еще раз.");
                Debug_Show_Array("crc16 ok. Неверный формат ответа.", bytes_buff, i);

            }
            else
            {
                //не сошлось crc16
                mainForm.msg("Неверный контрольная сумма. Попробуйте еще раз.");
                Debug_Show_Array("crc16 bad",bytes_buff, i);
            }
        }

        //Выставить флаги о том что запрос отправлен
        void Request_Start(Command_type cmd_)
        {
            wait_data = true;
            command_ = cmd_;
        }

        //Сбросить флаги ожидания ответа на запрос
        public void Request_Reset()
        {
            wait_data = false;              //Выключаем ожидание данных
            command_ = Command_type.None;   //обнулить команду

            if (connection == Link_type.TCP)
            {
                //timer_TCP_wait.Stop();
            }

            if (connection == Link_type.COM_port)
            {
                if (serialPort.IsOpen) serialPort.DiscardInBuffer();  //Очистим входной буффер
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
                        mainForm.Set_Connection_StatusBar(true, mainForm.link.serialPort.PortName);
                        //Отправим сообщение в статус бар
                        mainForm.msg("Открыт последовательный порт " + serialPort.PortName);
                        return true;
                    }
                    catch
                    {
                        MessageBox.Show("Порт занят");
                    }
                }
                else
                {
                    MessageBox.Show("Порт не выбран");
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
            wait_data = false;  //Выключаем ожидание данных
            command_ = Command_type.None;     //обнулить команду

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
                //Изменим значок
                mainForm.Set_Connection_StatusBar(false, "");
                mainForm.msg("Порт закрыт");
                connection = Link_type.Not_connected;
                return true;
            }

            return false;
        }

        //Отправить данные по каналу связи
        public bool Send_Data(byte[] data, int count, Command_type cmd)
        {
            if (connection == (int)Link_type.Not_connected) return false;

            //Добавим контрольную сумму
            int len = count;
            UInt16 crc_ = crc16_o.ComputeChecksum(data, count);
            data[len++] = (byte)(crc_);
            data[len++] = (byte)(crc_ >> 8);

            //Добавим сообщение в Log окно
            mainForm.debug_Log_Add_Line(data, count, true, this);

            //Соединений через последовательный порт
            if (connection == Link_type.COM_port)
            {
                if (serialPort.IsOpen)
                {
                    //Debug_Show_Array(data, len);
                    try
                    {
                        serialPort.Write(data, 0, len);
                        Request_Start(cmd);
                        return true;
                    }
                    catch (Exception)
                    {
                        MessageBox.Show("Порт больше не доступен.\nВозможно был отсоединен кабель.");
                        Close_connections();
                    }
                    
                }
                else
                {
                    MessageBox.Show("Порт закрыт. Возможно он занят другим процессом либо завис. Попробуйте открыть его заново");
                    //connection = (int)Link_type.Not_connected;
                    return false;
                }
            }

            //Соединение по TCP
            if (connection == Link_type.TCP)
            {
                //**************************

            }

            return false;
         }
    }
}
