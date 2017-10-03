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

namespace VizijskiSustavWPF
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class BitLamp : UserControl
    {

        public static readonly DependencyProperty pLCTag = DependencyProperty.Register("PLCTag", typeof(plcTag), typeof(BitLamp), new PropertyMetadata());
        public plcTag PLCTag
        {
            get { return (plcTag)GetValue(pLCTag); }
            set { SetValue(pLCTag, value); }
        }

        public static readonly DependencyProperty pLCConnection = DependencyProperty.Register("PLCConnection", typeof(PLCInterface), typeof(BitLamp), new PropertyMetadata(null, new PropertyChangedCallback(OnPLCAssign)));

        public PLCInterface PLCConnection
        {
            get { return (PLCInterface)GetValue(pLCConnection); }
            set { SetValue(pLCConnection, value); }
        } 

        public static readonly DependencyProperty text = DependencyProperty.Register("Text", typeof(string), typeof(BitLamp), new PropertyMetadata());
        public string Text
        {
            get { return (string)GetValue(text); }
            set { SetValue(text, value); }
        }

        public static readonly DependencyProperty onColor = DependencyProperty.Register("OnColor", typeof(Brush), typeof(BitLamp), new PropertyMetadata());
        public Brush OnColor
        {
            get { return (Brush)GetValue(onColor); }
            set { SetValue(onColor, value); }
        }

        public static readonly DependencyProperty offColor = DependencyProperty.Register("OffColor", typeof(Brush), typeof(BitLamp), new PropertyMetadata());
        public Brush OffColor
        {
            get { return (Brush)GetValue(offColor); }
            set { SetValue(offColor, value); }
        }

        public BitLamp()
        {
            InitializeComponent();
           
        }

        public void Connect()
        {
            PLCConnection.Update_100_ms += new PLCInterface.UpdateHandler(updatePage123);
            PLCConnection.Update_1_s += new PLCInterface.UpdateHandler(updatePage1234);
        }

        private void updatePage123(object sender, PLCInterfaceEventArgs e)
        {

            Dispatcher.BeginInvoke((Action)(() =>
            {
                try
                { 
                    PLCTag.GetValueFromGroupBuffer(e.CyclicStatusBuffer);
                    if ((bool)PLCTag.Value)
                    {
                        label123.Background = OnColor;
                    }
                    else
                    {
                        label123.Background = OffColor;
                    }
                }
                catch
                {
                }
            }
            ));
        }

        private void updatePage1234(object sender, PLCInterfaceEventArgs e)
        {
        }

        private static void OnPLCAssign(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BitLamp promjenaKontrola = (BitLamp)d;
            promjenaKontrola.Connect();
        }
    }
}
