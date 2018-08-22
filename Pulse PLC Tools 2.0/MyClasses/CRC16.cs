using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pulse_PLC_Tools_2._0
{
    public class CRC16
    {
        const ushort polynomial = 0xA001;
        ushort[] table = new ushort[256];
        public ushort ComputeChecksum(byte[] bytes, int length)
        {
            ushort crc = 0xFFFF;
            for (int i = 0; i < length; ++i)
            {
                byte index = (byte)(crc ^ bytes[i]);
                crc = (ushort)((crc >> 8) ^ table[index]);
            }
            return crc;
        }
        public byte[] ComputeChecksumBytes(byte[] bytes, int length)
        {
            ushort crc = ComputeChecksum(bytes, length);
            return BitConverter.GetBytes(crc);
        }
        public CRC16()
        {
            ushort value;
            ushort temp;
            for (ushort i = 0; i < table.Length; ++i)
            {
                value = 0;
                temp = i;
                for (byte j = 0; j < 8; ++j)
                {
                    if (((value ^ temp) & 0x0001) != 0)
                    {
                        value = (ushort)((value >> 1) ^ polynomial);
                    }
                    else
                    {
                        value >>= 1;
                    }
                    temp >>= 1;
                }
                table[i] = value;
            }
        }
    }
}
