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
    /// Interaction logic for MyTimePicker.xaml
    /// </summary>
    public partial class MyTimePicker : UserControl
    {
        
        public int Hours {
            get { return (int)GetValue(HoursProperty); }
            set { SetValue(HoursProperty, value); }
        }
        public int Minutes { get; set; }

        public static readonly DependencyProperty HoursProperty =
            DependencyProperty.Register("Hours", typeof(DateTime), typeof(MyTimePicker));

        public MyTimePicker()
        {
            InitializeComponent();
        }
    }
}
