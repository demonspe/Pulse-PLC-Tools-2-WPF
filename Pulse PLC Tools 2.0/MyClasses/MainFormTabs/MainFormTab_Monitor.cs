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
        //___________________________________________
        //
        //Вкладка "Монитор" обработка событий контролов
        //___________________________________________
        //
        private void button_Read_Imp1_Extra_Click_1(object sender, RoutedEventArgs e)
        {
            CMD_Buffer.Add_CMD(Command_type.Check_Pass, link, null, 0);
            CMD_Buffer.Add_CMD(Command_type.Read_IMP_extra, link, IMP_type.IMP1, 0);
            CMD_Buffer.Add_CMD(Command_type.Close_Session, link, IMP_type.IMP1, 0);
        }
        private void button_Read_Imp2_Extra_Click_1(object sender, RoutedEventArgs e)
        {
            CMD_Buffer.Add_CMD(Command_type.Check_Pass, link, null, 0);
            CMD_Buffer.Add_CMD(Command_type.Read_IMP_extra, link, IMP_type.IMP2, 0);
            CMD_Buffer.Add_CMD(Command_type.Close_Session, link, IMP_type.IMP2, 0);
        }
    }
}
