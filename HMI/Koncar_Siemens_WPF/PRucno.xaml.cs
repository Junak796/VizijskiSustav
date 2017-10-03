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
using HalconDotNet;
using System.Threading;

namespace VizijskiSustavWPF
{
    /// <summary>
    /// Interaction logic for PRucno.xaml
    /// </summary>
    public partial class PRucno : Page
    {
        private HDevelopExport HDevExp;

        float currentPosX = 0.0f, currentPosY = 0.0f, currentPosR = 0.0f;
      
        

      
        public PRucno()
        {

            
           
            InitializeComponent();

            HDevExp = new HDevelopExport();
            App.PLC.Update_100_ms += new PLCInterface.UpdateHandler(updatePagePRucno_100ms);
            App.PLC.Update_1_s += new PLCInterface.UpdateHandler(updatePagePRucno_1s);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            HDevExp.InitHalcon();
          

            Thread exportThread = new Thread(new ThreadStart(this.RunExport));
            exportThread.Start();
        }

       

        private void RunExport()
        {
            HTuple WindowID = hWindowControlWPF1.HalconID;
            HDevExp.RunHalcon(WindowID);

            this.Dispatcher.Invoke(new Action(() => {
             
            }));
        }

      

        private void updatePagePRucno_100ms(object sender, PLCInterfaceEventArgs e)
        {
            currentPosX = (float)e.StatusData.HorizontalnaOs.AktualnaPozicija.Value;
            currentPosY = (float)e.StatusData.VertikalnaOs.AktualnaPozicija.Value;
            currentPosR = (float)e.StatusData.RotacijskaOs.AktualnaPozicija.Value;
            

            Dispatcher.BeginInvoke((Action)(() =>
            {
               
                


               
                // Horizontalna os


              

            
                // Vertikalna os
                if ((bool)e.StatusData.VertikalnaOs.UPoziciji.Value)
                {
                    ell_yOsUPoziciji.Fill = new LinearGradientBrush(Colors.Green, Colors.White, 0.0);
                }
                else
                {
                    ell_yOsUPoziciji.Fill = new LinearGradientBrush((Color)ColorConverter.ConvertFromString("#FF979797"), Colors.White, 0.0);
                }

                if ((bool)e.StatusData.VertikalnaOs.Greska.Value)
                {
                    ell_yOsGreska.Fill = new LinearGradientBrush(Colors.Red, Colors.White, 0.0);
                }
                else
                {
                    ell_yOsGreska.Fill = new LinearGradientBrush((Color)ColorConverter.ConvertFromString("#FF979797"), Colors.White, 0.0);
                }

               

                // Rotaciona os
                if ((bool)e.StatusData.RotacijskaOs.UPoziciji.Value)
                {
                    ell_rOsUPoziciji.Fill = new LinearGradientBrush(Colors.Green, Colors.White, 0.0);
                }
                else
                {
                    ell_rOsUPoziciji.Fill = new LinearGradientBrush((Color)ColorConverter.ConvertFromString("#FF979797"), Colors.White, 0.0);
                }

                if ((bool)e.StatusData.RotacijskaOs.Greska.Value)
                {
                    ell_rOsGreska.Fill = new LinearGradientBrush(Colors.Red, Colors.White, 0.0);
                }
                else
                {
                    ell_rOsGreska.Fill = new LinearGradientBrush((Color)ColorConverter.ConvertFromString("#FF979797"), Colors.White, 0.0);
                }

                if ((bool)e.StatusData.RotacijskaOs.Referencirana.Value)
                {
                    ell_rOsReferencirana.Fill = new LinearGradientBrush(Colors.Red, Colors.White, 0.0);
                }
                else
                {
                    ell_rOsReferencirana.Fill = new LinearGradientBrush((Color)ColorConverter.ConvertFromString("#FF979797"), Colors.White, 0.0);
                }

            }
            ));

        
        }

        private void updatePagePRucno_1s(object sender, PLCInterfaceEventArgs e)
        {
         

            Dispatcher.BeginInvoke((Action)(() =>
            { 
            
             
            }
            ));
        }

       


      
























     

       

       

      

    }
}
