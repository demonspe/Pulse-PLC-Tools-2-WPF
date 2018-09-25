using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse_PLC_Tools_2
{
    public static class BytesArrayHelper
    {
        public static byte[] Add(this byte[] bytesArray, byte data)
        {
            Array.Resize(ref bytesArray, bytesArray.Length + 1);
            bytesArray[bytesArray.Length - 1] = data;
            return bytesArray;
        }
    }
}
