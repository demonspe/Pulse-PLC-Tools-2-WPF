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
            CMD_Buffer.Add_CMD(Commands.Check_Pass, link, null, 0);
            CMD_Buffer.Add_CMD(Commands.Read_DateTime, link, null, 0);
            CMD_Buffer.Add_CMD(Commands.Read_Main_Params, link, null, 0);
            CMD_Buffer.Add_CMD(Commands.Read_IMP, link, IMP_type.IMP1, 0);
            CMD_Buffer.Add_CMD(Commands.Read_IMP, link, IMP_type.IMP2, 0);
            CMD_Buffer.Add_CMD(Commands.Close_Session, link, null, 0);
        }
        //Кнопка "Записать все"
        private void button_Write_All_Config_Click(object sender, RoutedEventArgs e)
        {
            CMD_Buffer.Add_CMD(Commands.Check_Pass, link, null, 0);
            CMD_Buffer.Add_CMD(Commands.Write_DateTime, link, null, 0);
            CMD_Buffer.Add_CMD(Commands.Write_Main_Params, link, null, 0);
            CMD_Buffer.Add_CMD(Commands.Write_IMP, link, IMP_type.IMP1, 0);
            CMD_Buffer.Add_CMD(Commands.Write_IMP, link, IMP_type.IMP2, 0);
            CMD_Buffer.Add_CMD(Commands.Close_Session, link, null, 0);
        }

        //Кнопка "Поиск устройств"
        private void button_Search_Devices_Click(object sender, RoutedEventArgs e)
        {
            CMD_Buffer.Add_CMD(Commands.Search_Devices, link, null, 0);
            CMD_Buffer.Add_CMD(Commands.Close_Session, link, null, 0);
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
