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
        //**********************************************
        //Вкладка "Настройки-> Основные" обработка событий контролов
        //___________________________________________
        //
        //Прочитать основные параметры
        private void button_Main_Params_Read_Click(object sender, RoutedEventArgs e)
        {
            CMD_Buffer.Add_CMD(Command_type.Check_Pass, link, null, 0);
            CMD_Buffer.Add_CMD(Command_type.Read_Main_Params, link, null, 0);
            CMD_Buffer.Add_CMD(Command_type.Close_Session, link, null, 0);
        }
        //Записать основные параметры
        private void button_Main_Params_Write_Click(object sender, RoutedEventArgs e)
        {
            if (comboBox_work_mode.SelectedIndex == -1) { MessageBox.Show("Не выбран режим работы"); return; }
            if (comboBox_battery_mode.SelectedIndex == -1) { MessageBox.Show("Не выбран режим работы"); return; }
            CMD_Buffer.Add_CMD(Command_type.Check_Pass, link, null, 0);
            CMD_Buffer.Add_CMD(Command_type.Write_Main_Params, link, null, 0);
            CMD_Buffer.Add_CMD(Command_type.Close_Session, link, null, 0);
        }
        //Записать Пароли
        private void button_Pass_Write_Click(object sender, RoutedEventArgs e)
        {
            string p_w = textBox_Write_Pass.Text, p_w_hex = "\nhex: ";
            for (int i = 0; i < 6; i++) { if (i < p_w.Length) p_w_hex += "0x" + Convert.ToByte(p_w[i]).ToString("X") + " "; else p_w_hex += "0xFF "; }
            p_w += " (" + p_w.Length + " симв)";
            if (!(bool)checkBox_Write_Pass.IsChecked) { p_w = "Без изменений"; p_w_hex = ""; }
            string p_r = textBox_Read_Pass.Text, p_r_hex = "\nhex: ";
            for (int i = 0; i < 6; i++) { if (i < p_r.Length) p_r_hex += "0x" + Convert.ToByte(p_r[i]).ToString("X") + " "; else p_r_hex += "0xFF "; }
            p_r += " (" + p_r.Length + " симв)";
            if (!(bool)checkBox_Read_Pass.IsChecked) { p_r = "Без изменений"; p_r_hex = ""; }
            if (MessageBox.Show("Записать новые пароли?\n\nПароль на запись: " + p_w + "" + p_w_hex +
                "\n\nПароль на чтение: " + p_r + p_r_hex, "Запись новых паролей", (MessageBoxButton)System.Windows.Forms.MessageBoxButtons.YesNo) == MessageBoxResult.Yes)
            {
                CMD_Buffer.Add_CMD(Command_type.Check_Pass, link, null, 0);
                CMD_Buffer.Add_CMD(Command_type.Pass_Write, link, new bool[] { (bool)checkBox_Write_Pass.IsChecked, (bool)checkBox_Read_Pass.IsChecked }, 0);
                CMD_Buffer.Add_CMD(Command_type.Close_Session, link, null, 0);
            }
        }
        //Кнопка "Перезагрузить"
        private void button_Reboot_Click(object sender, RoutedEventArgs e)
        {
            CMD_Buffer.Add_CMD(Command_type.Check_Pass, link, null, 0);
            CMD_Buffer.Add_CMD(Command_type.Reboot, link, null, 0);
            CMD_Buffer.Add_CMD(Command_type.Close_Session, link, null, 0);
        }
        //Кнопка "Очистить память"
        private void button_EEPROM_BURN_Click(object sender, RoutedEventArgs e)
        {
            CMD_Buffer.Add_CMD(Command_type.Check_Pass, link, null, 0);
            CMD_Buffer.Add_CMD(Command_type.EEPROM_Burn, link, null, 0);
            CMD_Buffer.Add_CMD(Command_type.Close_Session, link, null, 0);
        }
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
        //Кнопка "Очистить ошибки"
        private void button_Clear_Errors_Click(object sender, RoutedEventArgs e)
        {
            CMD_Buffer.Add_CMD(Command_type.Check_Pass, link, null, 0);
            CMD_Buffer.Add_CMD(Command_type.Clear_Errors, link, null, 0);
            CMD_Buffer.Add_CMD(Command_type.Close_Session, link, null, 0);
        }
    }
}
