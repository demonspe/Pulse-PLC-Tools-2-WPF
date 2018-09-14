using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse_PLC_Tools_2
{
    public enum WorkMode: byte { Counter = 0, USPD_A = 1, USPD_B = 2, USPD_C = 3 }
    public enum InterfaceMode: byte { Disable = 0, ReadOnly = 1, WriteRead = 2 }
    public enum BatteryMode: byte { Disable = 0, Enable = 1}

    public class DeviceMainParams : BindableBase
    {
        private char[] pass_Read;    //Пароль доступа к данным устройства с которым идет общение
        private char[] pass_Write;
        private byte[] serial_num;   //Серийный номер устройства с которым идет общение

        private byte work_mode;             //Режим устройства (Счетчик/УСПД)
        private byte mode_No_Battery;       //Режим работы без часов, тарифов и BKP (без батареи)
        private byte rs485_Work_Mode;       //Режим работы интерфейса (выкл, чтение, чтение/запись)
        private byte bluetooth_Work_Mode;   //Режим работы интерфейса (выкл, чтение, чтение/запись)

        public char[] Pass_Read { get => pass_Read; set { pass_Read = value; RaisePropertyChanged(nameof(Pass_Read)); } }
        public char[] Pass_Write { get => pass_Write; set { pass_Write = value; RaisePropertyChanged(nameof(Pass_Write)); } }
        public byte[] Serial_num { get => serial_num; set { serial_num = value; RaisePropertyChanged(nameof(Serial_num)); } }
        public WorkMode Work_mode { get => (WorkMode)work_mode; set { work_mode = (byte)value; RaisePropertyChanged(nameof(Work_mode)); } }
        public BatteryMode Mode_No_Battery { get => (BatteryMode)mode_No_Battery; set { mode_No_Battery = (byte)value; RaisePropertyChanged(nameof(Mode_No_Battery)); } }
        public InterfaceMode RS485_Work_Mode { get => (InterfaceMode)rs485_Work_Mode; set { rs485_Work_Mode = (byte)value; RaisePropertyChanged(nameof(RS485_Work_Mode)); } }
        public InterfaceMode Bluetooth_Work_Mode { get => (InterfaceMode)bluetooth_Work_Mode; set { bluetooth_Work_Mode = (byte)value; RaisePropertyChanged(nameof(Bluetooth_Work_Mode)); } }

        public DeviceMainParams()
        {
            SetDefaultParams();
        }

        public void SetDefaultParams()
        {
            Serial_num = new byte[] { 0, 0, 0, 0 };
            Pass_Read = new char[] {
                Convert.ToChar((byte)255),
                Convert.ToChar((byte)255),
                Convert.ToChar((byte)255),
                Convert.ToChar((byte)255),
                Convert.ToChar((byte)255),
                Convert.ToChar((byte)255)
            };
            Pass_Write = new char[] { '1', '1', '1', '1', '1', '1' };
            Work_mode = 0;
            Mode_No_Battery = BatteryMode.Enable;
            RS485_Work_Mode = InterfaceMode.ReadOnly;
            Bluetooth_Work_Mode = InterfaceMode.ReadOnly;
        }
    }
}
