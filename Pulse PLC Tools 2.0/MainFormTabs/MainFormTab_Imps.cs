using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Pulse_PLC_Tools_2._0
{
    public partial class MainWindow : Window
    {
        //Кисти для покраски
        Brush br_imp_on = new SolidColorBrush(Color.FromArgb(0x7F, 0x00, 0xA2, 0xFF));
        Brush br_off = new SolidColorBrush(Color.FromArgb(0xB2, 0x80, 0x80, 0x80));
        Brush br_RS485_on = new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xB9, 0x80));

        
        //Вкладка "Конфигурация-> IMP 1" обработка событий контролов
        //

        //Выбор количества тарифов
        private void comboBox_num_of_tarifs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (comboBox_Tqty_imp1.SelectedIndex)
            {
                case 0:

                    textBox_T1_1_imp1.IsEnabled = false;
                    textBox_T3_1_imp1.IsEnabled = false;
                    textBox_T1_2_imp1.IsEnabled = false;
                    textBox_T3_2_imp1.IsEnabled = false;
                    textBox_T2_imp1.IsEnabled = false;
                    textBox_E_T2_imp1.IsEnabled = false;
                    textBox_E_T3_imp1.IsEnabled = false;
                    textBox_E_T2_imp1.Text = "0";
                    textBox_E_T3_imp1.Text = "0";
                    break;
                case 1:
                    textBox_T1_1_imp1.IsEnabled = true;
                    textBox_T3_1_imp1.IsEnabled = false;
                    textBox_T1_2_imp1.IsEnabled = false;
                    textBox_T3_2_imp1.IsEnabled = false;
                    textBox_T2_imp1.IsEnabled = true;
                    textBox_E_T2_imp1.IsEnabled = true;
                    textBox_E_T3_imp1.IsEnabled = false;
                    textBox_E_T3_imp1.Text = "0";
                    break;
                case 2:
                    textBox_T1_1_imp1.IsEnabled = true;
                    textBox_T3_1_imp1.IsEnabled = true;
                    textBox_T1_2_imp1.IsEnabled = true;
                    textBox_T3_2_imp1.IsEnabled = true;
                    textBox_T2_imp1.IsEnabled = true;
                    textBox_E_T2_imp1.IsEnabled = true;
                    textBox_E_T3_imp1.IsEnabled = true;
                    break;
            }
        }
        //Отрисовка линий
        public void imp1_draw()
        {
            groupBox_IMP1.IsEnabled = (bool)(checkBox_IMP1_On.IsChecked);
            draw_Imp1.Fill = (bool)(checkBox_IMP1_On.IsChecked) ? br_imp_on : br_off;
            draw_Imp1_double.Fill = (bool)(checkBox_IMP1_On.IsChecked) ? br_imp_on : br_off;
            draw_Imp1_line1.Fill = (bool)(checkBox_IMP1_On.IsChecked) ? br_imp_on : br_off;
            draw_Imp1_line2.Fill = (bool)(checkBox_IMP1_On.IsChecked) ? br_imp_on : br_off;
            draw_Imp1_line3.Fill = (bool)(checkBox_IMP1_On.IsChecked) ? br_RS485_on : br_off;
            draw_Imp1_line4.Fill = (bool)(checkBox_IMP1_On.IsChecked) ? br_RS485_on : br_off;
        }
        //Включить/отключить импульсный вход 1
        private void checkBox_IMP1_On_Checked(object sender, RoutedEventArgs e)
        {
            imp1_draw();
        }
        //Нажатие на рисунок
        private void draw_Imp1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            checkBox_IMP1_On.IsChecked = (bool)(checkBox_IMP1_On.IsChecked) ? false : true;
            imp1_draw();
        }
        //Кнопка "Прочитать"
        private void button_Read_Imp1_Click_1(object sender, RoutedEventArgs e)
        {
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Check_Pass, null, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Read_IMP, IMP_type.IMP1, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Close_Session, null, 0);
        }
        //Кнопка "Записать"
        private void button_Write_Imp1_Click_1(object sender, RoutedEventArgs e)
        {
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Check_Pass, null, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Write_IMP, IMP_type.IMP1, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Close_Session, null, 0);
        }

        
        //Вкладка "Конфигурация-> IMP 2" обработка событий контролов
        //

        //Выбор количества тарифов
        private void comboBox_num_of_tarifs_IMP2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (comboBox_Tqty_imp2.SelectedIndex)
            {
                case 0:
                    textBox_T1_1_imp2.IsEnabled = false;
                    textBox_T3_1_imp2.IsEnabled = false;
                    textBox_T1_2_imp2.IsEnabled = false;
                    textBox_T3_2_imp2.IsEnabled = false;
                    textBox_T2_imp2.IsEnabled = false;
                    textBox_E_T2_imp2.IsEnabled = false;
                    textBox_E_T3_imp2.IsEnabled = false;
                    textBox_E_T2_imp2.Text = "0";
                    textBox_E_T3_imp2.Text = "0";
                    break;
                case 1:
                    textBox_T1_1_imp2.IsEnabled = true;
                    textBox_T3_1_imp2.IsEnabled = false;
                    textBox_T1_2_imp2.IsEnabled = false;
                    textBox_T3_2_imp2.IsEnabled = false;
                    textBox_T2_imp2.IsEnabled = true;
                    textBox_E_T2_imp2.IsEnabled = true;
                    textBox_E_T3_imp2.IsEnabled = false;
                    textBox_E_T3_imp2.Text = "0";
                    break;
                case 2:
                    textBox_T1_1_imp2.IsEnabled = true;
                    textBox_T3_1_imp2.IsEnabled = true;
                    textBox_T1_2_imp2.IsEnabled = true;
                    textBox_T3_2_imp2.IsEnabled = true;
                    textBox_T2_imp2.IsEnabled = true;
                    textBox_E_T2_imp2.IsEnabled = true;
                    textBox_E_T3_imp2.IsEnabled = true;
                    break;
            }
        }
        //Отрисовка фигур
        public void imp2_draw()
        {
            groupBox_IMP2.IsEnabled = (bool)(checkBox_IMP2_On.IsChecked);
            draw_Imp2.Fill = (bool)(checkBox_IMP2_On.IsChecked) ? br_imp_on : br_off;
            draw_Imp2_double.Fill = (bool)(checkBox_IMP2_On.IsChecked) ? br_imp_on : br_off;
            draw_Imp2_line1.Fill = (bool)(checkBox_IMP2_On.IsChecked) ? br_imp_on : br_off;
            draw_Imp2_line2.Fill = (bool)(checkBox_IMP2_On.IsChecked) ? br_imp_on : br_off;
            draw_Imp2_line3.Fill = (bool)(checkBox_IMP2_On.IsChecked) ? br_RS485_on : br_off;
            draw_Imp2_line4.Fill = (bool)(checkBox_IMP2_On.IsChecked) ? br_RS485_on : br_off;
        }
        //Включить/отключить импульсный вход 1 (Чекбокс)
        private void checkBox_IMP2_On_Checked(object sender, RoutedEventArgs e)
        {
            imp2_draw();
        }
        //Нажатие на изображение входа
        private void draw_Imp2_MouseDown(object sender, MouseButtonEventArgs e)
        {
            checkBox_IMP2_On.IsChecked = (bool)(checkBox_IMP2_On.IsChecked) ? false : true;
            imp2_draw();
        }
        //Кнопка "Прочитать"
        private void button_Read_Imp2_Click_1(object sender, RoutedEventArgs e)
        {
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Check_Pass, null, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Read_IMP, IMP_type.IMP2, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Close_Session, null, 0);
        }
        //Кнопка "Записать"
        private void button_Write_Imp2_Click_1(object sender, RoutedEventArgs e)
        {
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Check_Pass, null, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Write_IMP, IMP_type.IMP2, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Close_Session, null, 0);
        }
    }
}
