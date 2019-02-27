using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse_PLC_Tools_2
{
    //Класс для представления данных в таблице Журнал событий (для всех 4х)
    public class DataGridRow_Event
    {
        public string Num { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string Name { get; set; }
    }
}
