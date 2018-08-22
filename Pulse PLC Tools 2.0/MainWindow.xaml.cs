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

    public class DataGridRow_Log
    {
        public string Num { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string Name { get; set; }
    }

    public partial class MainWindow : Window
    {
        public Link link;
        public Protocol protocol;

        //Значки для статуса соединения
        BitmapImage bitmap_red;
        BitmapImage bitmap_green;
        

        public MainWindow()
        {
            InitializeComponent();
            
            //Связь
            protocol = new Protocol(this);
            link = new Link(this);

            //Загрузим изображения
            bitmap_red = new BitmapImage();
            bitmap_red.BeginInit();
            bitmap_red.UriSource = new Uri("Pics/red.png", UriKind.Relative);
            bitmap_red.EndInit();

            bitmap_green = new BitmapImage();
            bitmap_green.BeginInit();
            bitmap_green.UriSource = new Uri("Pics/green.png", UriKind.Relative);
            bitmap_green.EndInit();
        }

        //Форма загружена
        private void mainForm_Loaded(object sender, RoutedEventArgs e)
        {
            //Заполним ComboBoxs данными для выбора
            //Минуты
            for (int i = 0; i < 24; i++)
            {
                T1_1_Hours.Items.Add(i.ToString());
                T3_1_Hours.Items.Add(i.ToString());
                T1_2_Hours.Items.Add(i.ToString());
                T3_2_Hours.Items.Add(i.ToString());
                T2_Hours.Items.Add(i.ToString());
            }
            //Часы
            for (int i = 0; i < 60; i++)
            {
                T1_1_Minutes.Items.Add(i.ToString());
                T3_1_Minutes.Items.Add(i.ToString());
                T1_2_Minutes.Items.Add(i.ToString());
                T3_2_Minutes.Items.Add(i.ToString());
                T2_Minutes.Items.Add(i.ToString());
            }
            //Количество тарифов
            comboBox_num_of_tarifs.Items.Add("Один");
            comboBox_num_of_tarifs.Items.Add("Два");
            comboBox_num_of_tarifs.Items.Add("Три");
            comboBox_num_of_tarifs.SelectedIndex = 0;
            //Режимы работы
            comboBox_mode.Items.Add("Счетчик");
            comboBox_mode.Items.Add("Концентратор A");
            comboBox_mode.Items.Add("Концентратор B");
            comboBox_mode.Items.Add("Концентратор C");
            //Импульсные входы checkBoxs
            groupBox_IMP1.IsEnabled = false;
            groupBox_IMP2.IsEnabled = false;
        }
        //*****************************************
        //ОБЩИЕ функции
        //_______________________________________
        //
        //Показать сообщение в статус баре
        public void msg(string message)
        {
            status_msg.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => { status_msg.Text = message; }));
        }
        //Добавить строку в журнал Power
        public void DataGrid_Log_Add_Row(DataGrid dataGrid_journal, DataGridRow_Log row)
        {
            mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => 
            {
                dataGrid_journal.Items.Add(row);
            }));
            
        }
        //Установить значок и текст в панеле статуса соединения
        public void Set_Connection_StatusBar(bool IsConnected, string link_name_)
        {
            link.link_name = link_name_;
            connect_status_img.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {
                if(IsConnected)
                    connect_status_img.Source = bitmap_green;
                else
                    connect_status_img.Source = bitmap_red;
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
            }
            catch { }
            
        }
        //**********************************************
        //Вкладка "Связь" обработка событий контролов
        //___________________________________________
        //
        //Кнопка "Открыть/Закрыть канал связи"
        private void button_open_com_Click(object sender, RoutedEventArgs e)
        {
            if (link.connection == Link_type.Not_connected)
            {
                //Если выбран COM порт в качестве канала связи
                if ((bool)radioButton_COM.IsChecked)
                {
                    if (link.Open_connection_COM(comboBox_COM.Text)) { }
                    protocol.CMD_Read_Serial(link);
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

        //Заполнить выпадающий список COM портов
        private void comboBox_COM_DropDownOpened(object sender, EventArgs e)
        {
            Update_COM_List();
        }

        //Кнопка "Связь"
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            protocol.CMD_Read_Serial(link);
        }

        //Кнопка "Записать Дату и Время"
        private void button_Write_DateTime_Click(object sender, RoutedEventArgs e)
        {
            protocol.CMD_Write_DateTime(link);
        }
        //Кнопка "Синхронизировать"
        private void button_Correction_DateTime_Click(object sender, RoutedEventArgs e)
        {

        }
        //Кнопка "Прочитать Дату и Время"
        private void button_Read_DateTime_Click(object sender, RoutedEventArgs e)
        {
            protocol.CMD_Read_DateTime(link);
        }

        //**********************************************
        //Вкладка "Конфигурация-> Счетчик" обработка событий контролов
        //___________________________________________
        //
        //Выбор количества тарифов
        private void comboBox_num_of_tarifs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (comboBox_num_of_tarifs.SelectedIndex)
            {
                case 0:
                    T1_1_Hours.IsEnabled = false;
                    T3_1_Hours.IsEnabled = false;
                    T1_2_Hours.IsEnabled = false;
                    T3_2_Hours.IsEnabled = false;
                    T2_Hours.IsEnabled = false;
                    T1_1_Minutes.IsEnabled = false;
                    T3_1_Minutes.IsEnabled = false;
                    T1_2_Minutes.IsEnabled = false;
                    T3_2_Minutes.IsEnabled = false;
                    T2_Minutes.IsEnabled = false;
                    textBox_E_T2.IsEnabled = false;
                    textBox_E_T3.IsEnabled = false;
                    break;
                case 1:
                    T1_1_Hours.IsEnabled = true;
                    T3_1_Hours.IsEnabled = false;
                    T1_2_Hours.IsEnabled = false;
                    T3_2_Hours.IsEnabled = false;
                    T2_Hours.IsEnabled = true;
                    T1_1_Minutes.IsEnabled = true;
                    T3_1_Minutes.IsEnabled = false;
                    T1_2_Minutes.IsEnabled = false;
                    T3_2_Minutes.IsEnabled = false;
                    T2_Minutes.IsEnabled = true;
                    textBox_E_T2.IsEnabled = true;
                    textBox_E_T3.IsEnabled = false;
                    break;
                case 2:
                    T1_1_Hours.IsEnabled = true;
                    T3_1_Hours.IsEnabled = true;
                    T1_2_Hours.IsEnabled = true;
                    T3_2_Hours.IsEnabled = true;
                    T2_Hours.IsEnabled = true;
                    T1_1_Minutes.IsEnabled = true;
                    T3_1_Minutes.IsEnabled = true;
                    T1_2_Minutes.IsEnabled = true;
                    T3_2_Minutes.IsEnabled = true;
                    T2_Minutes.IsEnabled = true;
                    textBox_E_T2.IsEnabled = true;
                    textBox_E_T3.IsEnabled = true;
                    break;
            }
        }
        //Включить/отключить импульсный вход 1
        private void checkBox_IMP1_On_Checked(object sender, RoutedEventArgs e)
        {
            groupBox_IMP1.IsEnabled = (bool)(checkBox_IMP1_On.IsChecked);
        }
        //Включить/отключить импульсный вход 2
        private void checkBox_IMP2_On_Checked(object sender, RoutedEventArgs e)
        {
            groupBox_IMP2.IsEnabled = (bool)(checkBox_IMP2_On.IsChecked);
        }
        //**********************************************
        //Вкладка "Журнал-> Питание" обработка событий контролов
        //___________________________________________
        //
        //Кнопка прочитать журнал вкл/откл устройства
        private void Button_Read_Log_Power_Click(object sender, RoutedEventArgs e)
        {
            dataGrid_Log_Power.Items.Clear();
            protocol.CMD_Read_Journal(link, Journal_type.POWER);
        }
        //**********************************************
        //Вкладка "Журнал-> Конфигурация" обработка событий контролов
        //___________________________________________
        //
        //Кнопка прочитать журнал вкл/откл устройства
        private void Button_Read_Log_Config_Click(object sender, RoutedEventArgs e)
        {
            dataGrid_Log_Config.Items.Clear();
            protocol.CMD_Read_Journal(link, Journal_type.CONFIG);
        }
        //**********************************************
        //Вкладка "Журнал-> Интерфейсы" обработка событий контролов
        //___________________________________________
        //
        //Кнопка прочитать журнал вкл/откл устройства
        private void Button_Read_Log_Interfaces_Click(object sender, RoutedEventArgs e)
        {
            dataGrid_Log_Interfaces.Items.Clear();
            protocol.CMD_Read_Journal(link, Journal_type.INTERFACES);
        }

        //**********************************************
        //Вкладка "Анализ обмена" обработка событий контролов
        //___________________________________________
        //
        //Метод для добавления строк в текстовый блок
        public void debug_Log_Add_Line(byte[] bytes, int count, bool out_direction, Link link_)
        {
            //Цвет зависит от направления данных
            Brush br;
            if (out_direction) { br = Brushes.Red; } else { br = Brushes.Blue; }
            //Получим канал
            string link_name = (out_direction) ? "->" : "<-";
            link_name += link_.link_name;
            link_name += "\n";

            //Байты в HEX
            string str_msg_HEX = "HEX: ";
            for (int k_ = 0; k_ < count; k_++) { str_msg_HEX += bytes[k_] + " "; }
            str_msg_HEX += "\n";
            //Байты в ASCII
            string str_msg_ASCII = "ASCII: ";
            for (int k_ = 0; k_ < count; k_++)
            {
                //Исключаем перенос строки
                if (bytes[k_] == 0x0D) str_msg_ASCII += "\\r";
                else if (bytes[k_] == 0x0A) str_msg_ASCII += "\\n";
                else
                    str_msg_ASCII += Convert.ToChar(bytes[k_]);
            }
            str_msg_ASCII += "\n";

            //Выводим информацию
            this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => {
                //Название канала
                textBlock_Log_Debug.Inlines.Add(new Bold(new Run(DateTime.Now + " " + link_name) { Foreground = br }));
                //В формате HEX[space]
                textBlock_Log_Debug.Inlines.Add(new Run(str_msg_HEX) { Foreground = br });
                //В формате ASCII
                textBlock_Log_Debug.Inlines.Add(new Run(str_msg_ASCII) { Foreground = br });
            }));

        }
        //Кнопка "Очистить"
        private void debug_Log_Clear_Click(object sender, RoutedEventArgs e)
        {
            textBlock_Log_Debug.Text = "";
        }
        //*****************************************
        //TreeView меню для навигации по вкладкам
        //_______________________________________
        //
        //Переход на вкладку связь
        private void TreeView_Link_Selected(object sender, RoutedEventArgs e)
        {
            tab_Link.IsSelected = true;
        }
        //Переход на вкладку доступ
        private void TreeView_Debug_Log_Selected(object sender, RoutedEventArgs e)
        {
            tab_Debug_Logs.IsSelected = true;
        }
       
        //Переход на вкладку журнал "Питание"
        private void TreeView_Log_Power_Selected(object sender, RoutedEventArgs e)
        {
            tab_Log_Power.IsSelected = true;
        }
        //Переход на вкладку журнал "Конфигурация"
        private void TreeView_Log_Config_Selected(object sender, RoutedEventArgs e)
        {
            tab_Log_Config.IsSelected = true;
        }
        //Переход на вкладку журнал "Интерфейсы"
        private void TreeView_Log_Interfaces_Selected(object sender, RoutedEventArgs e)
        {
            tab_Log_Interfaces.IsSelected = true;
        }
        //Переход на вкладку конфигурация Счетчик
        private void TreeView_Config_CNTR_Selected(object sender, RoutedEventArgs e)
        {
            tab_Config_CNTR.IsSelected = true;
        }
        //Переход на вкладку конфигурация Концентратор
        private void TreeView_Config_USPD_Selected(object sender, RoutedEventArgs e)
        {
            tab_Config_USPD.IsSelected = true;
        }
    }
}
