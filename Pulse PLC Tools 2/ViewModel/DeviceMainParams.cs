using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse_PLC_Tools_2
{
    public enum WorkMode: byte { Counter = 0, USPD_A = 1, USPD_B = 2, USPD_C = 3 }
    public enum InterfaceMode: byte { Disable = 0, ReadOnly = 1, WriteRead = 2 }
    public enum BatteryMode: byte { Enable = 0, Disable = 1}

    public class DeviceMainParams : BindableBase
    {
        private byte[] passWrite;  //Пароль доступа к данным устройства с которым идет общение
        private byte[] passRead;   //Пароль доступа к данным устройства с которым идет общение
        private byte[] passCurrent;   //Пароль текущий пароль который вводит пользователь
        private byte[] serial_bytes;  //Серийный номер устройства с которым идет общение
        private string serial_string;

        private byte errorsByte;
        private string firmwareVersion; //Версия прошивки
        private string eepromVersion; //Версия разметки памяти
        private byte work_mode;             //Режим устройства (Счетчик/УСПД)
        private byte mode_No_Battery;       //Режим работы без часов, тарифов и BKP (без батареи)
        private byte rs485_Work_Mode;       //Режим работы интерфейса (выкл, чтение, чтение/запись)
        private byte bluetooth_Work_Mode;   //Режим работы интерфейса (выкл, чтение, чтение/запись)
        private bool newPassWrite;  //Флаг записи нового пароля
        private bool newPassRead;   //Флаг записи нового пароля
        private DateTime deviceDateTime; //Время прочитанное из устройства

        public string VersionFirmware { get => firmwareVersion; set { firmwareVersion = value; RaisePropertyChanged(nameof(VersionFirmware)); } }
        public string VersionEEPROM { get => eepromVersion; set { eepromVersion = value; RaisePropertyChanged(nameof(VersionEEPROM)); } }

        public byte ErrorsByte { get => errorsByte;
            set
            {
                errorsByte = value;
                ErrorsList.Clear();
                if (errorsByte == 0) ErrorsList.Add("Нет ошибок");
                else
                {
                    if ((errorsByte & 1) > 0) ErrorsList.Add("Проблема с батарейкой");
                    if ((errorsByte & 2) > 0) ErrorsList.Add("Режим без батарейки");
                    if ((errorsByte & 4) > 0) ErrorsList.Add("Переполнение IMP1");
                    if ((errorsByte & 8) > 0) ErrorsList.Add("Переполнение IMP2");
                    if ((errorsByte & 16) > 0) ErrorsList.Add("Проблема с памятью");
                    if ((errorsByte & 32) > 0) ErrorsList.Add("Ошибка времени");
                    //!! добавить ошибки ДОДЕЛАТЬ
                }
                RaisePropertyChanged(nameof(ErrorsByte));
            }
        }
        public ObservableCollection<string> ErrorsList { get; }
        public byte[] Serial
        {
            get => serial_bytes;
            set
            {
                if (value.Length >= 4)
                {
                    serial_bytes = value;
                    serial_string = serial_bytes[0].ToString("00") + serial_bytes[1].ToString("00") + serial_bytes[2].ToString("00") + serial_bytes[3].ToString("00");
                    serial_bytes = new byte[4] { //Подгоняем длину массива под 4 
                        Convert.ToByte(serial_string.Substring(0, 2)),
                        Convert.ToByte(serial_string.Substring(2, 2)),
                        Convert.ToByte(serial_string.Substring(4, 2)),
                        Convert.ToByte(serial_string.Substring(6, 2))
                    };
                }
                RaisePropertyChanged(nameof(Serial));
                RaisePropertyChanged(nameof(Serial_View));
            }
        }
        public string Serial_View
        {
            get
            {
                string serial = serial_bytes[0].ToString("00") + serial_bytes[1].ToString("00") + serial_bytes[2].ToString("00") + serial_bytes[3].ToString("00");
                if (serial == "00000000") return string.Empty; else return serial;
            }
            set
            {
                if (value == null || value == string.Empty) return;
                string tmpStr = value;
                if (tmpStr.Length > 8) tmpStr = tmpStr.Substring(0, 8);
                bool isDigitsOnly = true;
                tmpStr.ToList().ForEach(ch => { if (!char.IsDigit(ch)) isDigitsOnly = false; } );
                if (isDigitsOnly)
                {
                    if (tmpStr == "0")
                    {
                        serial_string = "00000000";
                        serial_bytes = new byte[4] { 0, 0, 0, 0 };
                    }
                    if (tmpStr.Length == 8)
                    {
                        serial_string = tmpStr;
                        serial_bytes = new byte[4] {
                        Convert.ToByte(tmpStr.Substring(0, 2)),
                        Convert.ToByte(tmpStr.Substring(2, 2)),
                        Convert.ToByte(tmpStr.Substring(4, 2)),
                        Convert.ToByte(tmpStr.Substring(6, 2))
                        };
                    }
                }
                RaisePropertyChanged(nameof(Serial));
                RaisePropertyChanged(nameof(Serial_View));
            }
        }
        public byte[] PassWrite {
            get => passWrite;
            set
            {
                passWrite = new byte[6] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
                for (int i = 0; i < 6; i++) if (i < value.Length) passWrite[i] = value[i];
                RaisePropertyChanged(nameof(PassWrite_View));
            }
        }
        public byte[] PassRead
        {
            get => passRead;
            set
            {
                passRead = new byte[6] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
                for (int i = 0; i < 6; i++) if (i < value.Length) passRead[i] = value[i];
                RaisePropertyChanged(nameof(PassRead_View));
            }
        }
        public byte[] PassCurrent
        {
            get => passCurrent;
            set
            {
                passCurrent = new byte[6] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
                for (int i = 0; i < 6; i++) if (i < value.Length) passCurrent[i] = value[i];
                RaisePropertyChanged(nameof(PassCurrent_View));
            }
        }
        public string PassRead_View { get => Encoding.Default.GetString(passRead).Trim(Encoding.Default.GetString(new byte[1] { 255 })[0]);
            set
            {
                passRead = new byte[6] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
                for (int i = 0; i < 6; i++) if (i < value.Length) passRead[i] = Convert.ToByte(value[i]);
                PassRead = passRead;
            }
        }
        public string PassWrite_View { get => Encoding.Default.GetString(passWrite).Trim(Encoding.Default.GetString(new byte[1] { 255 })[0]);
            set
            {
                passWrite = new byte[6] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
                for (int i = 0; i < 6; i++) if (i < value.Length) passWrite[i] = Convert.ToByte(value[i]);
                PassWrite = passWrite;
            }
        }
        public string PassCurrent_View { get => Encoding.Default.GetString(passCurrent).Trim(Encoding.Default.GetString(new byte[1] { 255 })[0]);
            set
            {
                passCurrent = new byte[6] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
                for (int i = 0; i < 6; i++) if (i < value.Length) passCurrent[i] = Convert.ToByte(value[i]);
                PassCurrent = passCurrent;
            }
        }

        public WorkMode WorkMode { get => (WorkMode)work_mode; set { work_mode = (byte)value; RaisePropertyChanged(nameof(WorkMode_View)); } }
        public BatteryMode BatteryMode { get => (BatteryMode)mode_No_Battery; set { mode_No_Battery = (byte)value; RaisePropertyChanged(nameof(BatteryMode_View)); } }
        public InterfaceMode RS485_WorkMode { get => (InterfaceMode)rs485_Work_Mode; set { rs485_Work_Mode = (byte)value; RaisePropertyChanged(nameof(RS485_WorkMode_View)); } }
        public InterfaceMode Bluetooth_WorkMode { get => (InterfaceMode)bluetooth_Work_Mode; set { bluetooth_Work_Mode = (byte)value; RaisePropertyChanged(nameof(Bluetooth_WorkMode_View)); } }
        public byte WorkMode_View { get => work_mode; set { work_mode = value; RaisePropertyChanged(nameof(WorkMode_View)); } }
        public byte BatteryMode_View { get => mode_No_Battery; set { mode_No_Battery = value; RaisePropertyChanged(nameof(BatteryMode_View)); } }
        public byte RS485_WorkMode_View { get => rs485_Work_Mode; set { rs485_Work_Mode = value; RaisePropertyChanged(nameof(RS485_WorkMode_View)); } }
        public byte Bluetooth_WorkMode_View { get => bluetooth_Work_Mode; set { bluetooth_Work_Mode = value; RaisePropertyChanged(nameof(Bluetooth_WorkMode_View)); } }
        public bool NewPassWrite { get => newPassWrite; set { newPassWrite = value; RaisePropertyChanged(nameof(NewPassWrite)); } }
        public bool NewPassRead { get => newPassRead; set { newPassRead = value; RaisePropertyChanged(nameof(NewPassRead)); } }
        public DateTime DeviceDateTime
        {
            get => deviceDateTime;
            set
            {
                deviceDateTime = value;
                RaisePropertyChanged(nameof(DeviceDateTime));
                RaisePropertyChanged(nameof(PCDateTime));
                RaisePropertyChanged(nameof(TimeDifference));
            }
        }
        public DateTime PCDateTime { get => DateTime.Now; } //Время компьютера
        public TimeSpan TimeDifference { get => DeviceDateTime.Subtract(PCDateTime); } //Разница

        public DeviceMainParams()
        {
            ErrorsList = new ObservableCollection<string>();
            ErrorsList.Add("Нет ошибок");
            SetDefaultParams();
        }

        public void SetDefaultParams()
        {
            NewPassWrite = false;
            NewPassRead = false;
            Serial_View = "0";
            PassCurrent = new byte[6] { 255, 255, 255, 255, 255, 255};
            PassRead = new byte[6] { 255, 255, 255, 255, 255, 255 };
            PassWrite = new byte[6] { 255, 255, 255, 255, 255, 255 };
            WorkMode = WorkMode.Counter;
            BatteryMode = BatteryMode.Enable;
            RS485_WorkMode = InterfaceMode.ReadOnly;
            Bluetooth_WorkMode = InterfaceMode.ReadOnly;
        }
    }
}
