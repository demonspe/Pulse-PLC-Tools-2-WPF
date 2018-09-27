using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Pulse_PLC_Tools_2
{
    public enum PLCProtocolType : byte { Undefined = 0, PLCv1 = 11, PLCv2 = 22 }

    public class DataGridRow_PLC : BindableBase
    {
        //Проверить содержит ли строка только цифры
        bool IsDigitsOnly(string str)
        {
            foreach (char c in str)
            {
                if (c < '0' || c > '9')
                    return false;
            }
            return true;
        }

        private bool isEnable = false;
        private byte adrs_PLC;
        private byte[] serial_bytes;
        private string serial_string;
        private byte n_steps;
        private byte[] steps;
        private ImpAscueProtocolType protocol_type;
        private ushort adrs_ASCUE;
        private byte[] pass_bytes;
        private string ascue_pass_string;
        private bool lastPLCRequestStatus;
        private DateTime lastPLCRequestTime;
        private byte quality = 100;
        private PLCProtocolType typePLC;
        private byte errors_byte;

        public bool IsEnable { get { return isEnable; } set { isEnable = value; RaisePropertyChanged(nameof(IsEnable)); } }
        public byte Adrs_PLC { get { return adrs_PLC; } set { if (value >= 1 && value <= 250) adrs_PLC = value; RaisePropertyChanged(nameof(Adrs_PLC)); } }
        public byte[] Serial
        {
            get => serial_bytes;
            set
            {
                if (serial_bytes.Length < 4) return;
                serial_bytes = value;
                serial_string = serial_bytes[0].ToString("00") + serial_bytes[1].ToString("00") + serial_bytes[2].ToString("00") + serial_bytes[3].ToString("00");
                serial_bytes = new byte[4] { //Подгоняем длину массива под 4 
                        Convert.ToByte(serial_string.Substring(0, 2)),
                        Convert.ToByte(serial_string.Substring(2, 2)),
                        Convert.ToByte(serial_string.Substring(4, 2)),
                        Convert.ToByte(serial_string.Substring(6, 2))
                    };
                RaisePropertyChanged(nameof(Serial));
                RaisePropertyChanged(nameof(Serial_View));
            }
        }
        public string Serial_View
        {
            get { return serial_bytes[0].ToString("00") + serial_bytes[1].ToString("00") + serial_bytes[2].ToString("00") + serial_bytes[3].ToString("00"); }
            set
            {
                if (IsDigitsOnly(value))
                {
                    if (value == "0")
                    {
                        serial_string = "00000000";
                        serial_bytes = new byte[4] { 0, 0, 0, 0 };
                    }
                    if (value.Length == 8)
                    {
                        serial_string = value;
                        serial_bytes = new byte[4] {
                        Convert.ToByte(value.Substring(0, 2)),
                        Convert.ToByte(value.Substring(2, 2)),
                        Convert.ToByte(value.Substring(4, 2)),
                        Convert.ToByte(value.Substring(6, 2))
                        };
                    }
                }
                RaisePropertyChanged(nameof(Serial));
                RaisePropertyChanged(nameof(Serial_View));
            }
        }
        public byte N { get { return n_steps; } set { if (value >= 0 && value <= 5) n_steps = value; RaisePropertyChanged(nameof(N)); } }
        public byte[] Steps { get => steps; }
        public byte S1 { get { return steps[0]; } set { if (value >= 0 && value <= 250) steps[0] = value; RaisePropertyChanged(nameof(S1)); RaisePropertyChanged(nameof(Steps)); } }
        public byte S2 { get { return steps[1]; } set { if (value >= 0 && value <= 250) steps[1] = value; RaisePropertyChanged(nameof(S2)); RaisePropertyChanged(nameof(Steps)); } }
        public byte S3 { get { return steps[2]; } set { if (value >= 0 && value <= 250) steps[2] = value; RaisePropertyChanged(nameof(S3)); RaisePropertyChanged(nameof(Steps)); } }
        public byte S4 { get { return steps[3]; } set { if (value >= 0 && value <= 250) steps[3] = value; RaisePropertyChanged(nameof(S4)); RaisePropertyChanged(nameof(Steps)); } }
        public byte S5 { get { return steps[4]; } set { if (value >= 0 && value <= 250) steps[4] = value; RaisePropertyChanged(nameof(S5)); RaisePropertyChanged(nameof(Steps)); } }
        public ImpAscueProtocolType Protocol_ASCUE { get { return protocol_type; } set { protocol_type = value; RaisePropertyChanged(nameof(Protocol_ASCUE)); } }
        public ushort Adrs_ASCUE { get { return adrs_ASCUE; } set { adrs_ASCUE = value; RaisePropertyChanged(nameof(Adrs_ASCUE)); } }
        public byte[] Pass_ASCUE
        {
            get => pass_bytes;
            set
            {
                if (value.Length != 6) { throw new Exception("Массив с паролем АСКУЭ имеет длину отличную от 6"); }
                pass_bytes = value;
                ascue_pass_string = "";
                for (int i = 0; i < 6; i++)
                {
                    ascue_pass_string += (i < pass_bytes.Length) ? pass_bytes[i].ToString() : "0";
                }
                RaisePropertyChanged(nameof(Pass_ASCUE));
                RaisePropertyChanged(nameof(Pass_ASCUE_View));
            }
        }
        public string Pass_ASCUE_View
        {
            get => ascue_pass_string;
            set
            {
                if (value.Length == 6)
                {
                    ascue_pass_string = value;
                    pass_bytes = new byte[6] { 0, 0, 0, 0, 0, 0 };
                    for (int i = 0; i < 6; i++)
                    {
                        if (char.IsDigit(ascue_pass_string[i])) pass_bytes[i] = Convert.ToByte(ascue_pass_string[i]);
                    }
                }
                RaisePropertyChanged(nameof(Pass_ASCUE));
                RaisePropertyChanged(nameof(Pass_ASCUE_View));
            }
        }
        public bool LastPLCRequestStatus { get => lastPLCRequestStatus; set { lastPLCRequestStatus = value; RaisePropertyChanged(nameof(LastPLCRequestStatus)); } }
        public DateTime LastPLCRequestTime
        {//Дата последней успешной связи по PLC
            get => lastPLCRequestTime;
            set
            {
                lastPLCRequestTime = value;
                RaisePropertyChanged(nameof(LastPLCRequestTime));
                RaisePropertyChanged(nameof(LastPLCRequestTime_View));
            }
        }
        public string LastPLCRequestTime_View
        {
            get { if (lastPLCRequestTime < new DateTime(2000, 1, 1)) return "-"; else return lastPLCRequestTime.ToString("dd:mm:yy hh:mm:ss"); }
        }
        public byte Quality { get => quality; set { quality = value; RaisePropertyChanged(nameof(Quality)); } }
        public PLCProtocolType TypePLC
        {
            get => typePLC;
            set{ typePLC = value; RaisePropertyChanged(nameof(TypePLC_View)); }
        }
        public string TypePLC_View
        {
            get { if (typePLC == PLCProtocolType.PLCv1) return "PLCv1"; if (typePLC == PLCProtocolType.PLCv2) return "PLCv2"; return "-"; }
            set {
                if (value == "0" || value == "-") typePLC = PLCProtocolType.Undefined;
                if (value == "1" || value == "PLCv1") typePLC = PLCProtocolType.PLCv1;
                if (value == "2" || value == "PLCv2") typePLC = PLCProtocolType.PLCv2;
                RaisePropertyChanged(nameof(TypePLC_View));
            }
        }
        public byte ErrorsByte
        {
            get => errors_byte;
            set { errors_byte = value; RaisePropertyChanged(nameof(ErrorsByte_View)); }
        }
        public string ErrorsByte_View
        {
            get
            {
                string err_str = "";
                if (errors_byte == 0) err_str = "-";
                if ((errors_byte & 1) > 0) err_str += "ОБ ";
                if ((errors_byte & 2) > 0) err_str += "ББ ";
                if ((errors_byte & 4) > 0) err_str += "П1 ";
                if ((errors_byte & 8) > 0) err_str += "П2 ";
                if ((errors_byte & 16) > 0) err_str += "ОП ";
                if ((errors_byte & 32) > 0) err_str += "ОВ ";
                //if ((errors_byte & 64) > 0) err_str += "   ";
                //if ((errors_byte & 128) > 0) err_str += "   ";
                return err_str;
            }
        }
        //Показания
        //Текущие
        public ImpEnergyGroup E_Current { get; set; }
        //Начало суток
        public ImpEnergyGroup E_StartDay { get; set; }
        //Начало месяца
        //-----
        //Начало года
        //-----
        public DataGridRow_PLC()
        {
            SetDefault();
        }
        public DataGridRow_PLC(byte adrsPLC)
        {
            SetDefault();
            Adrs_PLC = adrsPLC;
        }
        public DataGridRow_PLC(byte adrsPLC, bool enable)
        {
            SetDefault();
            Adrs_PLC = adrsPLC;
            IsEnable = enable;
        }

        void SetDefault()
        {
            LastPLCRequestTime = DateTime.MinValue;
            IsEnable = false;
            Adrs_PLC = 1;
            Serial_View = "0"; //to fill 0
            steps = new byte[5] { 0, 0, 0, 0, 0 };
            N = 0;
            S1 = 0;
            S2 = 0;
            S3 = 0;
            S4 = 0;
            S5 = 0;
            Protocol_ASCUE = ImpAscueProtocolType.PulsePLC;
            Adrs_ASCUE = 0;
            Pass_ASCUE_View = "111111";
            LastPLCRequestStatus = false;
            LastPLCRequestTime = DateTime.MinValue;
            Quality = 100;
            TypePLC = PLCProtocolType.Undefined;
            ErrorsByte = 0;
            E_Current = new ImpEnergyGroup(false);
            E_StartDay = new ImpEnergyGroup(false);
            E_Current.PropertyChanged += (s, a) => { RaisePropertyChanged(nameof(E_Current)); };
            E_StartDay.PropertyChanged += (s, a) => { RaisePropertyChanged(nameof(E_StartDay)); };
        }
    }
}
