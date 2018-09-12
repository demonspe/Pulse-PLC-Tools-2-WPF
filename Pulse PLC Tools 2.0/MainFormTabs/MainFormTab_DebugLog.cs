using LinkLibrary;
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
    

    public partial class MainWindow : Window
    {
        //Обработчик события
        private void MessageInput(object sender, MessageDataEventArgs e)
        {
            if (e.MessageType == MessageType.ToolBarInfo) { msg(e.MessageString); return; }
            if (e.MessageType == MessageType.MsgBox) { MessageBox.Show(e.MessageString); return; }
            if (e.MessageType == MessageType.SendBytes || e.MessageType == MessageType.SendBytes) { Log_Add_Line_Bytes(e.Data, e.Length, e.MessageType); return; }
            Log_Add_Line_String(e.MessageString, e.MessageType);
        }
        
        //**********************************************
        //Вкладка "Анализ обмена" обработка событий контролов
        //___________________________________________
        //
        //Метод для добавления строк в текстовый блок
        public void Log_Add_Line_Bytes(byte[] msg, int count, MessageType msg_Dir)
        {
            //Цвет зависит от направления данных
            Brush br;
            string msgIcon = "";
            string msgMain = "";

            switch (msg_Dir)
            {
                case MessageType.SendBytes:
                    br = Brushes.Orange;
                    msgIcon = "->" + link.ConnectionString;
                    break;
                case MessageType.ReceiveBytes:
                    br = Brushes.Blue;
                    msgIcon = "<-" + link.ConnectionString;
                    break;
                default: return;
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
                paragraph_log_ex.Inlines.Add(new Bold(new Run("\n"+msgIcon + " [" + DateTime.Now + "] - ") { Foreground = br }));
                //Сообщение
                paragraph_log_ex.Inlines.Add(new Run(" " + msgMain) { Foreground = br });
                textBox_Log_Debug.ScrollToEnd();
            }));
        }
        public void Log_Add_Line_String(string msg, MessageType msg_Type)
        {
            //Цвет зависит от направления данных
            Brush br;
            string msgIcon = "";
            string msgMain = "";
            bool bold = false;
            switch (msg_Type)
            {
                case MessageType.Error:
                    br = Brushes.Red;
                    msgMain = (string)msg;
                    msgIcon = "[err]";
                    break;
                case MessageType.Warning:
                    br = Brushes.DarkOrange;
                    msgMain = (string)msg;
                    msgIcon = "(!)";
                    break;
                case MessageType.Normal:
                    br = Brushes.Black;
                    msgMain = (string)msg;
                    msgIcon = "--";
                    break;
                case MessageType.NormalBold:
                    br = Brushes.Black;
                    msgMain = (string)msg;
                    msgIcon = "--";
                    bold = true;
                    break;
                case MessageType.Good:
                    br = Brushes.Green;
                    msgMain = (string)msg;
                    msgIcon = "[OK]";
                    break;
                default: return;
            }
            //Выводим информацию
            this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => {
                //Название канала
                string newLine = (msgIcon == "[OK]") ? " " : "\n";
                paragraph_log.Inlines.Add(new Bold(new Run(newLine + msgIcon + " [" + DateTime.Now + "] - ") { Foreground = br }));
                paragraph_log_ex.Inlines.Add(new Bold(new Run("\n"+msgIcon + " [" + DateTime.Now + "] - ") { Foreground = br }));
                //Сообщение
                if(bold)
                {
                    paragraph_log.Inlines.Add(new Bold(new Run(" " + msgMain) { Foreground = br }));
                    paragraph_log_ex.Inlines.Add(new Bold(new Run(" " + msgMain) { Foreground = br }));
                }
                else
                {
                    paragraph_log.Inlines.Add(new Run(" " + msgMain) { Foreground = br });
                    paragraph_log_ex.Inlines.Add(new Run(" " + msgMain) { Foreground = br });
                }
                
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
