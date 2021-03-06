﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Windows;


namespace VizijskiSustavWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static PIzvjestaji pIzvjestaji;
        public static PPostavke pPostavke;
        public static PDimenzije pDimenzije; 
        public static PSrh pSrh;
        public static PValovitost pValovitost;
        public static PSablja pSablja;
        public static PKut pKut;
        public static PRucno pRucno;
        public static PLCInterface PLC;
        public static string  ReportPath = "reports/ControlSheet.xlsx";
        //public static CameraUI CamUI;
        public static MainWindow mwHandle;
        public static reportInterface MainReportInterface;
        public static Algoritmi AutoSearch = new Algoritmi();

      

        
        public App()
        {
            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.High;
            InitializeComponent();
            MainReportInterface = ((reportInterface)Application.Current.FindResource("MainReport"));
            //CamUI = new CameraUI();
            PLC = ((PLCInterface)Application.Current.FindResource("PLCinterf"));
            pIzvjestaji = new PIzvjestaji();
            pPostavke = new PPostavke();
            pDimenzije = new PDimenzije();
            pSrh = new PSrh();
            pValovitost = new PValovitost();
            pSablja = new PSablja();
            pKut = new PKut();
            pRucno = new PRucno();
            pRucno = new PRucno();

            App.PLC.StartCyclic();
            App.PLC.Update_Online_Flag += new PLCInterface.OnlineMarker(PLCInterface_PLCOnlineChanged);
            App.PLC.Update_100_ms += new PLCInterface.UpdateHandler(PLC_Update_100_ms);

            //App.CamUI.DataReady += new PrepoznavanjeOblika.CameraUI.DataReadyHandler(CamUI_DataReady);
            //App.CamUI.OnCameraOnlineChanged += new PrepoznavanjeOblika.CameraUI.CameraOnlineHandler(CamUI_CameraOnlineChanged);

          

            
        }

        private void PLC_Update_100_ms(PLCInterface sender, PLCInterfaceEventArgs e)
        {
            String msg = "";

            // Prioritet poruka (najviši prioritet je na dnu)
            //if ((bool)e.StatusData.RotacijskaOs.AutomaticActive.Value) msg = "MJERENJE SRHA U TIJEKU";
            //if ((bool)e.StatusData.VertikalnaOs.AutomaticActive.Value) msg = "MJERENJE VALOVITOSTI U TIJEKU";
            //if ((bool)e.StatusData.Ticalo.AutomaticActive.Value) msg = "MJERENJE DIMENZIJA U TIJEKU";
            //if ((bool)e.StatusData.HorizontalnaOs.ReferencedX.Value == false) msg = "OS X NIJE REFERENCIRANA";
            //if ((bool)e.StatusData.HorizontalnaOs.ReferencedY.Value == false) msg = "OS Y NIJE REFERENCIRANA";
            //if ((bool)e.StatusData.HorizontalnaOs.FaultX.Value == true) msg = "GREŠKA OSI X";
            //if ((bool)e.StatusData.HorizontalnaOs.FaultY.Value == true) msg = "GREŠKA OSI Y";
            if (mwHandle!=null)
            mwHandle.tb_statusMessage.Dispatcher.BeginInvoke((Action)(() => { mwHandle.tb_statusMessage.Text = msg; }));
        }

        

      
        // Event handler koji se poziva kad PLC postane online ili offline (Ethernet kabel se spoji ili odspoji).
        private void PLCInterface_PLCOnlineChanged(object sender, OnlineMarkerEventArgs e)
        {
            if (e.OnlineMark)
            {
                mwHandle.onlineFlag.Dispatcher.BeginInvoke((Action)(() => {mwHandle.onlineFlag.Fill = new LinearGradientBrush(Colors.Green, Colors.White, 0.0); }));
                mwHandle.t_connectionStatus.Dispatcher.BeginInvoke((Action)(() => { mwHandle.t_connectionStatus.Text = "PLC Status: Online"; }));
            }
            else
            {
                mwHandle.onlineFlag.Dispatcher.BeginInvoke((Action)(() => { mwHandle.onlineFlag.Fill = new LinearGradientBrush((Color)ColorConverter.ConvertFromString("#FF979797"), Colors.White, 0.0); }));
                mwHandle.t_connectionStatus.Dispatcher.BeginInvoke((Action)(() => { mwHandle.t_connectionStatus.Text = "PLC Status: Offline"; }));
            }
        }

    }
}
