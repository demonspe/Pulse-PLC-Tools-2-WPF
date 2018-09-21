using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse_PLC_Tools_2
{
    public enum ImpOverflowType: byte { Disable = 0, Overflow_5_Digits = 5, Overflow_6_Digits = 6 }
    public enum ImpNumOfTarifs: byte { One = 1, Two = 2, Three = 3 }
    public enum ImpAscueProtocolType : byte { PulsePLC = 0, Mercury230ART = 1 }

    public class ImpTime : BindableBase
    {
        public ImpTime()
        {
            this.hours = 0;
            this.minutes = 0;
        }
        public ImpTime(int hours, int minutes)
        {
            this.hours = hours;
            this.minutes = minutes;
        }

        private int hours;
        private int minutes;

        public int TimeInMinutes { get => hours * 60 + minutes; }
        public int Hours { get => hours;
            set
            {
                hours = value;
                if (value > 23) hours = 23;
                if (value < 0) hours = 0;
                RaisePropertyChanged(nameof(Hours));
                RaisePropertyChanged(nameof(TimeInMinutes));
            }
        }
        public int Minutes { get => minutes;
            set
            {
                minutes = value;
                if (value > 59) minutes = 59;
                if (value < 0) minutes = 0;
                RaisePropertyChanged(nameof(Minutes));
                RaisePropertyChanged(nameof(TimeInMinutes));
            }
        }
    }

    public class ImpParams : BindableBase
    {
        private  byte isEnable;      //Включен/отключен
        private  byte adrs_PLC;       //Сетевой адрес
        private ushort a;            //A передаточное число
        private ImpOverflowType perepoln;       //число разрядов после которых происходит переполнение 0, 5 или 6
        private int perepoln_view;  //число разрядов после которых происходит переполнение 0, 1 или 2 для отображения во view (ComboBox)
        private ImpNumOfTarifs t_qty;         //Количество тарифов
        private int t_qty_view;    //Количество тарифов для отображения во view (ComboBox)
        private ushort ascue_adrs;  //Адрес для протокола
        private byte[] ascue_pass;  //Пароль для протокола
        private string ascue_pass_string;  //Пароль для протокола
        private byte ascue_protocol;    //Тип протокола
        private ushort max_Power;    //Максимальная мощность нагрузки

        public byte IsEnable { get => isEnable; set { isEnable = value; RaisePropertyChanged(nameof(IsEnable)); } }
        public byte Adrs_PLC { get => adrs_PLC; set { adrs_PLC = value; RaisePropertyChanged(nameof(Adrs_PLC)); } }
        public ushort A { get => a; set { a = value; RaisePropertyChanged(nameof(A)); } }
        public ImpEnergyValue E_T1 { get; set; }
        public ImpEnergyValue E_T2 { get; set; }
        public ImpEnergyValue E_T3 { get; set; }
        public double E_Tsum_kWt { get => E_T1.Value_kWt + E_T2.Value_kWt + E_T3.Value_kWt; }
        public ImpEnergyValue E_T1_Start { get; set; }
        public ImpEnergyValue E_T2_Start { get; set; }
        public ImpEnergyValue E_T3_Start { get; set; }
        public double E_Tsum_Start_kWt { get => E_T1_Start.Value_kWt + E_T2_Start.Value_kWt + E_T3_Start.Value_kWt; }
        public ImpOverflowType Perepoln { get => perepoln;
            set {
                perepoln = value;
                if (perepoln == ImpOverflowType.Disable) perepoln_view = 0;
                if (perepoln == ImpOverflowType.Overflow_5_Digits) perepoln_view = 1;
                if (perepoln == ImpOverflowType.Overflow_6_Digits) perepoln_view = 2;
                RaisePropertyChanged(nameof(Perepoln));
                RaisePropertyChanged(nameof(Perepoln_View));
            }
        }
        public int Perepoln_View { get => perepoln_view;
            set {
                perepoln_view = value;
                if (perepoln_view == 0) perepoln = ImpOverflowType.Disable;
                if (perepoln_view == 1) perepoln = ImpOverflowType.Overflow_5_Digits;
                if (perepoln_view == 2) perepoln = ImpOverflowType.Overflow_6_Digits;
                RaisePropertyChanged(nameof(Perepoln));
                RaisePropertyChanged(nameof(Perepoln_View));
            }
        }
        public ImpNumOfTarifs T_qty { get => t_qty;
            set {
                t_qty = value;
                if (t_qty == ImpNumOfTarifs.One) t_qty_view = 0;
                if (t_qty == ImpNumOfTarifs.Two) t_qty_view = 1;
                if (t_qty == ImpNumOfTarifs.Three) t_qty_view = 2;
                RaisePropertyChanged(nameof(T_qty));
                RaisePropertyChanged(nameof(T_qty_View));
            }
        }
        public int T_qty_View { get => t_qty_view;
            set {
                t_qty_view = value;
                if (t_qty_view == 0) t_qty = ImpNumOfTarifs.One;
                if (t_qty_view == 1) t_qty = ImpNumOfTarifs.Two;
                if (t_qty_view == 2) t_qty = ImpNumOfTarifs.Three;
                RaisePropertyChanged(nameof(T_qty));
                RaisePropertyChanged(nameof(T_qty_View));
            }
        }
        public ImpTime T1_Time_1 { get; set; }
        public ImpTime T3_Time_1 { get; set; }
        public ImpTime T1_Time_2 { get; set; }
        public ImpTime T3_Time_2 { get; set; }
        public ImpTime T2_Time { get; set; }
        public ushort Ascue_adrs { get => ascue_adrs; set { ascue_adrs = value; RaisePropertyChanged(nameof(Ascue_adrs)); } }
        public byte[] Ascue_pass { get => ascue_pass;
            set {
                ascue_pass = value;
                ascue_pass_string = "";
                for (int i = 0; i < 6; i++)
                {
                    ascue_pass_string += (i < ascue_pass.Length) ? ascue_pass[i].ToString() : "0";
                }
                RaisePropertyChanged(nameof(Ascue_pass));
                RaisePropertyChanged(nameof(Ascue_pass_View));
            }
        }
        public string Ascue_pass_View { get => ascue_pass_string;
            set {
                ascue_pass_string = value;
                ascue_pass = new byte[6] { 0, 0, 0, 0, 0, 0 };
                for (int i = 0; i < 6; i++)
                {
                    if (i < ascue_pass_string.Length)
                        if (char.IsDigit(ascue_pass_string[i])) ascue_pass[i] = Convert.ToByte(ascue_pass_string[i]);
                }
                RaisePropertyChanged(nameof(Ascue_pass));
                RaisePropertyChanged(nameof(Ascue_pass_View));
            }
        }
        public byte Ascue_protocol { get => ascue_protocol; set { ascue_protocol = value; RaisePropertyChanged(nameof(Ascue_protocol)); } }
        public ushort Max_Power { get => max_Power; set { max_Power = value; RaisePropertyChanged(nameof(Max_Power)); } }

        public void UpdateAllProps()
        {
            RaisePropertyChanged(null);
        }

        public ImpParams()
        {
            Adrs_PLC = 1;
            SetDefaultParams();
        }
        public ImpParams(byte adrsPLC)
        {
            Adrs_PLC = adrsPLC;
            SetDefaultParams();
        }
        
        public void SetDefaultParams()
        {
            IsEnable = 0;
            A = 1600;

            E_T1 = new ImpEnergyValue();
            E_T2 = new ImpEnergyValue();
            E_T3 = new ImpEnergyValue();
            E_T1.PropertyChanged += (s, a) => { RaisePropertyChanged(nameof(E_Tsum_kWt)); };
            E_T2.PropertyChanged += (s, a) => { RaisePropertyChanged(nameof(E_Tsum_kWt)); };
            E_T3.PropertyChanged += (s, a) => { RaisePropertyChanged(nameof(E_Tsum_kWt)); };

            E_T1_Start = new ImpEnergyValue();
            E_T2_Start = new ImpEnergyValue();
            E_T3_Start = new ImpEnergyValue();
            E_T1_Start.PropertyChanged += (s, a) => { RaisePropertyChanged(nameof(E_Tsum_Start_kWt)); };
            E_T2_Start.PropertyChanged += (s, a) => { RaisePropertyChanged(nameof(E_Tsum_Start_kWt)); };
            E_T3_Start.PropertyChanged += (s, a) => { RaisePropertyChanged(nameof(E_Tsum_Start_kWt)); };

            Perepoln = ImpOverflowType.Disable;
            T_qty = ImpNumOfTarifs.One;
            T1_Time_1 = new ImpTime(7, 0); 
            T3_Time_1 = new ImpTime(10, 0);
            T1_Time_2 = new ImpTime(17, 0);
            T3_Time_2 = new ImpTime(21, 0);
            T2_Time = new ImpTime(23, 0);
            Ascue_adrs = 0;
            Ascue_pass = new byte[6] { 1, 1, 1, 1, 1, 1 };
            Ascue_protocol = (byte)ImpAscueProtocolType.PulsePLC;
            Max_Power = 0;
        }


    }
}
