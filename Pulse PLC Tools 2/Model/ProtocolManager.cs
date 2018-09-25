using LinkLibrary;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Pulse_PLC_Tools_2
{
    public class ProtocolManager
    {
        CommandBuffer CommandManager;
        PulsePLCv2Protocol Protocol;
        //View
        SynchronizationContext context;
        LinkManager LinkManager;
        DeviceMainParams DeviceParams;
        MainVM Main_VM;

        public ProtocolManager(MainVM mainVM, SynchronizationContext context)
        {
            CommandManager = new CommandBuffer();
            CommandManager.Message += mainVM.MessageInput;
            Protocol = new PulsePLCv2Protocol();
            Protocol.Message += mainVM.MessageInput;    //Обработчик сообщений (для лога)
            Protocol.CommandEnd += Protocol_CommandEnd; //Получение данных из ответов на команды
            Protocol.AccessEnd += Protocol_AccessEnd;
            this.context = context;
            LinkManager = mainVM.LinkManager;
            DeviceParams = mainVM.Device;
            Main_VM = mainVM;
        }
        //Канал связи был открыт
        public void Link_Connected()
        {
            Send_SearchDevices();
        }

        private void Protocol_AccessEnd(object sender, EventArgs e)
        {
            //Доступ закрыт изменить значки в cтатус баре
            //
            //
        }

        private void Protocol_CommandEnd(object sender, ProtocolEventArgs e)
        {
            if (!e.Status || e.DataObject == null) return;
            //Получим данные и отправим их в наше View
            context.Post(GetProtocolData, e.DataObject);
        }

        PulsePLCv2LoginPass GetLoginPass()
        {
            byte[] login = DeviceParams.Serial;

            byte[] pass = DeviceParams.PassCurrent;
            
            return new PulsePLCv2LoginPass(login, pass);
        }

        #region Common
        public void Send_SearchDevices()
        {
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Search_Devices, null, 0);
        }
        public void Send_ReadAllParams()
        {
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Check_Pass, GetLoginPass(), 0);
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Read_DateTime, null, 0);
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Read_Main_Params, null, 0);
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Read_IMP, ImpNum.IMP1, 0);
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Read_IMP, ImpNum.IMP2, 0);
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Close_Session, null, 0);
        }
        public void Send_WriteAllParams()
        {
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Check_Pass, GetLoginPass(), 0);
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Write_DateTime, DateTime.Now, 0);
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Write_Main_Params, DeviceParams, 0);
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Write_IMP, new ImpParamsForProtocol(Main_VM.Imp1, ImpNum.IMP1), 0);
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Write_IMP, new ImpParamsForProtocol(Main_VM.Imp2, ImpNum.IMP2), 0);
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Close_Session, null, 0);
        }
        #endregion
        #region DateTime
        public void Send_ReadDateTime()
        {
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Check_Pass, GetLoginPass(), 0);
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Read_DateTime, null, 0);
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Close_Session, null, 0);
        }
        public void Send_WriteDateTime()
        {
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Check_Pass, GetLoginPass(), 0);
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Write_DateTime, DateTime.Now, 0);
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Close_Session, null, 0);
        }
        public void Send_CorrectDateTime()
        {

        }
        #endregion
        #region MainParams
        public void Send_ReadMainParams()
        {
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Check_Pass, GetLoginPass(), 0);
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Read_Main_Params, null, 0);
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Close_Session, null, 0);
        }
        public void Send_WriteMainParams()
        {
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Check_Pass, GetLoginPass(), 0);
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Write_Main_Params, DeviceParams, 0);
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Close_Session, null, 0);
        }
        public void Send_ClearErrors()
        {
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Check_Pass, GetLoginPass(), 0);
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Clear_Errors, null, 0);
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Close_Session, null, 0);
        }
        public void Send_WritePass()
        {
            string p_w = DeviceParams.PassWrite_View, p_w_hex = "\nhex: ";
            for (int i = 0; i < 6; i++) { if (i < p_w.Length) p_w_hex += "0x" + Convert.ToByte(p_w[i]).ToString("X") + " "; else p_w_hex += "0xFF "; }
            p_w += " (" + p_w.Length + " симв)";
            if (!DeviceParams.NewPassWrite) { p_w = "Без изменений"; p_w_hex = ""; }
            string p_r = DeviceParams.PassRead_View, p_r_hex = "\nhex: ";
            for (int i = 0; i < 6; i++) { if (i < p_r.Length) p_r_hex += "0x" + Convert.ToByte(p_r[i]).ToString("X") + " "; else p_r_hex += "0xFF "; }
            p_r += " (" + p_r.Length + " симв)";
            if (!DeviceParams.NewPassRead) { p_r = "Без изменений"; p_r_hex = ""; }

            if (MessageBox.Show("Записать новые пароли?\n\nПароль на запись: " + p_w + "" + p_w_hex +
                "\n\nПароль на чтение: " + p_r + p_r_hex, "Запись новых паролей", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Check_Pass, GetLoginPass(), 0);
                CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Pass_Write, DeviceParams, 0);
                CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Close_Session, null, 0);
            }
        }
        #endregion
        #region Imps params
        public void Send_ReadImp1()
        {
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Check_Pass, GetLoginPass(), 0);
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Read_IMP, ImpNum.IMP1, 0);
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Close_Session, null, 0);
        }
        public void Send_WriteImp1()
        {
            ImpParamsForProtocol param = new ImpParamsForProtocol(Main_VM.Imp1, ImpNum.IMP1);
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Check_Pass, GetLoginPass(), 0);
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Write_IMP, param, 0);
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Close_Session, null, 0);
        }
        public void Send_ReadImp2()
        {
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Check_Pass, GetLoginPass(), 0);
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Read_IMP, ImpNum.IMP2, 0);
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Close_Session, null, 0);
        }
        public void Send_WriteImp2()
        {
            ImpParamsForProtocol param = new ImpParamsForProtocol(Main_VM.Imp2, ImpNum.IMP2);
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Check_Pass, GetLoginPass(), 0);
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Write_IMP, param, 0);
            CommandManager.Add_CMD(LinkManager.Link, Protocol, PulsePLCv2Protocol.Commands.Close_Session, null, 0);

        }
        #endregion

        void GetProtocolData(object DataObject)
        {
            ProtocolDataContainer dataContainer = (ProtocolDataContainer)DataObject;
            object data = dataContainer.Data;
            if (dataContainer.ProtocolName != "PulsePLCv2" || data == null) return;
            PulsePLCv2Protocol.Commands cmd = (PulsePLCv2Protocol.Commands)dataContainer.CommandCode;

            if (cmd == PulsePLCv2Protocol.Commands.Search_Devices)
            {
                Main_VM.SerialNumList.Clear();
                ((List<string>)data).ForEach(str => Main_VM.SerialNumList.Add(str));
                if (Main_VM.SerialNumList.Count > 0 && Main_VM.Device.Serial_View == string.Empty) Main_VM.Device.Serial_View = Main_VM.SerialNumList[0];
            }

            if (cmd == PulsePLCv2Protocol.Commands.Check_Pass)
            {
                AccessType access = (AccessType)data;
                //Отобразить во View
                //
            }
            if (cmd == PulsePLCv2Protocol.Commands.Read_Main_Params)
            {
                DeviceMainParams device = (DeviceMainParams)data;
                //Версии
                DeviceParams.VersionFirmware = device.VersionFirmware;
                DeviceParams.VersionEEPROM = device.VersionEEPROM;
                //Режимы работы
                DeviceParams.WorkMode = device.WorkMode;
                DeviceParams.BatteryMode = device.BatteryMode;
                DeviceParams.RS485_WorkMode = device.RS485_WorkMode;
                DeviceParams.Bluetooth_WorkMode = device.Bluetooth_WorkMode;
                //Ошибки
                DeviceParams.ErrorsByte = device.ErrorsByte;
                //Покрасим пункт меню в зеленый
                //
            }
            if (cmd == PulsePLCv2Protocol.Commands.Read_IMP)
            {
                if (((ImpParamsForProtocol)data).Num == ImpNum.IMP1)
                    Main_VM.Imp1 = ((ImpParamsForProtocol)data).Imp;
                else
                    Main_VM.Imp2 = ((ImpParamsForProtocol)data).Imp;
            }
            if (cmd == PulsePLCv2Protocol.Commands.Read_DateTime)
            {
                DeviceParams.DeviceDateTime = (DateTime)data;
            }
            if(cmd == PulsePLCv2Protocol.Commands.Read_IMP_extra)
            {
                ImpExParamsForProtocol exParams = (ImpExParamsForProtocol)data;

            }

        }
    }
}
