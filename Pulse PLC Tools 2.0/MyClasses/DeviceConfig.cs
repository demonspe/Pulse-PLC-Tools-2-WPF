using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Pulse_PLC_Tools_2._0
{
    public class ImpsData
    {
        public byte Is_Enable;     //Включен/отключен
        public byte adrs_PLC;      //Сетевой адрес
        public UInt16 A;           //A передаточное число
        public UInt32 E_T1;        //Энергия ПО ТАРИФУ 1
        public UInt32 E_T2;        //Энергия ПО ТАРИФУ 2
        public UInt32 E_T3;        //Энергия ПО ТАРИФУ 3
        public UInt32 E_Tsum;      //Энергия СУММА
        public byte perepoln;      //число разрядов после которых происходит переполнение 0, 5 или 6
        public byte T_qty;         //Количество тарифов
        public UInt16 T1_Time_1;   //Начало первой пиковой зоны в минутах
        public UInt16 T3_Time_1;   //Начало первой полупиковой зоны в минутах
        public UInt16 T1_Time_2;   //Начало второй пиковой зоны в минутах
        public UInt16 T3_Time_2;   //Начало второй полупиковой зоны в минутах
        public UInt16 T2_Time;     //Начало ночной зоны в минутах
        public UInt16 ascue_adrs;  //Адрес для протокола
        public byte[] ascue_pass;  //Пароль для протокола
        public byte ascue_protocol;    //Тип протокола
        public UInt16 max_Power;    //Максимальная мощность нагрузки

        public ImpsData()
        {
            SetDefaultParams();
        }

        public void SetDefaultParams()
        {
            Is_Enable = 0;
            adrs_PLC = 1;
            A = 1600;
            E_T1 = 0;
            E_T2 = 0;
            E_T3 = 0;
            E_Tsum = 0;
            perepoln = 0;
            T_qty = 1;
            T1_Time_1 = 7 * 60;
            T3_Time_1 = 10 * 60;
            T1_Time_2 = 17 * 60;
            T3_Time_2 = 21 * 60;
            T2_Time = 23 * 60;
            ascue_adrs = 0;
            ascue_pass = new byte[] { 1,1,1,1,1,1};
            ascue_protocol = 0;
            max_Power = 0;
        }
    }

    public class DeviceData
    {

        public char[] Pass_Read;    //Пароль доступа к данным устройства с которым идет общение
        public char[] Pass_Write;   
        public byte[] serial_num;   //Серийный номер устройства с которым идет общение

        public byte Work_mode;          //Режим устройства (Счетчик/УСПД)
        public byte Mode_No_Battery;    //Режим работы без часов, тарифов и BKP (без батареи)
        public byte RS485_Work_Mode;
        public byte Bluetooth_Work_Mode;
        public DeviceData()
        {
            SetDefaultParams();
        }

        public void SetDefaultParams()
        {
            serial_num = new byte[] { 0, 0, 0, 0 };
            Pass_Read = new char[] { '0', '0', '0', '0', '0', '0' };
            Pass_Write = new char[] { '1', '1', '1', '1', '1', '1' };
            Work_mode = 0;
            Mode_No_Battery = 0;
            RS485_Work_Mode = 2;
            Bluetooth_Work_Mode = 1;
        }

    }

    public class TableData
    {

    }

    public class DeviceConfig
    {
        MainWindow mainForm;
        public ImpsData Imp1, Imp2;
        public DeviceData Device;
        public TableData Table;

        public DeviceConfig(MainWindow mainForm_)
        {
            mainForm = mainForm_;

            Imp1 = new ImpsData();
            Imp2 = new ImpsData();
            Device = new DeviceData();
        }

        public void Get_From_Form()
        {
            mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => {
                //IMP 1
                //Включен ли
                Imp1.Is_Enable = ((bool)mainForm.checkBox_IMP1_On.IsChecked) ? (byte)1 : (byte)0;
                //Адрес PLC
                byte.TryParse(mainForm.textBox_adrsPLC_imp1.Text, out Imp1.adrs_PLC);
                //А
                UInt16.TryParse(mainForm.textBox_A_imp1.Text, out Imp1.A);
                //Переполнение
                if (mainForm.comboBox_perepoln_imp1.SelectedIndex == 0) Imp1.perepoln = 0;
                if (mainForm.comboBox_perepoln_imp1.SelectedIndex == 1) Imp1.perepoln = 5;
                if (mainForm.comboBox_perepoln_imp1.SelectedIndex == 2) Imp1.perepoln = 6;
                //Показания
                double e_float = 0;
                double.TryParse(mainForm.textBox_E_T1_imp1.Text.Replace('.',','), out e_float);
                e_float = e_float * 1000;
                Imp1.E_T1 = (UInt32)Math.Round(e_float);
                double.TryParse(mainForm.textBox_E_T2_imp1.Text.Replace('.', ','), out e_float);
                e_float = e_float * 1000;
                Imp1.E_T2 = (UInt32)Math.Round(e_float);
                double.TryParse(mainForm.textBox_E_T3_imp1.Text.Replace('.', ','), out e_float);
                e_float = e_float * 1000;
                Imp1.E_T3 = (UInt32)Math.Round(e_float);
                //Количество тарифов
                if (mainForm.comboBox_Tqty_imp1.SelectedIndex >= 0 && mainForm.comboBox_Tqty_imp1.SelectedIndex <= 2) Imp1.T_qty = (byte)(mainForm.comboBox_Tqty_imp1.SelectedIndex + 1);
                //Время тарифов
                UInt16 hours_ = 0, min_ = 0;
                UInt16.TryParse(mainForm.textBox_T1_1_imp1.Text.Substring(0, 2), out hours_);
                UInt16.TryParse(mainForm.textBox_T1_1_imp1.Text.Substring(3, 2), out min_);
                Imp1.T1_Time_1 = (UInt16)(hours_ * 60 + min_);
                UInt16.TryParse(mainForm.textBox_T3_1_imp1.Text.Substring(0, 2), out hours_);
                UInt16.TryParse(mainForm.textBox_T3_1_imp1.Text.Substring(3, 2), out min_);
                Imp1.T3_Time_1 = (UInt16)(hours_ * 60 + min_);
                UInt16.TryParse(mainForm.textBox_T1_2_imp1.Text.Substring(0, 2), out hours_);
                UInt16.TryParse(mainForm.textBox_T1_2_imp1.Text.Substring(3, 2), out min_);
                Imp1.T1_Time_2 = (UInt16)(hours_ * 60 + min_);
                UInt16.TryParse(mainForm.textBox_T3_2_imp1.Text.Substring(0, 2), out hours_);
                UInt16.TryParse(mainForm.textBox_T3_2_imp1.Text.Substring(3, 2), out min_);
                Imp1.T3_Time_2 = (UInt16)(hours_ * 60 + min_);
                UInt16.TryParse(mainForm.textBox_T2_imp1.Text.Substring(0, 2), out hours_);
                UInt16.TryParse(mainForm.textBox_T2_imp1.Text.Substring(3, 2), out min_);
                Imp1.T2_Time = (UInt16)(hours_ * 60 + min_);
                //Адрес аскуэ
                UInt16.TryParse(mainForm.textBox_adrsAscue_imp1.Text, out Imp1.ascue_adrs);
                //Пароль
                for (int i = 0; i < 6; i++)
                {
                    byte.TryParse(mainForm.textBox_passAscue_imp1.Text.Substring(i, 1), out Imp1.ascue_pass[i]);
                }
                //Протокол аскуэ
                if (mainForm.comboBox_protocol_imp1.SelectedIndex >= 0 && mainForm.comboBox_protocol_imp1.SelectedIndex <= 1)
                    Imp1.ascue_protocol = (byte)mainForm.comboBox_protocol_imp1.SelectedIndex;
                //Максимальная мощность
                UInt16.TryParse(mainForm.textBox_Max_Power_imp1.Text, out Imp1.max_Power);

                //IMP 2
                //Включен ли
                Imp2.Is_Enable = ((bool)mainForm.checkBox_IMP2_On.IsChecked) ? (byte)1 : (byte)0;
                //Адрес PLC
                byte.TryParse(mainForm.textBox_adrsPLC_imp2.Text, out Imp2.adrs_PLC);
                //А
                UInt16.TryParse(mainForm.textBox_A_imp2.Text, out Imp2.A);
                //Переполнение
                if (mainForm.comboBox_perepoln_imp2.SelectedIndex == 0) Imp2.perepoln = 0;
                if (mainForm.comboBox_perepoln_imp2.SelectedIndex == 1) Imp2.perepoln = 5;
                if (mainForm.comboBox_perepoln_imp2.SelectedIndex == 2) Imp2.perepoln = 6;
                //Показания
                e_float = 0;
                double.TryParse(mainForm.textBox_E_T1_imp2.Text.Replace('.', ','), out e_float);
                e_float = e_float * 1000;
                Imp2.E_T1 = (UInt32)Math.Round(e_float);
                double.TryParse(mainForm.textBox_E_T2_imp2.Text.Replace('.', ','), out e_float);
                e_float = e_float * 1000;
                Imp2.E_T2 = (UInt32)Math.Round(e_float);
                double.TryParse(mainForm.textBox_E_T3_imp2.Text.Replace('.', ','), out e_float);
                e_float = e_float * 1000;
                Imp2.E_T3 = (UInt32)Math.Round(e_float);
                //Количество тарифов
                if (mainForm.comboBox_Tqty_imp2.SelectedIndex >= 0 && mainForm.comboBox_Tqty_imp2.SelectedIndex <= 2) Imp2.T_qty = (byte)(mainForm.comboBox_Tqty_imp2.SelectedIndex + 1);
                //Время тарифов
                hours_ = 0;
                min_ = 0;
                UInt16.TryParse(mainForm.textBox_T1_1_imp2.Text.Substring(0, 2), out hours_);
                UInt16.TryParse(mainForm.textBox_T1_1_imp2.Text.Substring(3, 2), out min_);
                Imp2.T1_Time_1 = (UInt16)(hours_ * 60 + min_);
                UInt16.TryParse(mainForm.textBox_T3_1_imp2.Text.Substring(0, 2), out hours_);
                UInt16.TryParse(mainForm.textBox_T3_1_imp2.Text.Substring(3, 2), out min_);
                Imp2.T3_Time_1 = (UInt16)(hours_ * 60 + min_);
                UInt16.TryParse(mainForm.textBox_T1_2_imp2.Text.Substring(0, 2), out hours_);
                UInt16.TryParse(mainForm.textBox_T1_2_imp2.Text.Substring(3, 2), out min_);
                Imp2.T1_Time_2 = (UInt16)(hours_ * 60 + min_);
                UInt16.TryParse(mainForm.textBox_T3_2_imp2.Text.Substring(0, 2), out hours_);
                UInt16.TryParse(mainForm.textBox_T3_2_imp2.Text.Substring(3, 2), out min_);
                Imp2.T3_Time_2 = (UInt16)(hours_ * 60 + min_);
                UInt16.TryParse(mainForm.textBox_T2_imp2.Text.Substring(0, 2), out hours_);
                UInt16.TryParse(mainForm.textBox_T2_imp2.Text.Substring(3, 2), out min_);
                Imp2.T2_Time = (UInt16)(hours_ * 60 + min_);
                //Адрес аскуэ
                UInt16.TryParse(mainForm.textBox_adrsAscue_imp2.Text, out Imp2.ascue_adrs);
                //Пароль
                for (int i = 0; i < 6; i++)
                {
                    byte.TryParse(mainForm.textBox_passAscue_imp2.Text.Substring(i, 1), out Imp2.ascue_pass[i]);
                }
                //Протокол аскуэ
                if (mainForm.comboBox_protocol_imp2.SelectedIndex >= 0 && mainForm.comboBox_protocol_imp2.SelectedIndex <= 1)
                    Imp2.ascue_protocol = (byte)mainForm.comboBox_protocol_imp2.SelectedIndex;
                //Максимальная мощность
                UInt16.TryParse(mainForm.textBox_Max_Power_imp2.Text, out Imp2.max_Power);

                //Основные параметры
                if (mainForm.comboBox_work_mode.SelectedIndex >= 0 && mainForm.comboBox_work_mode.SelectedIndex <= 3) Device.Work_mode = (byte)mainForm.comboBox_work_mode.SelectedIndex;
                if (mainForm.comboBox_battery_mode.SelectedIndex >= 0 && mainForm.comboBox_battery_mode.SelectedIndex <= 1) Device.Mode_No_Battery = (byte)mainForm.comboBox_battery_mode.SelectedIndex;
                if (mainForm.comboBox_RS485_Is_Enable.SelectedIndex >= 0 && mainForm.comboBox_RS485_Is_Enable.SelectedIndex <= 2) Device.RS485_Work_Mode = (byte)mainForm.comboBox_RS485_Is_Enable.SelectedIndex;
                if (mainForm.comboBox_Bluetooth_Is_Enable.SelectedIndex >= 0 && mainForm.comboBox_Bluetooth_Is_Enable.SelectedIndex <= 2) Device.Bluetooth_Work_Mode = (byte)mainForm.comboBox_Bluetooth_Is_Enable.SelectedIndex;
                //Пароль на чтение
                int length_ = mainForm.textBox_Read_Pass.Text.Length <= 6 ? mainForm.textBox_Read_Pass.Text.Length : 6;
                for (int i = 0; i < 6; i++)
                {
                    if (i < length_)
                        Device.Pass_Read[i] = Convert.ToChar(mainForm.textBox_Read_Pass.Text.Substring(i, 1));
                    else
                        Device.Pass_Read[i] = Convert.ToChar(0xFF);
                }
                //Пароль
                length_ = mainForm.textBox_Write_Pass.Text.Length <= 6 ? mainForm.textBox_Write_Pass.Text.Length : 6;
                for (int i = 0; i < 6; i++)
                {
                    if (i < length_)
                        Device.Pass_Write[i] = Convert.ToChar(mainForm.textBox_Write_Pass.Text.Substring(i, 1));
                    else
                        Device.Pass_Write[i] = Convert.ToChar(0xFF);
                }
                //Серийный номер
                if (mainForm.comboBox_Serial.Text.Length >= 8)
                    for (int i = 0; i < 4; i++)
                    {
                        Device.serial_num[i] = Convert.ToByte(mainForm.comboBox_Serial.Text.Substring(i*2, 2));
                    }
            }));
        }

        public void Show_On_Form()
        {
            mainForm.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() => {
                //IMP 1
                mainForm.checkBox_IMP1_On.IsChecked = (bool)(Imp1.Is_Enable == 1) ? true : false;
                mainForm.imp1_draw();
                //Адрес PLC
                mainForm.textBox_adrsPLC_imp1.Text = Imp1.adrs_PLC.ToString();
                //A
                mainForm.textBox_A_imp1.Text = Imp1.A.ToString();
                //Переполнение
                if (Imp1.perepoln == 0) mainForm.comboBox_perepoln_imp1.SelectedIndex = 0;
                if (Imp1.perepoln == 5) mainForm.comboBox_perepoln_imp1.SelectedIndex = 1;
                if (Imp1.perepoln == 6) mainForm.comboBox_perepoln_imp1.SelectedIndex = 2;
                //Показания
                mainForm.textBox_E_T1_imp1.Text = (((double)Imp1.E_T1) / 1000).ToString();
                mainForm.textBox_E_T2_imp1.Text = (((double)Imp1.E_T2) / 1000).ToString();
                mainForm.textBox_E_T3_imp1.Text = (((double)Imp1.E_T3) / 1000).ToString();
                //Количество тарифов
                mainForm.comboBox_Tqty_imp1.SelectedIndex = (Imp1.T_qty >= 1 && Imp1.T_qty <= 3)? (Imp1.T_qty-1): -1;
                //Время тарифов
                mainForm.textBox_T1_1_imp1.Text = (Imp1.T1_Time_1 / 60).ToString("00") + ":" + (Imp1.T1_Time_1 % 60).ToString("00");
                mainForm.textBox_T3_1_imp1.Text = (Imp1.T3_Time_1 / 60).ToString("00") + ":" + (Imp1.T3_Time_1 % 60).ToString("00");
                mainForm.textBox_T1_2_imp1.Text = (Imp1.T1_Time_2 / 60).ToString("00") + ":" + (Imp1.T1_Time_2 % 60).ToString("00");
                mainForm.textBox_T3_2_imp1.Text = (Imp1.T3_Time_2 / 60).ToString("00") + ":" + (Imp1.T3_Time_2 % 60).ToString("00");
                mainForm.textBox_T2_imp1.Text = (Imp1.T2_Time / 60).ToString("00") + ":" + (Imp1.T2_Time % 60).ToString("00");
                //Адрес аскуэ
                mainForm.textBox_adrsAscue_imp1.Text = Imp1.ascue_adrs.ToString();
                //Пароль
                mainForm.textBox_passAscue_imp1.Text = "";
                for (int i = 0; i < 6; i++)
                {
                    mainForm.textBox_passAscue_imp1.Text += Imp1.ascue_pass[i];
                }
                //Протокол
                mainForm.comboBox_protocol_imp1.SelectedIndex = (Imp1.ascue_protocol >= 0 && Imp1.ascue_protocol <= 1) ? Imp1.ascue_protocol : -1;
                //Максимальная мощность
                mainForm.textBox_Max_Power_imp1.Text = Imp1.max_Power.ToString();

                //IMP 2
                mainForm.checkBox_IMP2_On.IsChecked = (bool)(Imp2.Is_Enable == 1) ? true : false;
                mainForm.imp2_draw();
                //Адрес PLC
                mainForm.textBox_adrsPLC_imp2.Text = Imp2.adrs_PLC.ToString();
                //A
                mainForm.textBox_A_imp2.Text = Imp2.A.ToString();
                //Переполнение
                if (Imp2.perepoln == 0) mainForm.comboBox_perepoln_imp2.SelectedIndex = 0;
                if (Imp2.perepoln == 5) mainForm.comboBox_perepoln_imp2.SelectedIndex = 1;
                if (Imp2.perepoln == 6) mainForm.comboBox_perepoln_imp2.SelectedIndex = 2;
                //Показания
                mainForm.textBox_E_T1_imp2.Text = (((double)Imp2.E_T1) / 1000).ToString();
                mainForm.textBox_E_T2_imp2.Text = (((double)Imp2.E_T2) / 1000).ToString();
                mainForm.textBox_E_T3_imp2.Text = (((double)Imp2.E_T3) / 1000).ToString();
                //Количество тарифов
                mainForm.comboBox_Tqty_imp2.SelectedIndex = (Imp2.T_qty >= 1 && Imp2.T_qty <= 3) ? (Imp2.T_qty - 1) : -1;
                //Время тарифов
                mainForm.textBox_T1_1_imp2.Text = (Imp2.T1_Time_1 / 60).ToString("00") + ":" + (Imp2.T1_Time_1 % 60).ToString("00");
                mainForm.textBox_T3_1_imp2.Text = (Imp2.T3_Time_1 / 60).ToString("00") + ":" + (Imp2.T3_Time_1 % 60).ToString("00");
                mainForm.textBox_T1_2_imp2.Text = (Imp2.T1_Time_2 / 60).ToString("00") + ":" + (Imp2.T1_Time_2 % 60).ToString("00");
                mainForm.textBox_T3_2_imp2.Text = (Imp2.T3_Time_2 / 60).ToString("00") + ":" + (Imp2.T3_Time_2 % 60).ToString("00");
                mainForm.textBox_T2_imp2.Text = (Imp2.T2_Time / 60).ToString("00") + ":" + (Imp2.T2_Time % 60).ToString("00");
                //Адрес аскуэ
                mainForm.textBox_adrsAscue_imp2.Text = Imp2.ascue_adrs.ToString();
                //Пароль
                mainForm.textBox_passAscue_imp2.Text = "";
                for (int i = 0; i < 6; i++)
                {
                    mainForm.textBox_passAscue_imp2.Text += Imp2.ascue_pass[i];
                }
                //Протокол
                mainForm.comboBox_protocol_imp2.SelectedIndex = (Imp2.ascue_protocol >= 0 && Imp2.ascue_protocol <= 1) ? Imp2.ascue_protocol : -1;
                //Максимальная мощность
                mainForm.textBox_Max_Power_imp2.Text = Imp2.max_Power.ToString();

                //Основные параметры
                mainForm.comboBox_work_mode.SelectedIndex = (Device.Work_mode >= 0 && Device.Work_mode <= 3) ? Device.Work_mode: -1;
                mainForm.comboBox_battery_mode.SelectedIndex = (Device.Mode_No_Battery >= 0 && Device.Mode_No_Battery <= 1) ? Device.Mode_No_Battery : -1;
                mainForm.comboBox_RS485_Is_Enable.SelectedIndex = (Device.RS485_Work_Mode >= 0 && Device.RS485_Work_Mode <= 2) ? Device.RS485_Work_Mode : -1;
                mainForm.comboBox_Bluetooth_Is_Enable.SelectedIndex = (Device.Bluetooth_Work_Mode >= 0 && Device.Bluetooth_Work_Mode <= 2) ? Device.Bluetooth_Work_Mode : -1;
                /*mainForm.textBox_Pass.Text =    Device.Pass_Write[0].ToString() + 
                                                Device.Pass_Write[1].ToString() + 
                                                Device.Pass_Write[2].ToString() + 
                                                Device.Pass_Write[3].ToString() + 
                                                Device.Pass_Write[4].ToString() + 
                                                Device.Pass_Write[5].ToString();*/
            }));
        }
    }
}
