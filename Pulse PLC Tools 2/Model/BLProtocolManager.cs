using LinkLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse_PLC_Tools_2
{
    class BLProtocolManager
    {
        CommandBuffer CMD_Buffer { get; }
        IProtocol Protocol { get; }

        public BLProtocolManager()
        {
            CMD_Buffer = new CommandBuffer();
            //Protocol = new PulsePLCv2Protocol();
        }
    }
}
