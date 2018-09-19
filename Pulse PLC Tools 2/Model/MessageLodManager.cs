using LinkLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Pulse_PLC_Tools_2
{
    public class LogManager
    {
        FlowDocument Log;
        FlowDocument LogEx;
        SynchronizationContext context;

        public LogManager(FlowDocument logSimple, FlowDocument logEx, SynchronizationContext context)
        {
            Log = logSimple;
            LogEx = logEx;
            this.context = context;

            Log.Blocks.Add(new Paragraph(new Run("Привет! Для начала открой канал связи.")));
            LogEx.Blocks.Add(new Paragraph(new Run("Привет! Для начала открой канал связи.")));
        }
        
        public void ClearLog()
        {
            Log.Blocks.Clear();
            LogEx.Blocks.Clear();
            Log.Blocks.Add(new Paragraph());
            LogEx.Blocks.Add(new Paragraph());
        }

        public void Add_Line_Bytes(byte[] msg, int count, MessageType msg_Dir, string connectionString)
        {
            //Цвет зависит от направления данных
            Brush br;
            string msgIcon = "";
            string msgMain = "";

            switch (msg_Dir)
            {
                case MessageType.SendBytes:
                    br = Brushes.Orange;
                    msgIcon = "->" + connectionString;
                    break;
                case MessageType.ReceiveBytes:
                    br = Brushes.Blue;
                    msgIcon = "<-" + connectionString;
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

            //Выводим информацию в конексте View
            context.Post((o) => {
                //Название канала
                ((Paragraph)LogEx.Blocks.FirstBlock).Inlines.Add(new Bold(new Run("\n" + msgIcon + " [" + DateTime.Now + "] - ") { Foreground = br }));
                //Сообщение
                ((Paragraph)LogEx.Blocks.FirstBlock).Inlines.Add(new Run(" " + msgMain) { Foreground = br });
            }, null);
            
            //textBox_Log_Debug.ScrollToEnd();
        }
        public void Add_Line_String(string msg, MessageType msg_Type)
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
            //Выводим информацию в конексте View
            context.Post((o) => {
                //Название канала
                string newLine = (msgIcon == "[OK]") ? " " : "\n";
                ((Paragraph)Log.Blocks.FirstBlock).Inlines.Add(new Bold(new Run(newLine + msgIcon + " [" + DateTime.Now + "] - ") { Foreground = br }));
                ((Paragraph)LogEx.Blocks.FirstBlock).Inlines.Add(new Bold(new Run("\n" + msgIcon + " [" + DateTime.Now + "] - ") { Foreground = br }));
                //Сообщение
                if (bold)
                {
                    ((Paragraph)Log.Blocks.FirstBlock).Inlines.Add(new Bold(new Run(" " + msgMain) { Foreground = br }));
                    ((Paragraph)LogEx.Blocks.FirstBlock).Inlines.Add(new Bold(new Run(" " + msgMain) { Foreground = br }));
                }
                else
                {
                    ((Paragraph)Log.Blocks.FirstBlock).Inlines.Add(new Run(" " + msgMain) { Foreground = br });
                    ((Paragraph)LogEx.Blocks.FirstBlock).Inlines.Add(new Run(" " + msgMain) { Foreground = br });
                }
            }, null);
            
            //Прокрутка вниз
            //textBox_Log_Debug.ScrollToEnd();
            //textBox_Log_Debug_ex.ScrollToEnd();
        }
    }
}
