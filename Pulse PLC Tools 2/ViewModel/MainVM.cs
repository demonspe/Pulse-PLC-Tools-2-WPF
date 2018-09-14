﻿using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Pulse_PLC_Tools_2
{
    public enum TabPages : int { Link, DateTime, MainParams, Imp1, Imp2, TablePLC, J_Power, J_Config, J_Interface, J_PLCRequests, TableDataPLC, Monitor, About }

    public class MainVM : BindableBase
    {
        public ImpParams Imp1 { get; set; }
        public ImpParams Imp2 { get; set; }
        public DeviceMainParams Device { get; set; }

        private string serialNum;
        public string SerialNum { get => serialNum; set { serialNum = value; RaisePropertyChanged(nameof(SerialNum)); } }
        private string pass;
        public string Pass { get => pass; set { pass = value; RaisePropertyChanged(nameof(Pass)); } }

        private int currentPage;
        public int CurrentPage { get => currentPage; set { currentPage = value; RaisePropertyChanged(nameof(CurrentPage)); } }

        //VM
        public LinkVM VM_Link { get; }
        public DateTimeVM VM_DateTime { get; }

        //Commands
        public DelegateCommand<string> CommandGoToPage { get; }

        public MainVM()
        {
            //Контейнеры для данных
            Imp1 = new ImpParams(1);
            Imp2 = new ImpParams(2);
            Device = new DeviceMainParams();
            //VM
            VM_Link = new LinkVM();
            VM_DateTime = new DateTimeVM();

            GoToPage(TabPages.Link);

            //Commands
            CommandGoToPage = new DelegateCommand<string>(
                 nameItem => {
                     if (nameItem.Length < 2) return;
                     int numPage;
                     if(int.TryParse(nameItem.Substring(nameItem.Length - 2, 2), out numPage))
                        GoToPage((TabPages)numPage);
                 });

            
        }

        void GoToPage(TabPages page)
        {
            CurrentPage = (int)page;
        }
    }
}
