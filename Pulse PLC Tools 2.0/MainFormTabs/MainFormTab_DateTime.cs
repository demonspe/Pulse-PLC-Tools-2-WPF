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
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Check_Pass, null, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Read_DateTime, null, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Close_Session, null, 0);
        }

        //Кнопка "Записать Дату и Время"
        private void button_Write_DateTime_Click(object sender, RoutedEventArgs e)
        {
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Check_Pass, null, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Write_DateTime, null, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Close_Session, null, 0);
        }

        //Кнопка "Синхронизировать"
        private void button_Correction_DateTime_Click(object sender, RoutedEventArgs e)
        {
            //ДОДЕЛАТЬ
        }
        
    }
}
