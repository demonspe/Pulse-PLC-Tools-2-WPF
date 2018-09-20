using LinkLibrary;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Pulse_PLC_Tools_2
{
    class ProtocolManager
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
            Protocol = new PulsePLCv2Protocol();
            Protocol.Message += mainVM.MessageInput;    //Обработчик сообщений (для лога)
            Protocol.CommandEnd += Protocol_CommandEnd; //Получение данных из ответов на команды
            Protocol.AccessEnd += Protocol_AccessEnd;
            this.context = context;
            LinkManager = mainVM.LinkManager;
            DeviceParams = mainVM.Device;
            Main_VM = mainVM;
        }

        private void Protocol_AccessEnd(object sender, EventArgs e)
        {
            //Доступ закрыт изменить значки в татус баре
            //
            //
        }

        private void Protocol_CommandEnd(object sender, ProtocolEventArgs e)
        {
            if (!e.Status || e.DataObject == null) return;
            //Получим данные и отправим их в наше View
            context.Post(GetProtocolData, e.DataObject);
        }

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
            }

            if(cmd == PulsePLCv2Protocol.Commands.Check_Pass)
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
                ImpParams impFrom = ((ImpParamsForProtocol)data).Imp;
                ImpParams impTo = ((ImpParamsForProtocol)data).Num == ImpNum.IMP1 ? Main_VM.Imp1 : Main_VM.Imp2;
                impTo.IsEnable = impFrom.IsEnable;
                if (impTo.IsEnable != 0)
                {
                    impTo.Adrs_PLC = impFrom.Adrs_PLC;
                    //Тип протокола
                    impTo.Ascue_protocol = impFrom.Ascue_protocol;
                    //Адрес аскуэ
                    impTo.Ascue_adrs = impFrom.Ascue_adrs;
                    //Пароль для аскуэ (6)
                    impTo.Ascue_pass[0] = impFrom.Ascue_pass[0];
                    impTo.Ascue_pass[1] = impFrom.Ascue_pass[1];
                    impTo.Ascue_pass[2] = impFrom.Ascue_pass[2];
                    impTo.Ascue_pass[3] = impFrom.Ascue_pass[3];
                    impTo.Ascue_pass[4] = impFrom.Ascue_pass[4];
                    impTo.Ascue_pass[5] = impFrom.Ascue_pass[5];
                    //Эмуляция переполнения
                    impTo.Perepoln = impFrom.Perepoln;
                    //Передаточное число
                    impTo.A = impFrom.A;
                    //Тарифы
                    impTo.T_qty = impFrom.T_qty;
                    impTo.T1_Time_1 = impFrom.T1_Time_1;
                    impTo.T3_Time_1 = impFrom.T3_Time_1;
                    impTo.T1_Time_2 = impFrom.T1_Time_2;
                    impTo.T3_Time_2 = impFrom.T3_Time_2;
                    impTo.T2_Time = impFrom.T2_Time;
                    //Показания - Текущие
                    impTo.E_T1 = impFrom.E_T1;
                    impTo.E_T2 = impFrom.E_T2;
                    impTo.E_T3 = impFrom.E_T3;
                    impTo.E_T1_Start = impFrom.E_T1_Start;
                    impTo.E_T2_Start = impFrom.E_T2_Start;
                    impTo.E_T3_Start = impFrom.E_T3_Start;
                    //Максимальная мощность
                    impTo.Max_Power = impFrom.Max_Power;
                }
            }
            if (cmd == PulsePLCv2Protocol.Commands.Read_DateTime)
            {

            }

            
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

        }
        public void Send_WriteAllParams()
        {

        }
        #endregion
        #region DateTime
        public void Send_ReadDateTime()
        {

        }
        public void Send_WriteDateTime()
        {

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

        }
        public void Send_ClearErrors()
        {

        }
        public void Send_WritePass()
        {

        }
        #endregion
        #region Imps params
        public void Send_ReadImp1()
        {

        }
        public void Send_WriteImp1()
        {

        }
        public void Send_ReadImp2()
        {

        }
        public void Send_WriteImp2()
        {

        }
        #endregion
    }
}
