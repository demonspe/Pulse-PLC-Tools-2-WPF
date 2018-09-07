using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace Pulse_PLC_Tools_2._0
{
    public enum Msg_Type : int { Error, Warning, Normal, Good, ToolBarInfo }

    public class StringMessageEventArgs : EventArgs
    {
        public Msg_Type MessageType { get; set; }
        public string MessageString { get; set; }
    }

    public class LinkMessageEventArgs : EventArgs
    {
        public byte[] Data { get; set; }
        public int Length { get; set; }
        public Msg_Direction Direction { get; set; }
    }

    public partial class MainWindow : Window
    {

        private void MessageInput(object sender, StringMessageEventArgs e)
        {
            if(e.MessageType == Msg_Type.ToolBarInfo)
            {
                msg(e.MessageString);
            }
            else
            {
                Log_Add_Line(e.MessageString, e.MessageType);
            }
        }
        private void LinkMessageInput(object sender, LinkMessageEventArgs e)
        {
            debug_Log_Add_Line(e.Data, e.Length, e.Direction);
        }


        //**********************************************
        //Вкладка "Анализ обмена" обработка событий контролов
        //___________________________________________
        //
        //Метод для добавления строк в текстовый блок
        public void debug_Log_Add_Line(byte[] msg, int count, Msg_Direction msg_Dir)
        {
            //Цвет зависит от направления данных
            Brush br;
            string msgIcon = "";
            string msgMain = "";

            switch (msg_Dir)
            {
                case Msg_Direction.Send:
                    br = Brushes.Orange;
                    msgIcon = "->" + link.ConnectionString;
                    break;
                case Msg_Direction.Receive:
                    br = Brushes.Blue;
                    msgIcon = "<-" + link.ConnectionString;
                    break;
                default:
                    br = Brushes.Black;
                    break;
            }

            //Если есть массив для отображения
            if (msg != null)
            {
                //Байты в HEX
                string str_msg_HEX = "HEX: ";
                for (int k_ = 0; k_ < count; k_++) { str_msg_HEX += ((byte[])msg)[k_] + " "; }
                //Байты в ASCII
                string str_msg_ASCII = "ASCII: ";
                for (int k_ = 0; k_ < count; k_++)
                {
                    //Исключаем перенос строки
                    if (((byte[])msg)[k_] == 0x0D) str_msg_ASCII += "\\r";
                    else if (((byte[])msg)[k_] == 0x0A) str_msg_ASCII += "\\n";
                    else if (((byte[])msg)[k_] < 32 || ((byte[])msg)[k_] > 126) str_msg_ASCII += ".";
                    else
                        str_msg_ASCII += Convert.ToChar(((byte[])msg)[k_]);
                }
                msgMain = str_msg_HEX + "   [" + str_msg_ASCII + "]";
            }

            //Выводим информацию
            this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => {
                //Название канала
                paragraph_log_ex.Inlines.Add(new Bold(new Run(msgIcon + " [" + DateTime.Now + "] - ") { Foreground = br }));
                //Сообщение
                paragraph_log_ex.Inlines.Add(new Run(" " + msgMain + "\n") { Foreground = br });
                textBox_Log_Debug.ScrollToEnd();
            }));
        }
        public void Log_Add_Line(string msg, Msg_Type msg_Type)
        {
            //Цвет зависит от направления данных
            Brush br;
            string msgIcon = "";
            string msgMain = "";

            switch (msg_Type)
            {
                case Msg_Type.Error:
                    br = Brushes.Red;
                    msgMain = (string)msg;
                    msgIcon = "[err]";
                    break;
                case Msg_Type.Warning:
                    br = Brushes.DarkOrange;
                    msgMain = (string)msg;
                    msgIcon = "(!)";
                    break;
                case Msg_Type.Normal:
                    br = Brushes.Black;
                    msgMain = (string)msg;
                    msgIcon = "--";
                    break;
                case Msg_Type.Good:
                    br = Brushes.Green;
                    msgMain = (string)msg;
                    msgIcon = "[OK]";
                    break;
                default:
                    br = Brushes.Black;
                    break;
            }
            //Выводим информацию
            this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => {
                //Название канала
                paragraph_log.Inlines.Add(new Bold(new Run(msgIcon + " [" + DateTime.Now + "] - ") { Foreground = br }));
                paragraph_log_ex.Inlines.Add(new Bold(new Run(msgIcon + " [" + DateTime.Now + "] - ") { Foreground = br }));
                //Сообщение
                paragraph_log.Inlines.Add(new Run(" " + msgMain + "\n") { Foreground = br });
                paragraph_log_ex.Inlines.Add(new Run(" " + msgMain + "\n") { Foreground = br });
                //Прокрутка вниз
                textBox_Log_Debug.ScrollToEnd();
                textBox_Log_Debug_ex.ScrollToEnd();
            }));
        }
        //Кнопка "Простой лог"
        private void debug_Log_Mode_Extra_Click(object sender, RoutedEventArgs e)
        {
            textBox_Log_Debug.Visibility = Visibility.Hidden;
            textBox_Log_Debug_ex.Visibility = Visibility.Visible;
        }
        //Кнопка "Расширеный лог"
        private void debug_Log_Mode_Simple_Click(object sender, RoutedEventArgs e)
        {
            textBox_Log_Debug.Visibility = Visibility.Visible;
            textBox_Log_Debug_ex.Visibility = Visibility.Hidden;
        }
        //Кнопка "Очистить"
        private void debug_Log_Clear_Click(object sender, RoutedEventArgs e)
        {
            paragraph_log.Inlines.Clear();
            paragraph_log_ex.Inlines.Clear();
        }
        //Кнопка "Прокрутить вверх"
        private void debug_Log_Up_Click(object sender, RoutedEventArgs e)
        {
            textBox_Log_Debug.ScrollToHome();
            textBox_Log_Debug_ex.ScrollToEnd();
        }
        //Кнопка "Прокрутить вниз"
        private void debug_Log_Down_Click(object sender, RoutedEventArgs e)
        {
            textBox_Log_Debug.ScrollToEnd();
            textBox_Log_Debug_ex.ScrollToEnd();
        }
    }
}
