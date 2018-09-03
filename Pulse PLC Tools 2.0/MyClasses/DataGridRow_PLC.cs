using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Pulse_PLC_Tools_2._0
{

    public class DataGridRow_PLC
    {
        //Проверить содержит ли строка только цифры
        public bool IsDigitsOnly(string str)
        {
            foreach (char c in str)
            {
                if (c < '0' || c > '9')
                    return false;
            }

            return true;
        }

        bool m_enable = false;
        public bool Enable { get { return m_enable; } set { m_enable = value; } }

        byte m_adrs_PLC;
        public byte Adrs_PLC { get { return m_adrs_PLC; } set { if (value >= 1 && value <= 250) m_adrs_PLC = value; } }

        public byte[] serial_bytes = new byte[4] { 0, 0, 0, 0 };
        public string Serial
        {
            get { return serial_bytes[0].ToString("00") + serial_bytes[1].ToString("00") + serial_bytes[2].ToString("00") + serial_bytes[3].ToString("00"); }
            set
            {
                if (value == "0") serial_bytes = new byte[4] { 0, 0, 0, 0 };
                if (value.Length == 8)
                {
                    serial_bytes = new byte[4] { Convert.ToByte(value.Substring(0, 2)), Convert.ToByte(value.Substring(2, 2)), Convert.ToByte(value.Substring(4, 2)), Convert.ToByte(value.Substring(6, 2)) };
                }
            }
        }

        byte m_N = 0, m_S1 = 0, m_S2 = 0, m_S3 = 0, m_S4 = 0, m_S5 = 0;
        public byte N { get { return m_N; } set { if (value >= 0 && value <= 5) m_N = value; } }
        public byte S1 { get { return m_S1; } set { if (value >= 1 && value <= 250) m_S1 = value; } }
        public byte S2 { get { return m_S2; } set { if (value >= 1 && value <= 250) m_S2 = value; } }
        public byte S3 { get { return m_S3; } set { if (value >= 1 && value <= 250) m_S3 = value; } }
        public byte S4 { get { return m_S4; } set { if (value >= 1 && value <= 250) m_S4 = value; } }
        public byte S5 { get { return m_S5; } set { if (value >= 1 && value <= 250) m_S5 = value; } }

        byte protocol_type = 0;
        public byte Protocol_ASCUE { get { return protocol_type; } set { if (value >= 0 && value <= 4) protocol_type = value; } }

        UInt16 m_adrs_ASCUE = 0;
        public UInt16 Adrs_ASCUE { get { return m_adrs_ASCUE; } set { m_adrs_ASCUE = value; } }

        public byte[] pass_bytes = new byte[6] { 1, 1, 1, 1, 1, 1 };
        public string Pass_ASCUE
        {
            get
            {
                return pass_bytes[0].ToString() +
                  pass_bytes[1].ToString() +
                  pass_bytes[2].ToString() +
                  pass_bytes[3].ToString() +
                  pass_bytes[4].ToString() +
                  pass_bytes[5].ToString();
            }
            set
            {
                if (IsDigitsOnly(value) && value.Length == 6)
                {
                    pass_bytes = new byte[6] { Convert.ToByte(value.Substring(0, 1)),
                                               Convert.ToByte(value.Substring(1, 1)),
                                               Convert.ToByte(value.Substring(2, 1)),
                                               Convert.ToByte(value.Substring(3, 1)),
                                               Convert.ToByte(value.Substring(4, 1)),
                                               Convert.ToByte(value.Substring(5, 1)) };
                }
            }
        }
        public bool Link { get; set; }
        public byte link_Day = 0, link_Month = 0, link_Year = 0, link_Hours = 0, link_Minutes = 0;
        public string Date_Link
        {
            get
            {
                if (link_Day > 0 && link_Day < 32 && link_Month > 0 && link_Month < 13 && link_Year < 100 && link_Hours < 24 && link_Minutes < 60)
                    return link_Day.ToString("00") + "." + link_Month.ToString("00") + "." + link_Year.ToString("00") + " " + link_Hours.ToString("00") + ":" + link_Minutes.ToString("00");
                else return "-";
            }
        } //Дата последней успешной связи по PLC
        public byte quality = 200;
        public string Quality { get { if (quality <= 100) return quality.ToString(); else return "-"; } }

        public byte type = 0;
        public string Type
        {
            get { if (type == 11) return "PLCv1"; if (type == 22) return "PLCv2"; return "-"; }
            set { if (value == "0") type = 0; if (value == "1") type = 11; if (value == "2") type = 22; }
        }

        public byte errors_byte = 0;
        public string Errors
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
        //***********************************************************************
        //
        //          Показания
        //
        //***********************************************************************
        //Подсчитать сумму тарифов
        void e_Summ()
        {
            e_Current_Summ = 0;
            if (e_Current_T1 < 0xFFFFFFFF) e_Current_Summ += e_Current_T1;
            if (e_Current_T1 < 0xFFFFFFFF) e_Current_Summ += e_Current_T2;
            if (e_Current_T1 < 0xFFFFFFFF) e_Current_Summ += e_Current_T3;

            e_StartDay_Summ = 0;
            if (e_StartDay_T1 < 0xFFFFFFFF) e_StartDay_Summ += e_StartDay_T1;
            if (e_StartDay_T2 < 0xFFFFFFFF) e_StartDay_Summ += e_StartDay_T2;
            if (e_StartDay_T3 < 0xFFFFFFFF) e_StartDay_Summ += e_StartDay_T3;
        }

        //Текущие
        UInt32 e_Current_T1;
        UInt32 e_Current_T2;
        UInt32 e_Current_T3;
        UInt32 e_Current_Summ;
        public bool e_Current_Correct = false;
        public string E_Current_T1 { get { if (e_Current_Correct && e_Current_T1 < 0xFFFFFFFF) return (((double)e_Current_T1) / 1000f).ToString(); else return "-"; } set { UInt32.TryParse(value, out e_Current_T1); } }
        public string E_Current_T2 { get { if (e_Current_Correct && e_Current_T1 < 0xFFFFFFFF) return (((double)e_Current_T2) / 1000f).ToString(); else return "-"; } set { UInt32.TryParse(value, out e_Current_T2); } }
        public string E_Current_T3 { get { if (e_Current_Correct && e_Current_T1 < 0xFFFFFFFF) return (((double)e_Current_T3) / 1000f).ToString(); else return "-"; } set { UInt32.TryParse(value, out e_Current_T3); } }
        public string E_Current_Summ { get { if (e_Current_Correct) return (((double)e_Current_Summ) / 1000f).ToString(); else return "-"; } }
        //Начало суток

        UInt32 e_StartDay_T1;
        UInt32 e_StartDay_T2;
        UInt32 e_StartDay_T3;
        UInt32 e_StartDay_Summ;
        public bool e_StartDay_Correct = false;
        public string E_StartDay_T1 { get { if (e_StartDay_Correct && e_StartDay_T1 < 0xFFFFFFFF) return (((double)e_StartDay_T1) / 1000f).ToString(); else return "-"; } set { UInt32.TryParse(value, out e_StartDay_T1); e_Summ(); } }
        public string E_StartDay_T2 { get { if (e_StartDay_Correct && e_StartDay_T2 < 0xFFFFFFFF) return (((double)e_StartDay_T2) / 1000f).ToString(); else return "-"; } set { UInt32.TryParse(value, out e_StartDay_T2); e_Summ(); } }
        public string E_StartDay_T3 { get { if (e_StartDay_Correct && e_StartDay_T3 < 0xFFFFFFFF) return (((double)e_StartDay_T3) / 1000f).ToString(); else return "-"; } set { UInt32.TryParse(value, out e_StartDay_T3); e_Summ(); } }
        public string E_StartDay_Summ { get { if (e_StartDay_Correct) return (((double)e_StartDay_Summ) / 1000f).ToString(); else return "-"; } }
        //Начало месяца

    }
}
