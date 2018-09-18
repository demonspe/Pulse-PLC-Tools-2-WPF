using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Pulse_PLC_Tools_2
{
    public enum TabPages : int { Link, DateTime, MainParams, Imp1, Imp2, TablePLC, J_Power, J_Config, J_Interface, J_PLCRequests, TableDataPLC, Monitor, About }

    public class MainVM : BindableBase
    {
        //Data
        public ImpParams Imp1 { get; set; }
        public ImpParams Imp2 { get; set; }
        public DeviceMainParams Device { get; set; }
        public ObservableCollection<DataGridRow_PLC> TablePLC { get; }
        //Device events journals
        public ObservableCollection<DataGridRow_Log> JournalPower { get; }
        public ObservableCollection<DataGridRow_Log> JournalConfig { get; }
        public ObservableCollection<DataGridRow_Log> JournalInterfaces { get; }
        public ObservableCollection<DataGridRow_Log> JournalRequestsPLC { get; }

        //Access data
        private string serialNum;
        public string SerialNum { get => serialNum; set { serialNum = value; RaisePropertyChanged(nameof(SerialNum)); } }
        private string pass;
        public string Pass { get => pass; set { pass = value; RaisePropertyChanged(nameof(Pass)); } }

        //Navigate
        private int currentPage;
        public int CurrentPage { get => currentPage; set { currentPage = value; RaisePropertyChanged(nameof(CurrentPage)); } }

        //VM
        public LinkVM VM_Link { get; }
        public DateTimeVM VM_DateTime { get; }

        //Model
        LinkManager LinkManager;
        ProtocolManager ProtocolManager;
        //Commands
        //Navigate
        public DelegateCommand<string> CommandGoToPage { get; }
        //For link
        public DelegateCommand OpenLink { get; }
        public DelegateCommand CloseLink { get; }
        //Files
        public DelegateCommand SaveFile { get; }
        public DelegateCommand OpenFile { get; }
        //For protocol
        public DelegateCommand Send_SearchDevices { get; }
        public DelegateCommand Send_ReadAllParams { get; }
        public DelegateCommand Send_WriteAllParams { get; }
        public DelegateCommand Send_ReadDateTime { get; }   //Date and Time
        public DelegateCommand Send_WriteDateTime { get; }
        public DelegateCommand Send_CorrectDateTime { get; }
        public DelegateCommand Send_ReadMainParams { get; } //Main Params
        public DelegateCommand Send_WriteMainParams { get; }
        public DelegateCommand Send_ClearErrors { get; }
        public DelegateCommand Send_WritePass { get; }
        public DelegateCommand Send_ReadImp1 { get; }
        public DelegateCommand Send_WriteImp1 { get; }
        public DelegateCommand Send_ReadImp2 { get; }
        public DelegateCommand Send_WriteImp2 { get; }

        public MainVM()
        {
            //Контейнеры для данных
            Imp1 = new ImpParams(1);
            Imp2 = new ImpParams(2);
            Device = new DeviceMainParams();
            TablePLC = new ObservableCollection<DataGridRow_PLC>();
            FillTablePLC();
            //Журналы событий
            JournalPower = new ObservableCollection<DataGridRow_Log>();
            JournalConfig = new ObservableCollection<DataGridRow_Log>();
            JournalInterfaces = new ObservableCollection<DataGridRow_Log>();
            JournalRequestsPLC = new ObservableCollection<DataGridRow_Log>();

            //VM
            VM_Link = new LinkVM();
            VM_DateTime = new DateTimeVM();
            
            //Model
            LinkManager = new LinkManager(VM_Link, SynchronizationContext.Current);
            ProtocolManager = new ProtocolManager(this);
            //
            GoToPage(TabPages.Link);
            
            //Commands
            CommandGoToPage = new DelegateCommand<string>(namePage => GoToPageFromXName(namePage));
            //For link
            OpenLink = new DelegateCommand(LinkManager.OpenLink);
            CloseLink = new DelegateCommand(LinkManager.CloseLink);
            //For files
            SaveFile = new DelegateCommand(() => { MessageBox.Show(nameof(SaveFile)); });
            OpenFile = new DelegateCommand(() => { MessageBox.Show(nameof(OpenFile)); });
            //For protocol
            //Common
            Send_SearchDevices = new DelegateCommand(ProtocolManager.Send_SearchDevices);
            Send_ReadAllParams = new DelegateCommand(ProtocolManager.Send_ReadAllParams);
            Send_WriteAllParams = new DelegateCommand(ProtocolManager.Send_WriteAllParams);
            //DateTime
            Send_ReadDateTime = new DelegateCommand(ProtocolManager.Send_ReadDateTime);
            Send_WriteDateTime = new DelegateCommand(ProtocolManager.Send_WriteDateTime);
            Send_CorrectDateTime = new DelegateCommand(ProtocolManager.Send_CorrectDateTime);
            //Main params
            Send_ReadMainParams = new DelegateCommand(ProtocolManager.Send_ReadMainParams);
            Send_WriteMainParams = new DelegateCommand(ProtocolManager.Send_WriteMainParams);
            Send_ClearErrors = new DelegateCommand(ProtocolManager.Send_ClearErrors);
            Send_WritePass = new DelegateCommand(ProtocolManager.Send_WritePass);
            //Imps params
            Send_ReadImp1 = new DelegateCommand(ProtocolManager.Send_ReadImp1);
            Send_WriteImp1 = new DelegateCommand(ProtocolManager.Send_WriteImp1);
            Send_ReadImp2 = new DelegateCommand(ProtocolManager.Send_ReadImp2);
            Send_WriteImp2 = new DelegateCommand(ProtocolManager.Send_WriteImp2);

        }

        void GoToPageFromXName(string xNameOfPage)
        {
            if (xNameOfPage.Length < 2) return;
            int numPage;
            if (int.TryParse(xNameOfPage.Substring(xNameOfPage.Length - 2, 2), out numPage))
                GoToPage((TabPages)numPage);
        }
        void GoToPage(TabPages page)
        {
            CurrentPage = (int)page;
        }

        void FillTablePLC()
        {
            for (int i = 0; i < 250; i++)
                TablePLC.Add(new DataGridRow_PLC((byte)(i + 1), ImpAscueProtocolType.Mercury230ART));
        }
    }
}
