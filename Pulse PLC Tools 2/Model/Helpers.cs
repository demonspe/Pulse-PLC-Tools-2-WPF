using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse_PLC_Tools_2
{
    public static class MyHelper
    {
        public static byte[] Add(this byte[] bytesArray, byte data)
        {
            Array.Resize(ref bytesArray, bytesArray.Length + 1);
            bytesArray[bytesArray.Length - 1] = data;
            return bytesArray;
        }

        public static List<List<T>> Split<T>(this List<T> source, int groupSize)
        {
            List<List<T>> tmp = new List<List<T>>();
            while (source.Count > groupSize)
            {
                tmp.Add(source.Take(groupSize).ToList());
                source = source.Skip(groupSize).ToList();
            }
            tmp.Add(source.Take(groupSize).ToList());
            return tmp;
        }

        public static uint ToUint32(this byte[] bytes, bool fromLowToHigth)
        {
            if (bytes.Length < 4) throw new Exception("В массиве меньше 4х элементов. Невозможно выполнить преобразование.");
            if(fromLowToHigth)
                return ((uint)bytes[3] << 24) + ((uint)bytes[2] << 16) + ((uint)bytes[1] << 8) + bytes[0];
            else
                return ((uint)bytes[0] << 24) + ((uint)bytes[1] << 16) + ((uint)bytes[2] << 8) + bytes[3];
        }
        public static ushort ToUint16(this byte[] bytes, bool fromLowToHigth)
        {
            if (bytes.Length < 2) throw new Exception("В массиве меньше 2х элементов. Невозможно выполнить преобразование.");
            if (fromLowToHigth)
                return (ushort)((bytes[1] << 8) + bytes[0]);
            else
                return (ushort)((bytes[0] << 8) + bytes[1]);
        }
    }
}
