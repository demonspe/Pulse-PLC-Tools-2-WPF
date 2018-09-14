using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse_PLC_Tools_2
{
    public class ImpParams : BindableBase
    {
        private  byte isEnable;      //Включен/отключен
        private  byte adrs_PLC;       //Сетевой адрес
        private  UInt16 a;            //A передаточное число
        private  UInt32 e_T1;         //Энергия ПО ТАРИФУ 1
        private  UInt32 e_T2;         //Энергия ПО ТАРИФУ 2
        private  UInt32 e_T3;         //Энергия ПО ТАРИФУ 3
        private  UInt32 e_T1_Start;   //Энергия На начало суток ПО ТАРИФУ 1
        private  UInt32 e_T2_Start;   //Энергия На начало суток ПО ТАРИФУ 2
        private  UInt32 e_T3_Start;   //Энергия На начало суток ПО ТАРИФУ 3
        private  byte perepoln;       //число разрядов после которых происходит переполнение 0, 5 или 6
        private  byte t_qty;         //Количество тарифов
        private  UInt16 t1_Time_1;   //Начало первой пиковой зоны в минутах
        private  UInt16 t3_Time_1;   //Начало первой полупиковой зоны в минутах
        private  UInt16 t1_Time_2;   //Начало второй пиковой зоны в минутах
        private  UInt16 t3_Time_2;   //Начало второй полупиковой зоны в минутах
        private  UInt16 t2_Time;     //Начало ночной зоны в минутах
        private  UInt16 ascue_adrs;  //Адрес для протокола
        private  byte[] ascue_pass;  //Пароль для протокола
        private  byte ascue_protocol;    //Тип протокола
        private  UInt16 max_Power;    //Максимальная мощность нагрузки


        public byte IsEnable { get => isEnable; set { isEnable = value; RaisePropertyChanged(nameof(IsEnable)); } }
        public byte Adrs_PLC { get => adrs_PLC; set { adrs_PLC = value; RaisePropertyChanged(nameof(Adrs_PLC)); } }
        public ushort A { get => a; set { a = value; RaisePropertyChanged(nameof(A)); } }
        public uint E_T1 { get => e_T1; set { e_T1 = value; RaisePropertyChanged(nameof(E_T1)); RaisePropertyChanged(nameof(E_Tsum)); } }
        public uint E_T2 { get => e_T2; set { e_T2 = value; RaisePropertyChanged(nameof(E_T2)); RaisePropertyChanged(nameof(E_Tsum)); } }
        public uint E_T3 { get => e_T3; set { e_T3 = value; RaisePropertyChanged(nameof(E_T3)); RaisePropertyChanged(nameof(E_Tsum)); } }
        public uint E_Tsum { get => E_T1 + E_T2 + E_T3; }
        public uint E_T1_Start { get => e_T1_Start; set { e_T1_Start = value; RaisePropertyChanged(nameof(E_T1_Start)); RaisePropertyChanged(nameof(E_Tsum_Start)); } }
        public uint E_T2_Start { get => e_T2_Start; set { e_T2_Start = value; RaisePropertyChanged(nameof(E_T2_Start)); RaisePropertyChanged(nameof(E_Tsum_Start)); } }
        public uint E_T3_Start { get => e_T3_Start; set { e_T3_Start = value; RaisePropertyChanged(nameof(E_T3_Start)); RaisePropertyChanged(nameof(E_Tsum_Start)); } }
        public uint E_Tsum_Start { get => E_T1_Start + E_T2_Start + E_T3_Start; }
        public byte Perepoln { get => perepoln; set { perepoln = value; RaisePropertyChanged(nameof(Perepoln)); } }
        public byte T_qty { get => t_qty; set { t_qty = value; RaisePropertyChanged(nameof(T_qty)); } }
        public ushort T1_Time_1 { get => t1_Time_1; set { t1_Time_1 = value; RaisePropertyChanged(nameof(T1_Time_1)); } }
        public ushort T3_Time_1 { get => t3_Time_1; set { t3_Time_1 = value; RaisePropertyChanged(nameof(T3_Time_1)); } }
        public ushort T1_Time_2 { get => t1_Time_2; set { t1_Time_2 = value; RaisePropertyChanged(nameof(T1_Time_2)); } }
        public ushort T3_Time_2 { get => t3_Time_2; set { t3_Time_2 = value; RaisePropertyChanged(nameof(T3_Time_2)); } }
        public ushort T2_Time { get => t2_Time; set { t2_Time = value; RaisePropertyChanged(nameof(T2_Time)); } }
        public ushort Ascue_adrs { get => ascue_adrs; set { ascue_adrs = value; RaisePropertyChanged(nameof(Ascue_adrs)); } }
        public byte[] Ascue_pass { get => ascue_pass; set { ascue_pass = value; RaisePropertyChanged(nameof(Ascue_pass)); } }
        public byte Ascue_protocol { get => ascue_protocol; set { ascue_protocol = value; RaisePropertyChanged(nameof(Ascue_protocol)); } }
        public ushort Max_Power { get => max_Power; set { max_Power = value; RaisePropertyChanged(nameof(Max_Power)); } }

        public ImpParams(byte adrsPLC)
        {
            Adrs_PLC = adrsPLC;
            SetDefaultParams();
        }

        public void SetDefaultParams()
        {
            IsEnable = 0;
            A = 1600;
            E_T1 = 0;
            E_T2 = 0;
            E_T3 = 0;
            E_T1_Start = 0;
            E_T2_Start = 0;
            E_T3_Start = 0;
            Perepoln = 0;
            T_qty = 1;
            T1_Time_1 = 7 * 60;
            T3_Time_1 = 10 * 60;
            T1_Time_2 = 17 * 60;
            T3_Time_2 = 21 * 60;
            T2_Time = 23 * 60;
            Ascue_adrs = 0;
            Ascue_pass = new byte[6] { 1, 1, 1, 1, 1, 1 };
            Ascue_protocol = 0;
            Max_Power = 0;
        }
    }
}
