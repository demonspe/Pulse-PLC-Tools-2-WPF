using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinkLibrary
{
    public static class CRC16
    {
        const ushort polynom_default = 0xA001;
        static ushort[] table = new ushort[256];

        public static ushort ComputeChecksum(byte[] bytes, int length, ushort polynom)
        {
            GatTable(polynom);

            ushort crc = 0xFFFF;
            for (int i = 0; i < length; ++i)
            {
                byte index = (byte)(crc ^ bytes[i]);
                crc = (ushort)((crc >> 8) ^ table[index]);
            }

            return crc;
        }

        public static ushort ComputeChecksum(byte[] bytes, int length)
        {
            return ComputeChecksum(bytes, length, polynom_default);
        }

        public static byte[] ComputeChecksumBytes(byte[] bytes, int length, ushort polynom)
        {
            ushort crc = ComputeChecksum(bytes, length, polynom);
            return BitConverter.GetBytes(crc);
        }

        public static byte[] ComputeChecksumBytes(byte[] bytes, int length)
        {
            ushort crc = ComputeChecksum(bytes, length);
            return BitConverter.GetBytes(crc);
        }

        static void GatTable(ushort polynom)
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
                        value = (ushort)((value >> 1) ^ polynom);
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
