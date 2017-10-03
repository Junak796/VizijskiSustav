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
    public partial class BitButton : UserControl
    {
      
        public static readonly DependencyProperty pLCTag = DependencyProperty.Register("PLCTag", typeof(plcTag), typeof(BitButton), new PropertyMetadata());
        public plcTag PLCTag
        {
            get { return (plcTag)GetValue(pLCTag); }
            set { SetValue(pLCTag, value); }
        }


        public static readonly DependencyProperty pLCConnection = DependencyProperty.Register("PLCConnection", typeof(PLCInterface), typeof(BitButton), new PropertyMetadata());

        public PLCInterface PLCConnection
        {
            get { return (PLCInterface)GetValue(pLCConnection); }
            set { SetValue(pLCConnection, value); }
        }

        public static readonly DependencyProperty text = DependencyProperty.Register("Text", typeof(string), typeof(BitButton), new PropertyMetadata());
        public string Text
        {
            get { return (string)GetValue(text); }
            set { SetValue(text, value); }
        }

        


        public BitButton()
        {
            InitializeComponent();
        }

       

        private void Button_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            PLCConnection.WriteTag(PLCTag, true);
           
        }

        private void Button_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            PLCConnection.WriteTag(PLCTag, false);
        }

        
    }
}
