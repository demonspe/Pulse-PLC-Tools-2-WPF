using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Pulse_PLC_Tools_2
{
    /// <summary>
    /// Interaction logic for PageService.xaml
    /// </summary>
    public partial class PageService : UserControl
    {
        public PageService()
        {
            InitializeComponent();


            //Вкл/Выкл тестовый режим PLC передатчика
            ComboBoxTestModeEnabled.Items.Add("Выкл");
            ComboBoxTestModeEnabled.Items.Add("Включить (после перезагрузки выключится)");
            ComboBoxTestModeEnabled.SelectedIndex = 0;

            //Выбор частоты
            for (int i = 8; i <= 28; i++)
            {
                string text = (int)(3906.25 * i) + " Гц (индекс " + i + ")";
                if(i == 16) text += " 'логический 0'";
                if(i == 17) text += " 'начало кадра'";
                if(i == 18) text += " 'логическая 1'";
                ComboBoxFreqs.Items.Add(text);
            }
            ComboBoxFreqs.SelectedIndex = 18 - 8;

            //Выбор делителя частоты
            ComboBoxFreqDivs.Items.Add(1);
            ComboBoxFreqDivs.Items.Add(2);
            ComboBoxFreqDivs.Items.Add(3);
            ComboBoxFreqDivs.Items.Add(4);
            ComboBoxFreqDivs.Items.Add(5);
            ComboBoxFreqDivs.Items.Add(6);
            ComboBoxFreqDivs.Items.Add(7);
            ComboBoxFreqDivs.Items.Add(8);
            ComboBoxFreqDivs.Items.Add(9);
            ComboBoxFreqDivs.Items.Add(10);
            ComboBoxFreqDivs.Items.Add(11);
            ComboBoxFreqDivs.Items.Add(12);
            ComboBoxFreqDivs.Items.Add(13 + " - Максимальная амплитуда (не стабильная)");
            ComboBoxFreqDivs.Items.Add(14);
            ComboBoxFreqDivs.Items.Add(15 + " - Рабочая амплитуда (стабильная)");
            ComboBoxFreqDivs.Items.Add(16);
            ComboBoxFreqDivs.Items.Add(17);
            ComboBoxFreqDivs.Items.Add(18);
            ComboBoxFreqDivs.Items.Add(19);
            ComboBoxFreqDivs.Items.Add(20);
            ComboBoxFreqDivs.Items.Add(21);
            ComboBoxFreqDivs.Items.Add(22);
            ComboBoxFreqDivs.SelectedIndex = 14;
        }
    }
}
