using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse_PLC_Tools_2
{
    public enum ImpNum : int { IMP1 = 1, IMP2 }
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
        public ImpTime(byte hours, byte minutes)
        {
            this.hours = hours;
            this.minutes = minutes;
        }

        private byte hours;
        private byte minutes;

        public int TimeInMinutes { get => hours * 60 + minutes; }
        public byte Hours { get => hours;
            set
            {
                hours = value;
                if (value > 23) hours = 23;
                if (value < 0) hours = 0;
                RaisePropertyChanged(nameof(Hours));
                RaisePropertyChanged(nameof(TimeInMinutes));
            }
        }
        public byte Minutes { get => minutes;
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

    public class ImpEnergyValue : BindableBase
    {
        public ImpEnergyValue()
        {
            Value_Wt = 0;
        }
        public ImpEnergyValue(uint energyInWth)
        {
            Value_Wt = energyInWth;
        }
        private uint e; //Энергия в ваттах
        private double e_kWt; //Энергия в киловатах

        public uint Value_Wt
        {
            get => e;
            set
            {
                if (value > 3999999999)
                    e = 3999999999;
                else
                    e = value;
                e_kWt = (double)e / 1000;
                RaisePropertyChanged(nameof(Value_Wt));
                RaisePropertyChanged(nameof(Value_kWt));
            }
        }
        public double Value_kWt
        {
            get => e_kWt;
            set
            {
                if (value > (double)0xFFFFFFFF / 1000)
                    e_kWt = 3999999.999;
                else
                    e_kWt = value;
                e = Convert.ToUInt32(e_kWt * 1000);
                RaisePropertyChanged(nameof(Value_Wt));
                RaisePropertyChanged(nameof(Value_kWt));
            }
        }
    }

    public class ImpEnergyGroup : BindableBase
    {
        private bool isCorrect;
        private ImpEnergyValue e_T1_Value;
        private ImpEnergyValue e_T2_Value;
        private ImpEnergyValue e_T3_Value;

        public bool IsCorrect
        {
            get => isCorrect;
            set
            {
                isCorrect = value;
                RaisePropertyChanged(nameof(IsCorrect));
                RaisePropertyChanged(nameof(E_Summ_View));
                RaisePropertyChanged(nameof(E_T1_View));
                RaisePropertyChanged(nameof(E_T2_View));
                RaisePropertyChanged(nameof(E_T3_View));
            }
        }
        public ImpEnergyValue E_T1
        {
            get => e_T1_Value;
            set
            {
                e_T1_Value = value;
                RaisePropertyChanged(nameof(E_T1));
                RaisePropertyChanged(nameof(E_T1_View));
                RaisePropertyChanged(nameof(E_Summ_View));
            }
        }
        public ImpEnergyValue E_T2
        {
            get => e_T2_Value;
            set
            {
                e_T2_Value = value;
                RaisePropertyChanged(nameof(E_T2));
                RaisePropertyChanged(nameof(E_T2_View));
                RaisePropertyChanged(nameof(E_Summ_View));
            }
        }
        public ImpEnergyValue E_T3
        {
            get => e_T3_Value;
            set
            {
                e_T3_Value = value;
                RaisePropertyChanged(nameof(E_T3));
                RaisePropertyChanged(nameof(E_T3_View));
                RaisePropertyChanged(nameof(E_Summ_View));
            }
        }
        public string E_T1_View { get => (IsCorrect && E_T1.Value_Wt < 0xFFFFFFFF) ? E_T1.Value_kWt.ToString() : "-"; }
        public string E_T2_View { get => (IsCorrect && E_T2.Value_Wt < 0xFFFFFFFF) ? E_T2.Value_kWt.ToString() : "-"; }
        public string E_T3_View { get => (IsCorrect && E_T3.Value_Wt < 0xFFFFFFFF) ? E_T3.Value_kWt.ToString() : "-"; }
        public string E_Summ_View
        {
            get
            {
                if (IsCorrect)
                {
                    double summ = 0;
                    if (E_T1.Value_Wt < 0xFFFFFFFF) summ += E_T1.Value_kWt;
                    if (E_T2.Value_Wt < 0xFFFFFFFF) summ += E_T2.Value_kWt;
                    if (E_T3.Value_Wt < 0xFFFFFFFF) summ += E_T3.Value_kWt;
                    return summ.ToString();
                }
                else return "-";
            }
        }

        public ImpEnergyGroup(bool isCorrect)
        {
            IsCorrect = isCorrect;
            E_T1 = new ImpEnergyValue(0);
            E_T2 = new ImpEnergyValue(0);
            E_T3 = new ImpEnergyValue(0);

            E_T1.PropertyChanged += (s, a) => {
                RaisePropertyChanged(nameof(E_Summ_View));
                RaisePropertyChanged(nameof(E_T1_View));
            };
            E_T2.PropertyChanged += (s, a) => {
                RaisePropertyChanged(nameof(E_Summ_View));
                RaisePropertyChanged(nameof(E_T2_View));
            };
            E_T3.PropertyChanged += (s, a) => {
                RaisePropertyChanged(nameof(E_Summ_View));
                RaisePropertyChanged(nameof(E_T3_View));
            };
        }
    }

    public class ImpExParams : BindableBase
    {
        private ImpNum num;
        private byte currentTarif;
        private ushort impCounter;
        private uint timeMsFromLastImp;
        private uint currentPower;
        private DateTime actualAtTime;

        public ImpNum Num { get => num; set { num = value; RaisePropertyChanged(nameof(Num)); } }
        public byte CurrentTarif { get => currentTarif; set { currentTarif = value; RaisePropertyChanged(nameof(CurrentTarif)); } }
        public ushort ImpCounter { get => impCounter; set { impCounter = value; RaisePropertyChanged(nameof(ImpCounter)); } }
        public uint MsFromLastImp { get => timeMsFromLastImp; set { timeMsFromLastImp = value; RaisePropertyChanged(nameof(MsFromLastImp)); RaisePropertyChanged(nameof(SecFromLastImp)); } }
        public double SecFromLastImp { get => ((double)timeMsFromLastImp/1000); }
        public uint CurrentPower { get => currentPower; set { currentPower = value; RaisePropertyChanged(nameof(CurrentPower)); } }
        public DateTime ActualAtTime { get => actualAtTime; set { actualAtTime = value; RaisePropertyChanged(nameof(ActualAtTime)); } }

        public ImpExParams()
        {
            Num = ImpNum.IMP1;
            CurrentTarif = 1;
            ImpCounter = 0;
            MsFromLastImp = 0;
            CurrentPower = 0;
            ActualAtTime = DateTime.MinValue;
        }
        public ImpExParams(ImpNum num)
        {
            Num = num;
            CurrentTarif = 1;
            ImpCounter = 0;
            MsFromLastImp = 0;
            CurrentPower = 0;
            ActualAtTime = DateTime.MinValue;
        }
    }

    public class ImpParams : BindableBase
    {
        private ImpNum num;
        private byte isEnable;      //Включен/отключен
        private byte adrs_PLC;       //Сетевой адрес
        private ushort a;            //A передаточное число
        ImpEnergyGroup e_Current;
        ImpEnergyGroup e_StartDay;
        private ImpOverflowType perepoln;       //число разрядов после которых происходит переполнение 0, 5 или 6
        private int perepoln_view;  //число разрядов после которых происходит переполнение 0, 1 или 2 для отображения во view (ComboBox)
        private ImpNumOfTarifs t_qty;         //Количество тарифов
        private int t_qty_view;    //Количество тарифов для отображения во view (ComboBox)
        private ushort ascue_adrs;  //Адрес для протокола
        private byte[] ascue_pass;  //Пароль для протокола
        private string ascue_pass_string;  //Пароль для протокола
        private byte ascue_protocol;    //Тип протокола
        private ushort max_Power;    //Максимальная мощность нагрузки

        public ImpNum Num { get => num; set { num = value; RaisePropertyChanged(nameof(Num)); } }
        public byte IsEnable { get => isEnable; set { isEnable = value; RaisePropertyChanged(nameof(IsEnable)); } }
        public byte Adrs_PLC { get => adrs_PLC; set { adrs_PLC = value; RaisePropertyChanged(nameof(Adrs_PLC)); } }
        public ushort A { get => a; set { a = value; RaisePropertyChanged(nameof(A)); } }
        public ImpEnergyGroup E_Current { get => e_Current; set { e_Current = value; RaisePropertyChanged(nameof(E_Current)); } }
        public ImpEnergyGroup E_StartDay { get => e_StartDay; set { e_StartDay = value; RaisePropertyChanged(nameof(E_StartDay)); } }
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
                        if (char.IsDigit(ascue_pass_string[i])) ascue_pass[i] = Convert.ToByte(ascue_pass_string.Substring(i, 1));
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
            Num = ImpNum.IMP1;
            Adrs_PLC = 1;
            SetDefaultParams();
        }
        public ImpParams(ImpNum impNum)
        {
            Num = impNum;
            Adrs_PLC = (byte)impNum;
            SetDefaultParams();
        }
        
        public void SetDefaultParams()
        {
            IsEnable = 0;
            A = 1600;

            E_Current = new ImpEnergyGroup(true);
            E_StartDay = new ImpEnergyGroup(true);

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
