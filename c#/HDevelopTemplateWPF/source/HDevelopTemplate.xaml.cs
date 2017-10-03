using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using HalconDotNet;

namespace HDevelopTemplateWPF
{
  /// <summary>
  /// Interaction logic for Window1.xaml
  /// </summary>
  public partial class HDevelopTemplate : Window
  {
		private HDevelopExport HDevExp;

    public HDevelopTemplate()
    {
      InitializeComponent();

			HDevExp = new HDevelopExport();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
 			HDevExp.InitHalcon();
    }

    private void buttonRun_Click(object sender, RoutedEventArgs e)
    {
      HTuple WindowID = hWindowControlWPF1.HalconID;
			labelStatus.Content = "Running...";
			labelStatus.UpdateLayout();
			HDevExp.RunHalcon(WindowID);
			labelStatus.Content = "Finished.";
    }
  }
}
