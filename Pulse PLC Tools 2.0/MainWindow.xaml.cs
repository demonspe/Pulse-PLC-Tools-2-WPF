using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data;

namespace Pulse_PLC_Tools_2._0
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    /// 

    public enum DebugLog_Msg_Type : int { Error, Warning, Normal, Good }
    public enum DebugLog_Msg_Direction : int { Send, Receive }
    public enum Status_Img : int { Connected, Disconnected, Access_Read, Access_Write }

    public partial class MainWindow : Window
    {
        public Link link;
        public Protocol protocol;
        public Command_Buffer CMD_Buffer;
        //Конфигурация
        public DeviceConfig deviceConfig;
        //Таблица PLC
        public List<DataGridRow_PLC> plc_table;
        //Таблица с показаниями
        //public List<DataGridRow_E_Data> e_data_table;

        //Значки для статуса соединения
        BitmapImage bitmap_red;
        BitmapImage bitmap_green;
        BitmapImage bitmap_Access_Read;
        BitmapImage bitmap_Access_Write;

        //Функция для установки главноего окна по центру экрана
        private void CenterWindowOnScreen()
        {
            double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
            double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;
            double windowWidth = this.Width;
            double windowHeight = this.Height;
            this.Left = (screenWidth / 2) - (windowWidth / 2);
            this.Top = (screenHeight / 2) - (windowHeight / 2);
        }

        public MainWindow()
        {
            InitializeComponent();

            CenterWindowOnScreen();

            //Загрузим изображения
            bitmap_red = new BitmapImage();
            bitmap_red.BeginInit();
            bitmap_red.UriSource = new Uri("Pics/red.png", UriKind.Relative);
            bitmap_red.EndInit();

            bitmap_green = new BitmapImage();
            bitmap_green.BeginInit();
            bitmap_green.UriSource = new Uri("Pics/green.png", UriKind.Relative);
            bitmap_green.EndInit();

            bitmap_Access_Read = new BitmapImage();
            bitmap_Access_Read.BeginInit();
            bitmap_Access_Read.UriSource = new Uri("Pics/access_Read.png", UriKind.Relative);
            bitmap_Access_Read.EndInit();

            bitmap_Access_Write = new BitmapImage();
            bitmap_Access_Write.BeginInit();
            bitmap_Access_Write.UriSource = new Uri("Pics/access_Write.png", UriKind.Relative);
            bitmap_Access_Write.EndInit();
        }

        //Форма загружена
        private void mainForm_Loaded(object sender, RoutedEventArgs e)
        {
            //Связь
            protocol = new Protocol(this);
            link = new Link(this);
            CMD_Buffer = new Command_Buffer(this);
            //Конфигурация
            deviceConfig = new DeviceConfig(this);

            //Логи
            textBox_Log_Debug.Visibility = Visibility.Visible;
            textBox_Log_Debug_ex.Visibility = Visibility.Hidden;

            //Количество тарифов
            comboBox_Tqty_imp1.Items.Add("Один");
            comboBox_Tqty_imp1.Items.Add("Два");
            comboBox_Tqty_imp1.Items.Add("Три");
            comboBox_Tqty_imp1.SelectedIndex = 0;
            comboBox_Tqty_imp2.Items.Add("Один");
            comboBox_Tqty_imp2.Items.Add("Два");
            comboBox_Tqty_imp2.Items.Add("Три");
            comboBox_Tqty_imp2.SelectedIndex = 0;
            //Переполнение
            comboBox_perepoln_imp1.Items.Add("Без сброса");
            comboBox_perepoln_imp1.Items.Add(" 99 999,99");
            comboBox_perepoln_imp1.Items.Add("999 999,99");
            comboBox_perepoln_imp2.Items.Add("Без сброса");
            comboBox_perepoln_imp2.Items.Add(" 99 999,99");
            comboBox_perepoln_imp2.Items.Add("999 999,99");
            //Режимы работы
            comboBox_work_mode.Items.Add("Счетчик");
            comboBox_work_mode.Items.Add("Концентратор A");
            comboBox_work_mode.Items.Add("Концентратор B");
            comboBox_work_mode.Items.Add("Концентратор C");
            //Батарея
            comboBox_battery_mode.Items.Add("Часы+Тарифы+Архив");
            comboBox_battery_mode.Items.Add("Без батареи");
            //Интерфейсы
            comboBox_RS485_Is_Enable.Items.Add("Отключен");
            comboBox_RS485_Is_Enable.Items.Add("Только чтение");
            comboBox_RS485_Is_Enable.Items.Add("Чтение/Запись");
            comboBox_Bluetooth_Is_Enable.Items.Add("Отключен");
            comboBox_Bluetooth_Is_Enable.Items.Add("Только чтение");
            comboBox_Bluetooth_Is_Enable.Items.Add("Чтение/Запись");

            comboBox_protocol_imp1.Items.Add("Pulse PLC");
            comboBox_protocol_imp1.Items.Add("Pulse PLC + Mercury 230ART");
            //comboBox_protocol_imp1.Items.Add("Pulse PLC + Modbus RTU");
            //comboBox_protocol_imp1.Items.Add("Pulse PLC + Энергомера CE303");
            comboBox_protocol_imp2.Items.Add("Pulse PLC");
            comboBox_protocol_imp2.Items.Add("Pulse PLC + Mercury 230ART");
            //comboBox_protocol_imp2.Items.Add("Pulse PLC + Modbus RTU");
            //comboBox_protocol_imp2.Items.Add("Pulse PLC + Энергомера CE303");
            //Импульсные входы checkBoxs
            groupBox_IMP1.IsEnabled = false;
            groupBox_IMP2.IsEnabled = false;
            //Заполним все контролы стандартными значениями
            deviceConfig.Show_On_Form();

            PLC_Table_Clear();
        }

        //Форма закрывается
        private void mainForm_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //при попытке закрытия программы
            //спрашивает стоит ли завершиться
            if (MessageBox.Show("Вы уверены что хотите закрыть программу? \nВсе несохраненные данные исчезнут", "Закрыть программу", (MessageBoxButton)System.Windows.Forms.MessageBoxButtons.YesNo) == MessageBoxResult.Yes)
            {
                link.Close_connections();
                //и после этого только завершается работа приложения
                System.Windows.Application.Current.Shutdown();
            }
            else
            {
                //устанавливает флаг отмены события в истину
                e.Cancel = true;
            }
        }
        //************************************************************************************************************ - ОБЩИЕ функции
        //_______________________________________________________________________________________
        //
        //Показать сообщение в статус баре
        public void msg(string message)
        {
            status_msg.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { status_msg.Text = message; }));
        }
        //Добавить строку в журнал
        public void DataGrid_Log_Add_Row(DataGrid dataGrid_journal, DataGridRow_Log row)
        {
            mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {
                dataGrid_journal.Items.Add(row);
            }));
        }
        //Установить значок и текст в панеле статуса соединения
        public void Set_Connection_StatusBar(Status_Img img, string link_name_)
        {
            if (link == null) return;
            link.link_name = link_name_;
            connect_status_img.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {
                if (img == Status_Img.Connected) connect_status_img.Source = bitmap_green;
                if (img == Status_Img.Disconnected) connect_status_img.Source = bitmap_red;
                if (img == Status_Img.Access_Read) connect_status_img.Source = bitmap_Access_Read;
                if (img == Status_Img.Access_Write) connect_status_img.Source = bitmap_Access_Write;

            }));
            connect_status_txt.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {
                connect_status_txt.Text = link_name_;
            }));
        }
        //Обновить список COM портов
        public void Update_COM_List()
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

        }

        //____________________________
        //
        // Горячие клавиши
        //____________________________
        public void hotKey_Ctrl_P_Request_PLC()
        {
            PLC_Table_Refresh();
            byte[] selected_ = PLC_Table_Get_Selected_Items();
            if (selected_[0] == 0) return;
            CMD_Buffer.Add_CMD(Command_type.Check_Pass, link, null, 0);
            for (int i = 0; i < selected_[0]; i++)
            {
                byte n_st = plc_table[selected_[i + 1] - 1].N;
                byte st1 = plc_table[selected_[i + 1] - 1].S1;
                byte st2 = plc_table[selected_[i + 1] - 1].S2;
                byte st3 = plc_table[selected_[i + 1] - 1].S3;
                byte st4 = plc_table[selected_[i + 1] - 1].S4;
                byte st5 = plc_table[selected_[i + 1] - 1].S5;
                CMD_Buffer.Add_CMD(Command_type.Request_PLC, link, new byte[] { selected_[i + 1], n_st, st1, st2, st3, st4, st5 }, 0);
                PLC_Table_Send_Data_Request(Command_type.Read_PLC_Table, new byte[] { 1, selected_[i + 1] });
            }
            //PLC_Table_Send_Data_Request(Command_type.Read_PLC_Table, selected_);
            CMD_Buffer.Add_CMD(Command_type.Close_Session, link, null, 0);
        }
        public void hotKey_Ctrl_M_Monitor_Request()
        {
            treeView_IMPS_Monitor.IsSelected = true;
            CMD_Buffer.Add_CMD(Command_type.Check_Pass, link, null, 0);
            CMD_Buffer.Add_CMD(Command_type.Read_IMP_extra, link, IMP_type.IMP1, 0);
            CMD_Buffer.Add_CMD(Command_type.Read_IMP_extra, link, IMP_type.IMP2, 0);
            CMD_Buffer.Add_CMD(Command_type.Close_Session, link, null, 0);
        }
        public void hotKey_Ctrl_R_Read_PLC_Table()
        {
            byte[] selected_ = PLC_Table_Get_Selected_Items();
            if (selected_[0] == 0) return;
            CMD_Buffer.Add_CMD(Command_type.Check_Pass, link, null, 0);
            PLC_Table_Send_Data_Request(Command_type.Read_PLC_Table, selected_);
            CMD_Buffer.Add_CMD(Command_type.Close_Session, link, null, 0);
        }
        //Обработка нажатия клавиш главной формы
        private void mainForm_KeyDown(object sender, KeyEventArgs e)
        {
            //Отменить все запросы
            if (!CMD_Buffer.Buffer_Is_Emty() && e.Key == Key.Escape) CMD_Buffer.Clear_Buffer();

            //Отправить запрос PLC на выделенные в таблице устройства
            if (CMD_Buffer.Buffer_Is_Emty() && e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.P)
            {
                hotKey_Ctrl_P_Request_PLC();
            }
            //Монитор параметров импульсных входов
            if (CMD_Buffer.Buffer_Is_Emty() && e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.M)
            {
                hotKey_Ctrl_M_Monitor_Request();
            }
            //Прочитать строки в таблице PLC
            if (CMD_Buffer.Buffer_Is_Emty() && e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.R)
            {
                hotKey_Ctrl_R_Read_PLC_Table();
            }
            
        }
    }
}