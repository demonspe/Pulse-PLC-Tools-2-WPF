using LinkLibrary;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse_PLC_Tools_2
{
    class ProtocolManager
    {
        CommandBuffer CommandManager;
        PulsePLCv2Protocol Protocol;
        LinkManager LinkManager;
        DeviceMainParams DeviceParams;

        public ProtocolManager(LinkManager linkManager, MainVM mainVM, EventHandler<MessageDataEventArgs> MessageInputHandler)
        {
            CommandManager = new CommandBuffer();
            Protocol = new PulsePLCv2Protocol();
            Protocol.Message += MessageInputHandler;
            LinkManager = linkManager;
            DeviceParams = mainVM.Device;
        }

        PulsePLCv2LoginPass GetLoginPass()
        {
            byte[] login = DeviceParams.Serial;
            byte[] pass = Encoding.Default.GetBytes(DeviceParams.PassCurrent);
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
