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
        //************************************************************************************************************************ - Основная панель с кнопками
        //Основная панель с кнопками
        //_______________________________________
        //
        private void button_Read_All_Config_Click(object sender, RoutedEventArgs e)
        {
            CMD_Buffer.Add_CMD(Command_type.Check_Pass, link, null, 0);
            CMD_Buffer.Add_CMD(Command_type.Read_DateTime, link, null, 0);
            CMD_Buffer.Add_CMD(Command_type.Read_Main_Params, link, null, 0);
            CMD_Buffer.Add_CMD(Command_type.Read_IMP, link, IMP_type.IMP1, 0);
            CMD_Buffer.Add_CMD(Command_type.Read_IMP, link, IMP_type.IMP2, 0);
            CMD_Buffer.Add_CMD(Command_type.Close_Session, link, null, 0);
        }

        private void button_Write_All_Config_Click(object sender, RoutedEventArgs e)
        {
            CMD_Buffer.Add_CMD(Command_type.Check_Pass, link, null, 0);
            CMD_Buffer.Add_CMD(Command_type.Write_DateTime, link, null, 0);
            CMD_Buffer.Add_CMD(Command_type.Write_Main_Params, link, null, 0);
            CMD_Buffer.Add_CMD(Command_type.Write_IMP, link, IMP_type.IMP1, 0);
            CMD_Buffer.Add_CMD(Command_type.Write_IMP, link, IMP_type.IMP2, 0);
            CMD_Buffer.Add_CMD(Command_type.Close_Session, link, null, 0);
        }
    }
}
