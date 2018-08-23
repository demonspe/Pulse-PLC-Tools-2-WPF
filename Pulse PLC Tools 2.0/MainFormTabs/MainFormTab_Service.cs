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
        //Вкладка "Сервис" обработка событий контролов
        //

        //Кнопка "Прочитать байт"
        private void button_EEPROM_Read_Byte_Click(object sender, RoutedEventArgs e)
        {
            UInt16 adrs_eep;
            if (UInt16.TryParse(textBox_Adrs_EEPROM.Text, out adrs_eep))
            {
                CMD_Buffer.Add_CMD(Command_type.Check_Pass, link, null, 0);
                CMD_Buffer.Add_CMD(Command_type.EEPROM_Read_Byte, link, adrs_eep, 0);
                CMD_Buffer.Add_CMD(Command_type.Close_Session, link, null, 0);
            }
            else MessageBox.Show("Введите корректный адрес в пределах от 0 до 65535");
        }
    }
}
