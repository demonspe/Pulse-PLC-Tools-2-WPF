using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Pulse_PLC_Tools_2._0
{
    public partial class MainWindow : Window
    {
        //Вкладка "Конфигурация-> Маршруты PLC" обработка событий контролов
        //

        //Очистить данные в таблице PLC Table
        public void PLC_Table_Clear()
        {
            Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => {
                dataGrid_PLC_Table.ItemsSource = null;
                dataGrid_E_Data.ItemsSource = null;
                plc_table = new List<DataGridRow_PLC>();
                for (int i = 1; i < 251; i++)
                {
                    plc_table.Add(new DataGridRow_PLC() { Adrs_PLC = (byte)i });
                }
                dataGrid_E_Data.ItemsSource = plc_table;
                dataGrid_PLC_Table.ItemsSource = plc_table;

            }));
        }
        //Обновить данные после изменения PLC Table
        public void PLC_Table_Refresh()
        {
            Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => {
                dataGrid_PLC_Table.CommitEdit();
                dataGrid_PLC_Table.CommitEdit();
                dataGrid_PLC_Table.Items.Refresh();

                dataGrid_E_Data.CommitEdit();
                dataGrid_E_Data.CommitEdit();
                dataGrid_E_Data.Items.Refresh();
            }));
        }

        //Настройка отображения колонок в таблице
        private void checkBox_TablePLC_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)(checkBox_Show_PLC.IsChecked))
            {
                nColumn.Visibility = Visibility.Visible;
                s1Column.Visibility = Visibility.Visible;
                s2Column.Visibility = Visibility.Visible;
                s3Column.Visibility = Visibility.Visible;
                s4Column.Visibility = Visibility.Visible;
                s5Column.Visibility = Visibility.Visible;

                linkColumn.Visibility = Visibility.Visible;
                dateLinkColumn.Visibility = Visibility.Visible;
                qualityColumn.Visibility = Visibility.Visible;
            }
            else
            {
                nColumn.Visibility = Visibility.Hidden;
                s1Column.Visibility = Visibility.Hidden;
                s2Column.Visibility = Visibility.Hidden;
                s3Column.Visibility = Visibility.Hidden;
                s4Column.Visibility = Visibility.Hidden;
                s5Column.Visibility = Visibility.Hidden;

                linkColumn.Visibility = Visibility.Hidden;
                dateLinkColumn.Visibility = Visibility.Hidden;
                qualityColumn.Visibility = Visibility.Hidden;
            }

            if ((bool)(checkBox_Show_ASCUE.IsChecked))
            {
                protocolColumn.Visibility = Visibility.Visible;
                adrs_ascueColumn.Visibility = Visibility.Visible;
                pass_ascueColumn.Visibility = Visibility.Visible;
            }
            else
            {
                protocolColumn.Visibility = Visibility.Hidden;
                adrs_ascueColumn.Visibility = Visibility.Hidden;
                pass_ascueColumn.Visibility = Visibility.Hidden;
            }

            if ((bool)(checkBox_Show_Status.IsChecked))
            {
                versionColumn.Visibility = Visibility.Visible;
                err_byteColumn.Visibility = Visibility.Visible;
            }
            else
            {
                versionColumn.Visibility = Visibility.Hidden;
                err_byteColumn.Visibility = Visibility.Hidden;
            }

        }

        //Кнопка "Прочитать активные"
        private void Button_PLC_Table_Read_En(object sender, RoutedEventArgs e)
        {
            PLC_Table_Refresh();
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Check_Pass, null, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Read_PLC_Table, new byte[] { 0 }, 0);
        }
        //Кнопка "Прочитать все"
        private void Button_PLC_Table_Read_All(object sender, RoutedEventArgs e)
        {
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Check_Pass, null, 0);
            dataGrid_PLC_Table.SelectAll();
            PLC_Table_Send_Data_Request(PulsePLCv2Commands.Read_PLC_Table, PLC_Table_Get_Selected_Items());
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Close_Session, null, 0);
        }
        //Кнопка "Записать все"
        private void Button_PLC_Table_Write_All(object sender, RoutedEventArgs e)
        {
            PLC_Table_Refresh();
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Check_Pass, null, 0);
            dataGrid_PLC_Table.SelectAll();
            PLC_Table_Send_Data_Request(PulsePLCv2Commands.Write_PLC_Table, PLC_Table_Get_Selected_Items());
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Close_Session, null, 0);
        }

        //Контекстное меню "Запрос по PLC -> Проверка связи"
        private void menuItem_PLC_Request_PLCv1(object sender, RoutedEventArgs e)
        {
            PLC_Request_Send_Selected(PLC_Request.PLCv1);
        }
        //Контекстное меню "Запрос по PLC -> Серийный номер"
        private void menuItem_PLC_Request_Serial(object sender, RoutedEventArgs e)
        {
            PLC_Request_Send_Selected(PLC_Request.Serial_Num);
        }
        //Контекстное меню "Запрос по PLC -> Синхронизация времени"
        private void menuItem_PLC_Request_Time(object sender, RoutedEventArgs e)
        {
            PLC_Request_Send_Selected(PLC_Request.Time_Synchro);
        }
        //Контекстное меню "Запрос по PLC -> Показания - Текущие"
        private void menuItem_PLC_Request_E_Current(object sender, RoutedEventArgs e)
        {
            PLC_Request_Send_Selected(PLC_Request.E_Current);
        }
        //Контекстное меню "Запрос по PLC -> Показания - На начало суток"
        private void menuItem_PLC_Request_E_Start_Day(object sender, RoutedEventArgs e)
        {
            PLC_Request_Send_Selected(PLC_Request.E_Start_Day);
        }
        //
        private void PLC_Request_Send_Selected(PLC_Request plc_request_type)
        {
            PLC_Table_Refresh();
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Check_Pass, null, 0);
            byte[] selected_ = PLC_Table_Get_Selected_Items();
            for (int i = 0; i < selected_[0]; i++)
            {
                byte n_st = plc_table[selected_[i + 1] - 1].N;
                byte st1 = plc_table[selected_[i + 1] - 1].S1;
                byte st2 = plc_table[selected_[i + 1] - 1].S2;
                byte st3 = plc_table[selected_[i + 1] - 1].S3;
                byte st4 = plc_table[selected_[i + 1] - 1].S4;
                byte st5 = plc_table[selected_[i + 1] - 1].S5;
                CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Request_PLC, new byte[] { (byte)plc_request_type, selected_[i + 1], n_st, st1, st2, st3, st4, st5 }, 0);
                PLC_Table_Send_Data_Request(PulsePLCv2Commands.Read_PLC_Table, new byte[] { 1, selected_[i + 1] });
            }
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Close_Session, null, 0);
        }
        //Контекстное меню "Очистить таблицу"
        private void menuItem_PLC_Table_Clear(object sender, RoutedEventArgs e)
        {
            PLC_Table_Clear();
        }
        //Контекстное меню "Включить выделенные"
        private void menuItem_Set_Enable_Selected(object sender, RoutedEventArgs e)
        {
            byte[] selected_items = PLC_Table_Get_Selected_Items();
            for (int i = 0; i < selected_items[0]; i++)
            {
                plc_table[selected_items[i + 1] - 1].Enable = true;
            }
            //Отобразим
            mainForm.PLC_Table_Refresh();
        }
        //Контекстное меню "Выключить выделенные"
        private void menuItem_Set_Disable_Selected(object sender, RoutedEventArgs e)
        {
            byte[] selected_items = PLC_Table_Get_Selected_Items();
            for (int i = 0; i < selected_items[0]; i++)
            {
                plc_table[selected_items[i + 1] - 1].Enable = false;
            }
            //Отобразим
            mainForm.PLC_Table_Refresh();
        }
        //Контекстное меню "Прочитать выделенное"
        private void menuItem_Read_Selected(object sender, RoutedEventArgs e)
        {
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Check_Pass, null, 0);
            PLC_Table_Send_Data_Request(PulsePLCv2Commands.Read_PLC_Table, PLC_Table_Get_Selected_Items());
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Close_Session, null, 0);
        }
        //Контекстное меню "Записать выделенное"
        private void menuItem_Write_Selected(object sender, RoutedEventArgs e)
        {
            PLC_Table_Refresh();
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Check_Pass, null, 0);
            PLC_Table_Send_Data_Request(PulsePLCv2Commands.Write_PLC_Table, PLC_Table_Get_Selected_Items());
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Close_Session, null, 0);
        }
        //Получить массив с выбранными адресами в таблице
        byte[] PLC_Table_Get_Selected_Items()
        {
            int num_item = 0;
            //Массив адресов для чтения
            byte[] selected_items = new byte[dataGrid_PLC_Table.SelectedItems.Count + 1];
            selected_items[num_item++] = (byte)dataGrid_PLC_Table.SelectedItems.Count;
            foreach (var item in dataGrid_PLC_Table.SelectedItems)
            {   //Адрес
                selected_items[num_item++] = (byte)(dataGrid_PLC_Table.Items.IndexOf(item) + 1);
            }
            return selected_items;
        }
        //Отправить запросы на чтение/запись маршрутов PLC
        public void PLC_Table_Send_Data_Request(PulsePLCv2Commands CMD, byte[] selected_items)
        {
            if (CMD != PulsePLCv2Commands.Read_PLC_Table && CMD != PulsePLCv2Commands.Write_PLC_Table) return;

            if (selected_items[0] == 0) return;
            //Делим массив на несколько или не делим
            if (selected_items[0] < 10)
            {
                CMD_Buffer.Add_CMD(link, protocol, (int)CMD, selected_items, 0);
            }
            else
            {
                //Количество запросов по 10 адресов
                int count_cmds = (selected_items[0] - 1) / 10 + 1;
                int adrs_pntr = 1;
                byte[] request_params;
                for (int i = 0; i < count_cmds; i++)
                {
                    request_params = new byte[11];
                    request_params[0] = (i + 1 != count_cmds) ? (byte)10 : (byte)(selected_items[0] - (count_cmds - 1) * 10);
                    for (int k = 0; k < request_params[0]; k++)
                    {
                        request_params[k + 1] = selected_items[adrs_pntr++];
                    }
                    CMD_Buffer.Add_CMD(link, protocol, (int)CMD, request_params, 0);
                }
            }
        }
    }
}
