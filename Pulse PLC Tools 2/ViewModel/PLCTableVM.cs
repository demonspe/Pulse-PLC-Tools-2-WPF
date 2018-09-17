using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse_PLC_Tools_2
{
    public class PLCTableVM : BindableBase
    {
        private ObservableCollection<DataGridRow_PLC> tablePLC;
        public ObservableCollection<DataGridRow_PLC> TablePLC { get => tablePLC; set { tablePLC = value; RaisePropertyChanged(nameof(TablePLC)); } }
        

        public PLCTableVM()
        {
            TablePLC = new ObservableCollection<DataGridRow_PLC>();
            FillTable();
        }

        void FillTable()
        {
            for (int i = 0; i < 250; i++)
            {
                DataGridRow_PLC row = new DataGridRow_PLC((byte)(i + 1), ImpAscueProtocolType.Mercury230ART);
                TablePLC.Add(row);
            }
        }
    }
}
