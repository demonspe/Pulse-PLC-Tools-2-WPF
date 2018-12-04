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
        private int countSelectedRows;
        //Отображение столбцов
        private bool isCheckedPLC;
        private bool isCheckedASU;
        private bool isCheckedStatus;

        //CheckBoxes
        public bool IsCheckedPLC { get => isCheckedPLC; set { isCheckedPLC = value; RaisePropertyChanged(nameof(IsCheckedPLC)); RaisePropertyChanged(nameof(IsVisiblePLC)); } }
        public bool IsCheckedASU { get => isCheckedASU; set { isCheckedASU = value; RaisePropertyChanged(nameof(IsCheckedASU)); RaisePropertyChanged(nameof(IsVisibleASU)); } }
        public bool IsCheckedStatus { get => isCheckedStatus; set { isCheckedStatus = value; RaisePropertyChanged(nameof(IsCheckedStatus)); RaisePropertyChanged(nameof(IsVisibleStatus)); } }
        //Colums Visibility props
        public Visibility IsVisiblePLC { get => isCheckedPLC ? Visibility.Visible: Visibility.Hidden; }
        public Visibility IsVisibleASU { get => isCheckedASU ? Visibility.Visible : Visibility.Hidden; }
        public Visibility IsVisibleStatus { get => isCheckedStatus ? Visibility.Visible : Visibility.Hidden; }
        //Data
        public ObservableCollection<DataGridRow_PLC> TablePLC { get; }
        //Selected
        public List<DataGridRow_PLC> SelectedRows { get; }
        public int CountSelectedRows { get => countSelectedRows; set { countSelectedRows = value; RaisePropertyChanged(nameof(CountSelectedRows)); } }
        //Events
        public DelegateCommand<IList> SelectionChanged { get; }

        public PLCTableVM()
        {
            SelectedRows = new List<DataGridRow_PLC>();
            TablePLC = new ObservableCollection<DataGridRow_PLC>();
            ResetTable();

            IsCheckedPLC = true;
            IsCheckedASU = false;
            IsCheckedStatus = false;

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
