using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Pulse_PLC_Tools_2
{
    public class PLCTableVM : BindableBase
    {
        int countSelectedRows;

        public ObservableCollection<DataGridRow_PLC> TablePLC { get; }
        public List<DataGridRow_PLC> SelectedRows { get; }
        public int CountSelectedRows { get => countSelectedRows; set { countSelectedRows = value; RaisePropertyChanged(nameof(CountSelectedRows)); } }

        //Events
        public DelegateCommand<IList> SelectionChanged { get; }

        public PLCTableVM()
        {
            SelectedRows = new List<DataGridRow_PLC>();
            TablePLC = new ObservableCollection<DataGridRow_PLC>();
            ResetTable();

            //Обновление списка выделенных строк таблицы
            SelectionChanged = new DelegateCommand<IList>((items) => 
            {
                var selectedItems = items.Cast<DataGridRow_PLC>();

                SelectedRows.Clear();
                foreach (var item in selectedItems)
                {
                    SelectedRows.Add(item);
                }
                CountSelectedRows = SelectedRows.Count;
            });
        }

        public void EnableSelected() { SelectedRows.ForEach(r => r.IsEnable = true); }
        public void DisableSelected() { SelectedRows.ForEach(r => r.IsEnable = false); }
        public void ResetTable()
        {
            TablePLC.Clear();
            for (int i = 0; i < 250; i++)
            {
                TablePLC.Add(new DataGridRow_PLC() { Adrs_PLC = (byte)(i + 1), Protocol_ASCUE = ImpAscueProtocolType.Mercury230ART });
            }
                
        }
    }
}
