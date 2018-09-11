using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Pulse_PLC_Tools_2._0
{
    public partial class MainWindow : Window
    {
        //Вкладка "Журнал-> Питание" обработка событий контролов
        //
        //Кнопка "Прочитать журнал вкл/откл устройства"
        private void Button_Read_Log_Power_Click(object sender, RoutedEventArgs e)
        {
            dataGrid_Log_Power.Items.Clear();
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Check_Pass, null, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Read_Journal, Journal_type.POWER, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Close_Session, null, 0);
            //protocol.Send_CMD(Command_type.Read_Journal, link, Journal_type.POWER);
        }

        //Вкладка "Журнал-> Конфигурация" обработка событий контролов
        //
        //Кнопка "Прочитать журнал вкл/откл устройства"
        private void Button_Read_Log_Config_Click(object sender, RoutedEventArgs e)
        {
            dataGrid_Log_Config.Items.Clear();
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Check_Pass, null, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Read_Journal, Journal_type.CONFIG, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Close_Session, null, 0);
            //protocol.Send_CMD(Command_type.Read_Journal, link, Journal_type.CONFIG);
        }
        
        //Вкладка "Журнал-> Интерфейсы" обработка событий контролов
        //
        //Кнопка "Прочитать журнал вкл/откл устройства"
        private void Button_Read_Log_Interfaces_Click(object sender, RoutedEventArgs e)
        {
            dataGrid_Log_Interfaces.Items.Clear();
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Check_Pass, null, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Read_Journal, Journal_type.INTERFACES, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Close_Session, null, 0);
        }

        //Вкладка "Журнал-> Лог опроса" обработка событий контролов
        //
        //Кнопка "Прочитать журнал вкл/откл устройства"
        private void Button_Read_Log_Requests_Click(object sender, RoutedEventArgs e)
        {
            dataGrid_Log_Requests.Items.Clear();
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Check_Pass, null, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Read_Journal, Journal_type.REQUESTS, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Close_Session, null, 0);
        }
    }
}
