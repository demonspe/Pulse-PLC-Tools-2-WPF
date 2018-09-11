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
        //
        // Верхняя панель с кнопками
        //_______________________________________

        //Кнопка "Прочитать все"
        private void button_Read_All_Config_Click(object sender, RoutedEventArgs e)
        {
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Check_Pass,  null, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Read_DateTime, null, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Read_Main_Params, null, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Read_IMP, IMP_type.IMP1, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Read_IMP, IMP_type.IMP2, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Close_Session, null, 0);
        }
        //Кнопка "Записать все"
        private void button_Write_All_Config_Click(object sender, RoutedEventArgs e)
        {
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Check_Pass, null, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Write_DateTime, null, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Write_Main_Params, null, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Write_IMP, IMP_type.IMP1, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Write_IMP, IMP_type.IMP2, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Close_Session, null, 0);
        }

        //Кнопка "Поиск устройств"
        private void button_Search_Devices_Click(object sender, RoutedEventArgs e)
        {
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Search_Devices, null, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Close_Session, null, 0);
        }

        //Кнопка "Сохранить файл"
        private void button_SaveFile_Click(object sender, RoutedEventArgs e)
        {
            //
        }
        
        //Кнопка "Открыть файл"
        private void button_OpenFile_Click(object sender, RoutedEventArgs e)
        {
            //
        }
    }
}
