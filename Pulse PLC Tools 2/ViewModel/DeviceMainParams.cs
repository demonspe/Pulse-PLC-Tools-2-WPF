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
        private string pass_Write;  //Пароль доступа к данным устройства с которым идет общение
        private string pass_Read;   //Пароль доступа к данным устройства с которым идет общение
        private byte[] serial_num;  //Серийный номер устройства с которым идет общение

        private byte work_mode;             //Режим устройства (Счетчик/УСПД)
        private byte mode_No_Battery;       //Режим работы без часов, тарифов и BKP (без батареи)
        private byte rs485_Work_Mode;       //Режим работы интерфейса (выкл, чтение, чтение/запись)
        private byte bluetooth_Work_Mode;   //Режим работы интерфейса (выкл, чтение, чтение/запись)
        private bool newPassWrite;  //Флаг записи нового пароля
        private bool newPassRead;   //Флаг записи нового пароля

        public ObservableCollection<string> ListErrors { get; }
        public byte[] Serial { get => serial_num; set { serial_num = value; RaisePropertyChanged(nameof(Serial)); } }
        public string PassWrite {
            get => pass_Write;
            set
            {
                char[] tmp = new char[6] { Convert.ToChar(0xFF), Convert.ToChar(0xFF), Convert.ToChar(0xFF), Convert.ToChar(0xFF), Convert.ToChar(0xFF), Convert.ToChar(0xFF) };
                for (int i = 0; i < 6; i++) if (i < value.Length) tmp[i] = value[i];
                pass_Write = new string(tmp);
                RaisePropertyChanged(nameof(PassWrite_View));
            }
        }
        public string PassRead
        {
            get => pass_Read;
            set
            {
                char[] tmp = new char[6] { Convert.ToChar(0xFF), Convert.ToChar(0xFF), Convert.ToChar(0xFF), Convert.ToChar(0xFF), Convert.ToChar(0xFF), Convert.ToChar(0xFF) };
                for (int i = 0; i < 6; i++) if (i < value.Length) tmp[i] = value[i];
                pass_Read = new string(tmp);
                RaisePropertyChanged(nameof(PassRead_View));
            }
        }
        public string PassRead_View { get => pass_Read.Trim(Convert.ToChar(255)); set { PassRead = value; } }
        public string PassWrite_View { get => pass_Write.Trim(Convert.ToChar(255)); set { PassWrite = value; } }

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


        public DeviceMainParams()
        {
            ListErrors = new ObservableCollection<string>();
            ListErrors.Add("Нет ошибок");
            NewPassWrite = false;
            NewPassRead = false;
            SetDefaultParams();
        }

        public void SetDefaultParams()
        {
            Serial = new byte[] { 0, 0, 0, 0 };
            PassRead = "";
            PassWrite = "111111";
            WorkMode = 0;
            BatteryMode = BatteryMode.Enable;
            RS485_WorkMode = InterfaceMode.ReadOnly;
            Bluetooth_WorkMode = InterfaceMode.ReadOnly;
        }
    }
}
