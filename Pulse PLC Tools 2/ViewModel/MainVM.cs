using LinkLibrary;
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
using System.Windows.Documents;

namespace Pulse_PLC_Tools_2
{
    public enum TabPages : int { Link, DateTime, MainParams, Imp1, Imp2, TablePLC, J_Power, J_Config, J_Interface, J_PLCRequests, TableDataPLC, Monitor, About }

    public class MainVM : BindableBase
    {
        //Data
        ImpParams imp1, imp2;
        ImpExParams imp1Ex, imp2Ex;
        DeviceMainParams device;
        //Messages and Log
        private FlowDocument logContent;
        private FlowDocument logExContent;
        private Visibility logVisible;
        private Visibility logExVisible;
        private string toolBarText;
        //Navigate
        private int currentPage;
        
        public ObservableCollection<string> SerialNumList { get; }
        //Data
        public ImpParams Imp1 { get => imp1; set { imp1 = value; RaisePropertyChanged(nameof(Imp1)); } }
        public ImpParams Imp2 { get => imp2; set { imp2 = value; RaisePropertyChanged(nameof(Imp2)); } }
        public ImpExParams Imp1Ex { get => imp1Ex; set { imp1Ex = value; RaisePropertyChanged(nameof(Imp1Ex)); } }
        public ImpExParams Imp2Ex { get => imp2Ex; set { imp2Ex = value; RaisePropertyChanged(nameof(Imp2Ex)); } }
        public DeviceMainParams Device { get => device; set { device = value; RaisePropertyChanged(nameof(Device)); } }
        //Device events journals
        public ObservableCollection<DataGridRow_Log> JournalPower { get; }
        public ObservableCollection<DataGridRow_Log> JournalConfig { get; }
        public ObservableCollection<DataGridRow_Log> JournalInterfaces { get; }
        public ObservableCollection<DataGridRow_Log> JournalRequestsPLC { get; }
        //Messages and Log
        public FlowDocument Log { get => logContent; set { logContent = value; RaisePropertyChanged(nameof(Log)); } }
        public FlowDocument LogEx { get => logExContent; set { logExContent = value; RaisePropertyChanged(nameof(LogEx)); } }
        public Visibility LogVisible { get => logVisible; set { logVisible = value; RaisePropertyChanged(nameof(LogVisible)); } }
        public Visibility LogExVisible { get => logExVisible; set { logExVisible = value; RaisePropertyChanged(nameof(LogExVisible)); } }
        public string ToolBarText { get => toolBarText; set { toolBarText = value; RaisePropertyChanged(nameof(ToolBarText)); } }

        //Navigate
        public int CurrentPage { get => currentPage; set { currentPage = value; RaisePropertyChanged(nameof(CurrentPage)); } }

        //VM
        public LinkVM VM_Link { get; }
        public PLCTableVM VM_PLCTable { get; }

        //Model
        public LinkManager LinkManager { get; }
        public ProtocolManager ProtocolManager { get; }
        LogManager LogManager { get; }

        //------------------------------------------------------------------------------------
        //Commands
        //
        //HotKeys
        public DelegateCommand KeyDownEsc { get; private set; }
        //Log
        public DelegateCommand ShowLogSimple { get; private set; }
        public DelegateCommand ShowLogExpert { get; private set; }
        public DelegateCommand ClearLog { get; private set; }
        //Navigate
        public DelegateCommand<string> CommandGoToPage { get; private set; }
        //For link
        public DelegateCommand OpenLink { get; private set; }
        public DelegateCommand CloseLink { get; private set; }
        //Files
        public DelegateCommand SaveFile { get; private set; }
        public DelegateCommand OpenFile { get; private set; }
        //For protocol
        public DelegateCommand Send_SearchDevices { get; private set; }
        public DelegateCommand Send_ReadAllParams { get; private set; }
        public DelegateCommand Send_WriteAllParams { get; private set; }
        public DelegateCommand Send_ReadDateTime { get; private set; }   //Date and Time
        public DelegateCommand Send_WriteDateTime { get; private set; }
        public DelegateCommand Send_CorrectDateTime { get; private set; }
        public DelegateCommand Send_ReadMainParams { get; private set; } //Main Params
        public DelegateCommand Send_WriteMainParams { get; private set; }
        public DelegateCommand Send_ClearErrors { get; private set; }
        public DelegateCommand Send_WritePass { get; private set; }
        public DelegateCommand Send_ReadImp1 { get; private set; }   //Imp1
        public DelegateCommand Send_WriteImp1 { get; private set; }
        public DelegateCommand Send_ReadImp2 { get; private set; }   //Imp2
        public DelegateCommand Send_WriteImp2 { get; private set; }
        public DelegateCommand Send_ReadImp1Ex { get; private set; }
        public DelegateCommand Send_ReadImp2Ex { get; private set; }
        //PLC Table
        public DelegateCommand Send_ReadEnableRows { get; private set; }
        public DelegateCommand Send_ReadSelectedRows { get; private set; }
        public DelegateCommand Send_WriteSelectedRows { get; private set; }
        public DelegateCommand Send_Request_PLCv1 { get; private set; }
        public DelegateCommand Send_Request_Time { get; private set; }
        public DelegateCommand Send_Request_Serial { get; private set; }
        public DelegateCommand Send_Request_E_Current { get; private set; }
        public DelegateCommand Send_Request_E_StartDay { get; private set; }
        public DelegateCommand EnableSelected { get; private set; }
        public DelegateCommand DisableSelected { get; private set; }
        public DelegateCommand ClearPLCTable { get; private set; }
        //Data E Table
        public DelegateCommand Send_Read_E_Enabled { get; private set; }
        public DelegateCommand Send_Read_E_Selected { get; private set; }
        //Journals
        public DelegateCommand Send_ReadJournal_Interface { get; private set; }
        public DelegateCommand Send_ReadJournal_Config { get; private set; }
        public DelegateCommand Send_ReadJournal_Power { get; private set; }
        public DelegateCommand Send_ReadJournal_RequestsPLC { get; private set; }
        //----------------------------------------------------------------------------------

        public MainVM()
        {
            SerialNumList = new ObservableCollection<string>();

            //Контейнеры для данных
            Imp1 = new ImpParams(ImpNum.IMP1);
            Imp2 = new ImpParams(ImpNum.IMP2);
            Imp1Ex = new ImpExParams(ImpNum.IMP1);
            Imp2Ex = new ImpExParams(ImpNum.IMP2);
            Device = new DeviceMainParams();

            //Журналы событий
            JournalPower = new ObservableCollection<DataGridRow_Log>();
            JournalConfig = new ObservableCollection<DataGridRow_Log>();
            JournalInterfaces = new ObservableCollection<DataGridRow_Log>();
            JournalRequestsPLC = new ObservableCollection<DataGridRow_Log>();

            //Log
            Log = new FlowDocument();
            LogEx = new FlowDocument();
            LogVisible = Visibility.Visible;
            LogExVisible = Visibility.Hidden;
            LogManager = new LogManager(Log, LogEx, SynchronizationContext.Current);
            ToolBarText = "Привет";

            //VM
            VM_Link = new LinkVM();
            VM_PLCTable = new PLCTableVM();

            //Model
            LinkManager = new LinkManager(this, SynchronizationContext.Current);
            ProtocolManager = new ProtocolManager(this, SynchronizationContext.Current);

            //Commands
            InitCommands();

            //Start page
            GoToPage(TabPages.Link);
        }

        void InitCommands()
        {
            KeyDownEsc = new DelegateCommand(ProtocolManager.ClearCommandBuffer);

            CommandGoToPage = new DelegateCommand<string>(namePage => GoToPageFromXName(namePage));
            //Log
            ShowLogSimple = new DelegateCommand(() => { LogVisible = Visibility.Visible; LogExVisible = Visibility.Hidden; });
            ShowLogExpert = new DelegateCommand(() => { LogVisible = Visibility.Hidden; LogExVisible = Visibility.Visible; });
            ClearLog = new DelegateCommand(LogManager.ClearLog);
            //For link
            OpenLink = new DelegateCommand(LinkManager.OpenLink);
            CloseLink = new DelegateCommand(LinkManager.CloseLink);
            //For files
            SaveFile = new DelegateCommand(() => { FileConfigManager.SaveConfig(new PulsePLCv2Config() { Imp1 = this.Imp1, Imp2 = this.Imp2, Device = this.Device, TablePLC = VM_PLCTable.TablePLC.ToList() }); });
            OpenFile = new DelegateCommand(() => {
                PulsePLCv2Config config = FileConfigManager.LoadConfig();
                if(config != null)
                {
                    Imp1 = config.Imp1;
                    Imp2 = config.Imp2;
                    Device = config.Device;
                    VM_PLCTable.TablePLC.Clear();
                    for (int i = 0; i < 250; i++) VM_PLCTable.TablePLC.Add(config.TablePLC[i]);
                    MessageInput(this, new MessageDataEventArgs() { MessageString = "Файл конфигурации успешно загружен", MessageType = MessageType.Normal });
                    MessageInput(this, new MessageDataEventArgs() { MessageString = "Файл конфигурации успешно загружен", MessageType = MessageType.ToolBarInfo });
                }
                else
                    MessageInput(this, new MessageDataEventArgs() { MessageString = "Не удалось загрузить файл конфигурации", MessageType = MessageType.Error });
            });
            //For protocol
            //Common
            Send_SearchDevices = new DelegateCommand(ProtocolManager.Send_SearchDevices);
            Send_ReadAllParams = new DelegateCommand(ProtocolManager.Send_ReadAllParams);
            Send_WriteAllParams = new DelegateCommand(()=> ProtocolManager.Send_WriteAllParams(Device, Imp1, Imp2));
            //DateTime
            Send_ReadDateTime = new DelegateCommand(ProtocolManager.Send_ReadDateTime);
            Send_WriteDateTime = new DelegateCommand(ProtocolManager.Send_WriteDateTime);
            Send_CorrectDateTime = new DelegateCommand(ProtocolManager.Send_CorrectDateTime);
            //Main params
            Send_ReadMainParams = new DelegateCommand(ProtocolManager.Send_ReadMainParams);
            Send_WriteMainParams = new DelegateCommand(() => ProtocolManager.Send_WriteMainParams(Device));
            Send_ClearErrors = new DelegateCommand(ProtocolManager.Send_ClearErrors);
            Send_WritePass = new DelegateCommand(() => ProtocolManager.Send_WritePass(Device));
            //Imps params
            Send_ReadImp1 = new DelegateCommand(() => ProtocolManager.Send_ReadImp(ImpNum.IMP1));
            Send_WriteImp1 = new DelegateCommand(() => ProtocolManager.Send_WriteImp(Imp1));
            Send_ReadImp2 = new DelegateCommand(() => ProtocolManager.Send_ReadImp(ImpNum.IMP2));
            Send_WriteImp2 = new DelegateCommand(() => ProtocolManager.Send_WriteImp(Imp2));
            Send_ReadImp1Ex = new DelegateCommand(() => ProtocolManager.Send_ReadImpEx(ImpNum.IMP1));
            Send_ReadImp2Ex = new DelegateCommand(() => ProtocolManager.Send_ReadImpEx(ImpNum.IMP2));
            //PLC Table
            Send_ReadEnableRows = new DelegateCommand(ProtocolManager.Send_ReadEnableRowsAdrss);
            Send_ReadSelectedRows = new DelegateCommand(ProtocolManager.Send_ReadSelectedRows);
            Send_WriteSelectedRows = new DelegateCommand(() => ProtocolManager.Send_WriteSelectedRows(VM_PLCTable.SelectedRows));
            Send_Request_PLCv1 = new DelegateCommand(() => { ProtocolManager.Send_RequestPLC(VM_PLCTable.SelectedRows, PLC_Request.PLCv1); });
            Send_Request_Time = new DelegateCommand(() => { ProtocolManager.Send_RequestPLC(VM_PLCTable.SelectedRows, PLC_Request.Time_Synchro); });
            Send_Request_Serial = new DelegateCommand(() => { ProtocolManager.Send_RequestPLC(VM_PLCTable.SelectedRows, PLC_Request.Serial_Num); });
            Send_Request_E_Current = new DelegateCommand(() => { ProtocolManager.Send_RequestPLC(VM_PLCTable.SelectedRows, PLC_Request.E_Current); });
            Send_Request_E_StartDay = new DelegateCommand(() => { ProtocolManager.Send_RequestPLC(VM_PLCTable.SelectedRows, PLC_Request.E_Start_Day); });
            EnableSelected = new DelegateCommand(VM_PLCTable.EnableSelected);
            DisableSelected = new DelegateCommand(VM_PLCTable.DisableSelected);
            ClearPLCTable = new DelegateCommand(VM_PLCTable.ResetTable);
            //Data E Table
            Send_Read_E_Enabled = new DelegateCommand(() => ProtocolManager.Send_Read_E_Enabled(VM_PLCTable.TablePLC.ToList()));
            Send_Read_E_Selected = new DelegateCommand(()=>ProtocolManager.Send_Read_E_Selected(VM_PLCTable.SelectedRows));
            //Journals
            Send_ReadJournal_Interface = new DelegateCommand(() => ProtocolManager.Send_ReadJournal(Journal_type.INTERFACES));
            Send_ReadJournal_Config = new DelegateCommand(() => ProtocolManager.Send_ReadJournal(Journal_type.CONFIG));
            Send_ReadJournal_Power = new DelegateCommand(() => ProtocolManager.Send_ReadJournal(Journal_type.POWER));
            Send_ReadJournal_RequestsPLC = new DelegateCommand(() => ProtocolManager.Send_ReadJournal(Journal_type.REQUESTS));
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
        
        //Обработчик события сообщения
        public void MessageInput(object sender, MessageDataEventArgs e)
        {
            if (e.MessageType == MessageType.ToolBarInfo) { ToolBarText = e.MessageString; return; }
            if (e.MessageType == MessageType.MsgBox) { MessageBox.Show(e.MessageString); return; }
            if (e.MessageType == MessageType.SendBytes || e.MessageType == MessageType.ReceiveBytes) { LogManager.Add_Line_Bytes(e.Data, e.Length, e.MessageType, LinkManager.Link.ConnectionString); return; }
            LogManager.Add_Line_String(e.MessageString, e.MessageType);
        }

        //Канал связи был подключен
        public void Link_Connected(object sender, EventArgs e)
        {
            ProtocolManager.Link_Connected();
            VM_Link.IsConnected = true;
        }
        //Канал связи был отключен
        public void Link_Disconnected(object sender, EventArgs e)
        {
            VM_Link.IsConnected = false;
        }
        
    }
}
