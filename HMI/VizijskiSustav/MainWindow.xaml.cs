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

namespace VizijskiSustav
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            App.PLC.Update_1_s += new PLCInterface.UpdateHandler(updatePage);
            App.PLC.Update_100_ms += new PLCInterface.UpdateHandler(updatePage_100);
        }


        private void updatePage(object sender, PLCInterfaceEventArgs e)
        {
           
            Dispatcher.BeginInvoke(new Action(() =>
            {
               
            })) ;

        }

        private void updatePage_100(object sender, PLCInterfaceEventArgs e)
        {

            Dispatcher.BeginInvoke(new Action(() =>
            {
                //if (((bool)e.StatusData.Dimension.AutomaticActive.Value == true) && (b_ponoviMjerenje.Content != "STOP"))
                //{
                //    b_ponoviMjerenje.Foreground = Brushes.Red;
                //    b_ponoviMjerenje.Content = "STOP";
                //}
                //else if (((bool)e.StatusData.Dimension.AutomaticActive.Value == false) && (b_ponoviMjerenje.Foreground != Brushes.Black))
                //{
                //    b_ponoviMjerenje.Content = "PONOVI MJERENJE\n    AUTOMATSKI";
                //    b_ponoviMjerenje.Foreground = Brushes.Black;
                //}
            }));

        }

        private void Button_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            App.PLC.WriteTag(App.PLC.CONTROL.Test.StartVerticalUp, true);
            App.PLC.WriteTag(App.PLC.CONTROL.Test.StartVerticalDown, true);
            App.PLC.WriteTag(App.PLC.CONTROL.Test.StartHorizontalUp, true);
            App.PLC.WriteTag(App.PLC.CONTROL.Test.StartHorizontalDown, true);
        }

        private void Button_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            App.PLC.WriteTag(App.PLC.CONTROL.Test.StartVerticalUp, false);
            App.PLC.WriteTag(App.PLC.CONTROL.Test.StartVerticalDown, false);
            App.PLC.WriteTag(App.PLC.CONTROL.Test.StartHorizontalUp, false);
            App.PLC.WriteTag(App.PLC.CONTROL.Test.StartHorizontalDown, false);
        }
    }
}
