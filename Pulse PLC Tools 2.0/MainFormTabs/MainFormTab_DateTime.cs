using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Pulse_PLC_Tools_2._0
{
    public partial class MainWindow : Window
    {
        // 
        //Вкладка "Дата/Время" обработка событий контролов
        //_____________________________________________________

        //Кнопка "Прочитать Дату и Время"
        private void button_Read_DateTime_Click(object sender, RoutedEventArgs e)
        {
            CMD_Buffer.Add_CMD(Command.Check_Pass, link, null, 0);
            CMD_Buffer.Add_CMD(Command.Read_DateTime, link, null, 0);
            CMD_Buffer.Add_CMD(Command.Close_Session, link, null, 0);
        }

        //Кнопка "Записать Дату и Время"
        private void button_Write_DateTime_Click(object sender, RoutedEventArgs e)
        {
            CMD_Buffer.Add_CMD(Command.Check_Pass, link, null, 0);
            CMD_Buffer.Add_CMD(Command.Write_DateTime, link, null, 0);
            CMD_Buffer.Add_CMD(Command.Close_Session, link, null, 0);
        }

        //Кнопка "Синхронизировать"
        private void button_Correction_DateTime_Click(object sender, RoutedEventArgs e)
        {
            //ДОДЕЛАТЬ
        }
        
    }
}
