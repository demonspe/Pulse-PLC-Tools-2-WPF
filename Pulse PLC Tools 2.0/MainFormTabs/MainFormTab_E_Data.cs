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
        private void button_E_Data_Read_All(object sender, RoutedEventArgs e)
        {
            PLC_Table_Refresh();
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Check_Pass, null, 0);
            dataGrid_E_Data.SelectAll();
            E_Data_Send_Data_Request(E_Data_Get_Selected_Items());
        }

        private void button_E_Data_Read_Enable(object sender, RoutedEventArgs e)
        {
            PLC_Table_Refresh();
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Check_Pass, null, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Read_E_Data, new byte[] { 0 }, 0);
        }

        //Получить массив с выбранными адресами в таблице
        byte[] E_Data_Get_Selected_Items()
        {
            int num_item = 0;
            //Массив адресов для чтения
            byte[] selected_items = new byte[dataGrid_E_Data.SelectedItems.Count + 1];
            selected_items[num_item++] = (byte)dataGrid_E_Data.SelectedItems.Count;
            foreach (var item in dataGrid_E_Data.SelectedItems)
            {   //Адрес
                selected_items[num_item++] = (byte)(dataGrid_E_Data.Items.IndexOf(item) + 1);
            }
            return selected_items;
        }

        //Отправить запросы на чтение показаний
        public void E_Data_Send_Data_Request(byte[] selected_items)
        {
            if (selected_items[0] == 0) return;
            //Делим массив на несколько или не делим
            for (int i = 0; i < selected_items[0]; i++)
            {
                CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Read_E_Current, selected_items[i + 1], 0);
                CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Read_E_Start_Day, selected_items[i + 1], 0);
            }
        }
        //Очистить таблицу с показаниями
        private void menuItem_PLC_Table_Clear_E(object sender, RoutedEventArgs e)
        {
            PLC_Table_Clear();
        }

        private void menuItem_Read_Selected_E(object sender, RoutedEventArgs e)
        {
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Check_Pass, null, 0);
            E_Data_Send_Data_Request(E_Data_Get_Selected_Items());
        }
    }
}
