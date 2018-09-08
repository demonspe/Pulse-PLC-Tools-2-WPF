using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

        //Чтение списка COM портов в системe
        void Reading_COM_List_Handler(object mainForm_)
        {
            while (true)
            {
                Update_COM_List();
                Thread.Sleep(500);  //Проверка каналов каждые 500 мс
            }
        }

        //Обновить список COM портов
        public void Update_COM_List()
        {
            mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {
                try
                {
                    //Мониторим com порты
                    string[] myPort; //создаем массив строк
                    myPort = System.IO.Ports.SerialPort.GetPortNames(); // в массив помещаем доступные порты
                    comboBox_COM.Items.Clear();
                    myPort.ToList().ForEach(item => comboBox_COM.Items.Add(item)); //Массив помещаем в comboBox_COM
                    if (comboBox_COM.Items.Count > 0) comboBox_COM.SelectedIndex = 0;
                }
                catch { }
            }));
        }

        //Кнопка "Открыть/Закрыть канал связи"
        private void Button_Open_Link_Click(object sender, RoutedEventArgs e)
        {
            //Если выбран COM порт в качестве канала связи
            if ((bool)radioButton_COM.IsChecked)
            {
                if (comboBox_COM.Text == "") { Log_Add_Line("Порт не выбран", Msg_Type.Warning); return; }
                link = new LinkCOM(comboBox_COM.Text);
                //Обработчики событий
                link.Connected += Link_Opened;
                link.Disconnected += Link_Closed;
                link.DataRecieved += protocol.DateRecieved;

                if (!link.Connect()) Log_Add_Line("Порт занят", Msg_Type.Warning);
                return;
            }
        }

        private void Button_Close_Link_Click(object sender, RoutedEventArgs e)
        {
            link.Disconnect();
        }

        private void Link_Opened(object sender, EventArgs e)
        {
            //if (access_Type == Access_Type.Read) img_ = Status_Img.Access_Read;
            //if (access_Type == Access_Type.Write) img_ = Status_Img.Access_Write;

            groupBox_link_type.IsEnabled = false;
            groupBox_dateTime.IsEnabled = true;
            button_open_com.IsEnabled = false;
            button_close_com.IsEnabled = true;

            Set_Connection_StatusBar(Status_Img.Connected, link.ConnectionString);
            //Отправим сообщение в статус бар
            msg("Открыт канал связи [" + link.ConnectionString + "]");
            Log_Add_Line("Открыт канал связи [" + link.ConnectionString + "]", Msg_Type.Normal);

            //CMD_Buffer.Add_CMD(Command_type.Search_Devices, link, null, 0);
            //CMD_Buffer.Add_CMD(Command_type.Close_Session, link, null, 0);
        }

        private void Link_Closed(object sender, EventArgs e)
        {
            groupBox_link_type.IsEnabled = true;
            //mainForm.grid_Access_Input.IsEnabled = false;
            groupBox_dateTime.IsEnabled = false;
            button_open_com.IsEnabled = true;
            button_close_com.IsEnabled = false;
            //Изменим значок
            mainForm.Set_Connection_StatusBar(Status_Img.Disconnected, "");
            //Сообщение о закрытии
            mainForm.msg("Порт закрыт");
            mainForm.Log_Add_Line("Канал связи закрыт", Msg_Type.Normal);
        }

        private void CommandSended(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() => {
                tabControl_main.IsEnabled = false;
                grid_Panel_Buttons.IsEnabled = false;
            }));
        }

        private void CommandBufferCleared(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() => {
                tabControl_main.IsEnabled = true;
                grid_Panel_Buttons.IsEnabled = true;
            }));
        }
    }
}
