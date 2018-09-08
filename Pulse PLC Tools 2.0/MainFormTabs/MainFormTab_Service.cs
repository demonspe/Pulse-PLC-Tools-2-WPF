using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
                CMD_Buffer.Add_CMD(Commands.Check_Pass, link, null, 0);
                CMD_Buffer.Add_CMD(Commands.EEPROM_Read_Byte, link, adrs_eep, 0);
                CMD_Buffer.Add_CMD(Commands.Close_Session, link, null, 0);
            }
            else MessageBox.Show("Введите корректный адрес в пределах от 0 до 65535");
        }

        //Кнопка "Записать серийный номер"
        private void button_SerialWrite_Click(object sender, RoutedEventArgs e)
        {
            string serial_string = textBox_Serial_New.Text;
            byte[] serial_bytes = new byte[4];
            if (serial_string.Length >= 8)
                for (int i = 0; i < 4; i++)
                {
                    serial_bytes[i] = Convert.ToByte(serial_string.Substring(i * 2, 2));
                }
            CMD_Buffer.Add_CMD(Commands.Check_Pass, link, null, 0);
            CMD_Buffer.Add_CMD(Commands.SerialWrite, link, serial_bytes, 0);
            CMD_Buffer.Add_CMD(Commands.Close_Session, link, null, 0);
        }

        //создаем регулярное выражение, описывающее правило ввода
        //в данном случае, это символы от 0 до 9
        Regex inputRegex = new Regex(@"^[0-9]$");
        private void textBox_Serial_New_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            //проверяем или подходит введенный символ нашему правилу
            Match match = inputRegex.Match(e.Text);
            //и проверяем или выполняется условие
            //если введенный символ не подходит нашему правилу
            if (!match.Success)
            {
                //то обработка события прекращается и ввода неправильного символа не происходит
                e.Handled = true;
            }
            if ((sender as TextBox).Text.Length >= 8) (sender as TextBox).Text = "";

        }
    }
}
