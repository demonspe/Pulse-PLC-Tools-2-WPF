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
        MainVM mainVM;

        public ProtocolManager(MainVM mainVM)
        {
            this.mainVM = mainVM;
        }
        #region Common
        public void Send_SearchDevices()
        {

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
