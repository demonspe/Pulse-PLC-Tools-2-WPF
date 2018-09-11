using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Pulse_PLC_Tools_2._0
{
    public partial class MainWindow : Window
    {
        //************************************************
        // TreeView меню для навигации по вкладкам
        //________________________________________________

        //Красим выделенный текст 
        void TreeView_Selected_Color(TreeViewItem treeView_item, Brush color)
        {
            foreach (TreeViewItem item in treeView_Menu.Items)
            {
                item.Foreground = Brushes.White;
                foreach (TreeViewItem item_ in item.Items) item_.Foreground = Brushes.White;
            }
            treeView_item.Foreground = color;
        }

        //Переход на вкладку "Связь"
        private void TreeView_Link_Selected(object sender, RoutedEventArgs e)
        {
            tab_Link.IsSelected = true;
            TreeView_Selected_Color((TreeViewItem)sender, Brushes.Black);
        }

        //Переход на вкладку "Дата/Время"
        private void TreeView_DateTime_Selected(object sender, RoutedEventArgs e)
        {
            tab_DateTime.IsSelected = true;
            TreeView_Selected_Color((TreeViewItem)sender, Brushes.Black);
        }

        //Переход на вкладку "Основные настройки"
        private void TreeView_Config_MAIN_Selected(object sender, RoutedEventArgs e)
        {
            tab_Main_Params.IsSelected = true;
            TreeView_Selected_Color((TreeViewItem)sender, Brushes.Black);
        }

        //Переход на вкладку "О программе"
        private void TreeView_About_Selected(object sender, RoutedEventArgs e)
        {
            tab_About.IsSelected = true;
            if (sender.GetType().ToString() == "System.Windows.Controls.TreeViewItem")
                TreeView_Selected_Color((TreeViewItem)sender, Brushes.Black);
        }

        //Переход на вкладку журнал "Питание"
        private void TreeView_Log_Power_Selected(object sender, RoutedEventArgs e)
        {
            tab_Log_Power.IsSelected = true;
            TreeView_Selected_Color((TreeViewItem)sender, Brushes.Black);
        }

        //Переход на вкладку журнал "Конфигурация"
        private void TreeView_Log_Config_Selected(object sender, RoutedEventArgs e)
        {
            tab_Log_Config.IsSelected = true;
            TreeView_Selected_Color((TreeViewItem)sender, Brushes.Black);
        }

        //Переход на вкладку журнал "Интерфейсы"
        private void TreeView_Log_Interfaces_Selected(object sender, RoutedEventArgs e)
        {
            tab_Log_Interfaces.IsSelected = true;
            TreeView_Selected_Color((TreeViewItem)sender, Brushes.Black);
        }

        //Переход на вкладку журнал "Запросы PLC"
        private void TreeView_Log_Requests_Selected(object sender, RoutedEventArgs e)
        {
            tab_Log_Requests.IsSelected = true;
            TreeView_Selected_Color((TreeViewItem)sender, Brushes.Black);
        }

        //Переход на вкладку конфигурация IMP1
        private void TreeView_Config_IMP1_Selected(object sender, RoutedEventArgs e)
        {
            tab_Config_IMP1.IsSelected = true;
            TreeView_Selected_Color((TreeViewItem)sender, Brushes.Black);
        }

        //Переход на вкладку конфигурация IMP1
        private void TreeView_Config_IMP2_Selected(object sender, RoutedEventArgs e)
        {
            tab_Config_IMP2.IsSelected = true;
            TreeView_Selected_Color((TreeViewItem)sender, Brushes.Black);
        }

        //Переход на вкладку "Монитор"
        private void TreeView_Config_IMPS_Monitor_Selected(object sender, RoutedEventArgs e)
        {
            tab_IMPS_Monitor.IsSelected = true;
            TreeView_Selected_Color((TreeViewItem)sender, Brushes.Black);
        }

        //Переход на вкладку конфигурация Концентратор
        private void TreeView_Config_TablePLC_Selected(object sender, RoutedEventArgs e)
        {
            tab_Config_USPD.IsSelected = true;
            TreeView_Selected_Color((TreeViewItem)sender, Brushes.Black);
        }

        //Переход на вкладку Данные
        private void TreeView_Data_Selected(object sender, RoutedEventArgs e)
        {
            tab_E_Data.IsSelected = true;
            TreeView_Selected_Color((TreeViewItem)sender, Brushes.Black);
        }

        //Переход на вкладку "Сервис/Настройка"
        private void TreeView_Service_Selected(object sender, RoutedEventArgs e)
        {
            tab_Service.IsSelected = true;
            TreeView_Selected_Color((TreeViewItem)sender, Brushes.Black);
        }

        //-----------------------------
        // Главное меню (контекстное)
        //-----------------------------

        //*** Файл ->
        //


        //*** Сервисные комманды ->

        //Пункт "Перезагрузить"
        private void button_Reboot_Click(object sender, RoutedEventArgs e)
        {
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Check_Pass, null, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Reboot, null, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Close_Session, null, 0);
        }
        //Пункт "Очистить память"
        private void button_EEPROM_BURN_Click(object sender, RoutedEventArgs e)
        {
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Check_Pass, null, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.EEPROM_Burn, null, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Close_Session, null, 0);
        }
        //Пункт "Включить режим обновления ПО"
        private void button_BOOTLOADER_On_Click(object sender, RoutedEventArgs e)
        {
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Check_Pass, null, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Bootloader, null, 0);
            CMD_Buffer.Add_CMD(link, protocol, (int)PulsePLCv2Commands.Close_Session, null, 0);
        }

        //*** Горячие клавиши ->

        //Пункт "Монитор запрос"
        private void menu_hotKey_Monitor(object sender, RoutedEventArgs e)
        {
            hotKey_Ctrl_M_Monitor_Request();
        }
        //Пункт "Запрос по таблице PLC"
        private void menu_hotKey_PLC_Request(object sender, RoutedEventArgs e)
        {
            hotKey_Ctrl_P_Request_PLC();
        }
        //Пункт "Чтение строк в таблице PLC"
        private void menu_hotKey_Read_PLC_Table(object sender, RoutedEventArgs e)
        {
            hotKey_Ctrl_R_Read_PLC_Table();
        }


        //*** Справка ->

        //Пункт "О флагах ошибок.."
        private void menu_Help_Errors_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Расшифровка сокращений флагов ошибок: \n" +
                "1. ОБ - Ошибка батареи\n" +
                "2. ББ - Режим без батарейки\n" +
                "3. П1 - Переполнение входа 1\n" +
                "4. П2 - Переполнение входа 2 \n" +
                "5. ОП - Ошибка памяти\n" +
                "6. ОВ - Ошибка времени", "Справка");
        }
        //Пункт "О заводских настройках.."
        private void menu_Help_Default_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Заводские настройки: \n" +
                "Пароль на чтение: 000000 \n" +
                "Пароль на запись: 111111 \n" +
                "Режим работы - Концентратор A, с батарейкой и часами \n" +
                "RS458 - только чтение, Bluetooth - только чтение \n" +
                "Импульсный вход 1 [Вкл., адреса PLC и Сетевой - 1] \n" +
                "Импульсный вход 2 [Вкл., адреса PLC и Сетевой - 2] \n", "Справка");
        }
        //Перейти на владку "О программе"
        private void menu_About_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { treeView_About.IsSelected = true; }));
        }
    }
}
