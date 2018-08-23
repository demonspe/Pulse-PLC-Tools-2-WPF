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
        //Вкладка "Связь" обработка событий контролов
        //

        //Метод - Перейти ко вкладке "Связь" (при проблемах со связью)
        public void Link_Tab_Select()
        {
            Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { treeView_Link.IsSelected = true; }));
        }

        //Кнопка "Открыть/Закрыть канал связи"
        private void button_open_com_Click(object sender, RoutedEventArgs e)
        {
            if (link.connection == Link_type.Not_connected)
            {

                //Если выбран COM порт в качестве канала связи
                if ((bool)radioButton_COM.IsChecked)
                {
                    if (link.Open_connection_COM(comboBox_COM.Text))
                    {
                        CMD_Buffer.Add_CMD(Command_type.Search_Devices, link, null, 0);
                        CMD_Buffer.Add_CMD(Command_type.Close_Session, link, null, 0);
                    }
                    return;
                }
                //Если выбран TCP в качестве канала связи
                if ((bool)radioButton_TCP.IsChecked)
                {
                    //if (link.Open_connection_TCP()) { }
                    return;
                }
                //Если выбран TCP в качестве канала связи
                if ((bool)radioButton_GSM.IsChecked)
                {
                    return;
                }
            }
            else
            {
                if (link.Close_connections()) { }
            }
        }

    }
}
