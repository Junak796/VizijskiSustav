using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace VizijskiSustav
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        
        public static PLCInterface PLC;
        public static string ReportPath = "reports/ControlSheet.xlsx";
        
        public static MainWindow mwHandle;
    
      
        public App()
        {
            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.High;
            InitializeComponent();
           
            PLC = new PLCInterface();
            App.PLC.StartCyclic();
            App.PLC.Update_100_ms += new PLCInterface.UpdateHandler(PLC_Update_100_ms);

        }

        private void PLC_Update_100_ms(PLCInterface sender, PLCInterfaceEventArgs e)
        {
        }

      

      

    }
}
