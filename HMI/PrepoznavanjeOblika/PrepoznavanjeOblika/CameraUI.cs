using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;

using GigEVisionSDK_NET;
using System.IO;
using System.Windows.Media.Imaging;


namespace PrepoznavanjeOblika
{

    public partial class CameraUI : UserControl
    {
        const bool admin_mode = false;

        private ImageGrabber IG;
        private EmguCVProcessing CV;
        private Calibration C;

        private Thread ImageProcThread;
        private static Object imageProcessingTypeLock = new Object();
        private bool signalStopImageProc = false;

        public delegate void DataReadyHandler(CameraUI sender, CameraUIEventArgs e);
        public event DataReadyHandler DataReady;

        public delegate void CameraOnlineHandler(CameraUI sender, CameraOnlineEventArgs e);
        public event CameraOnlineHandler OnCameraOnlineChanged;

        // Property WPF uses to turn image processing on or off.
        private bool cameraActive = false;
        public bool CameraActive
        {
            get { return cameraActive; }
            set
            {
                if (value != cameraActive)
                {
                    cameraActive = value;
                    OnCameraActiveChanged(EventArgs.Empty);
                }
            }
        }

        private bool useWebCamera = true;
        public bool UseWebCamera
        {
            get { return useWebCamera; }
            set { useWebCamera = value; }
        }

        public event EventHandler CameraActiveChanged;

        // Da bi se moglo nasljediti klasu i implementirati funkciju drugačije
        protected virtual void OnCameraActiveChanged(EventArgs e)
        {
            if (CameraActiveChanged != null)
            {
                CameraActiveChanged(this, e);
            }
        }

        // Timer za periodično pokušavanje spajanja na kameru
        System.Timers.Timer tim_TryConnecting;

        // Called from timer when trying to connect to camera
        private void TryConnecting(object sender, EventArgs e)
        {
            bool last_cameraConnected = cameraConnecetd;
            if (!cameraConnecetd)
            {
                // Try to connect
                cameraConnecetd = IG.ConnectToCamera();
            }
            else
            {
                // Check if still connected
                cameraConnecetd = IG.isCameraConnected();
            }
            
            // Signal that camera online status has changed
            if (OnCameraOnlineChanged != null && (cameraConnecetd != last_cameraConnected || first_run))
            {
                first_run = false;
                OnCameraOnlineChanged(this, new CameraOnlineEventArgs(cameraConnecetd));
            }

            tim_TryConnecting.Start();
        }

        private CameraOutputType cameraOutput;
        public CameraOutputType CameraOutput
        {
            get { return cameraOutput; }
            set { cameraOutput = value; }
        }

        private bool cameraConnecetd = false;
        private bool first_run = true;

        // Constructor
        public CameraUI()
        {
            InitializeComponent();

            if (!admin_mode)
            {
                // Sakrij tipke
                b_startCamera.Visible = false;
                button1.Visible = false;
                trackBar_cannyMax.Visible = false;
                trackBar_CannyMin.Visible = false;
                tb_cannyMax.Visible = false;
                tb_cannyMin.Visible = false;
                rtb_test.Visible = false;
            }

            IG = new ImageGrabber(pb_processed);
            CV = new EmguCVProcessing();

            // Za admin_mode *************************************************
            trackBar_cannyMax.Value = CV.thr2;
            trackBar_CannyMin.Value = CV.thr1;

            tb_cannyMax.Text = CV.thr2.ToString();
            tb_cannyMin.Text = CV.thr1.ToString();
            // ***************************************************************

            // Setting real world positions of calibration pattern (4 circles) in milimetres.
            // Points MUST be ordered counterclockwise starting from first quadrant.
            System.Drawing.PointF[] WorldPositionOfCalibCircles = new PointF[4];
            WorldPositionOfCalibCircles[0] = new PointF(10.001f, 10.000f);
            WorldPositionOfCalibCircles[1] = new PointF(-10.001f, 10.004f);
            WorldPositionOfCalibCircles[2] = new PointF(-10.001f, -10.001f);
            WorldPositionOfCalibCircles[3] = new PointF(10.000f, -10.001f);

            // Konstruktor pohranjuje zadane točke i učitava kalibracijsku matricu iz calibration.dat.
            // Ako datoteka ne postoji matrica se postavlja na jediničnu.
            C = new Calibration(WorldPositionOfCalibCircles);
            
            System.Drawing.PointF[] sortedPixelQuad = new PointF[4];
            sortedPixelQuad[0] = new PointF(1735.0f, 493.0f);
            sortedPixelQuad[1] = new PointF(665.0f, 492.5f);
            sortedPixelQuad[2] = new PointF(664.5f, 1562.0f);
            sortedPixelQuad[3] = new PointF(1734.0f, 1562.5f);

            CV.CalibData = C.CalculateCalibrationData(sortedPixelQuad);
            C.WriteCalibDataToFile("calibration.dat");

            //CV.CalibData = C.CalibData; // CV radi pixel to world pretvorbu pa mora imati kalibracijske podatke

            // test
            //C.ResetCalibrationMatrix();

            pb_processed.SizeMode = PictureBoxSizeMode.Zoom;

            // Start timer that periodicaly checks camera connection status and connects to camera if possible.
            tim_TryConnecting = new System.Timers.Timer(1000);
            tim_TryConnecting.AutoReset = false;
            tim_TryConnecting.Elapsed += TryConnecting;
            tim_TryConnecting.Start();
            
            // Registriraj event handler koji pokreće / zaustavlja obradu slike (promjenom propertyja CameraActive)
            CameraActiveChanged += delegate(object sender, EventArgs e)
            {
                if (cameraActive)
                {
                    // Start image processing (wait until camera is connected)
                    RunImageProcThread();
                }
                else
                {
                    // Stop image processing
                    StopImageProcThread();
                }
            };
        }

        private void test(object sender, string s)
        {
            try
            {
                pb_processed.Invoke(new MethodInvoker(() => rtb_test.Text = s));
            }
            catch (Exception)
            {
                ImageProcThread.Abort();
            }
        }

        private void ImageProcMethod()
        {
            // Mjerenje trajanja izvođenja (samo u admin modu)
            Stopwatch stopWatch = new Stopwatch();

            // Za kalibraciju
            List<PointF[]> calibCirclesCoords = new List<PointF[]>();

            while (true)
            {
                if (admin_mode)
                {
                    stopWatch.Reset();
                    stopWatch.Start();
                }

                if (!useWebCamera)
                {
                    // Čekaj da se kamera spoji
                    while (!IG.isCameraConnected())
                    {
                        if (signalStopImageProc || useWebCamera) break;
                        try
                        {
                            pb_processed.BeginInvoke(new MethodInvoker(() => pb_processed.Image = null));
                        }
                        catch (Exception)
                        {
                            ImageProcThread.Abort();
                        }
                        Thread.Sleep(100);
                    }

                    // Izađi iz petlje ako je došao signal za prekid threada
                    if (signalStopImageProc) break;
                    else if (!useWebCamera)
                    {
                        IG.GrabImage(); // Dohvati sliku (GigE vision SDK)

                        if (IG.mat != null)
                        {
                            CV.currentFrame = IG.mat;   // Daj CV-u pointer na učitanu sliku
                            
                            // Standard feature detection (One angle, one circle or one line)

                            CV.Process();

                            // Rezultati obrade su u CV.cameraOutput i CV.objectFound,
                            // a slika s označenim objektima je u CV.frameToShow
                            cameraOutput = CV.cameraOutput;

                            if (DataReady != null) // Ako je handler funkcija registrirana
                            {
                                CameraUIEventArgs args = new CameraUIEventArgs(cameraOutput, CV.objectFound);
                                DataReady(this, args);
                            }

                            // Prikaz slike na UI threadu s označenim pronađenim objektima
                            try
                            {
                                pb_processed.BeginInvoke(new MethodInvoker(() => pb_processed.Image = new Bitmap(CV.frameToShow.Bitmap)));
                            }
                            catch (Exception)
                            {
                                ImageProcThread.Abort();
                            }

                            if (admin_mode)
                            {
                                stopWatch.Stop();

                                // Ispis rezultata procesiranja slike
                                String str = "";
                                str += "Trajanje: " + stopWatch.Elapsed.Milliseconds.ToString() + " ms\n";
                                if (CV.cameraOutput.TYPE == 0) str += "Nema objekata";
                                else if (CV.cameraOutput.TYPE == objectType.ANGLE) str += String.Format("Kut: ({0:0.0}, {1:0.0}), {2:0.0} deg", CV.cameraOutput.POINT1.X, CV.cameraOutput.POINT1.Y, CV.cameraOutput.PARAMETER);
                                else if (CV.cameraOutput.TYPE == objectType.CIRCLE) str += String.Format("Krug: ({0:0.00}, {1:0.00}), r = {2:0.0}", CV.cameraOutput.POINT1.X, CV.cameraOutput.POINT1.Y, CV.cameraOutput.PARAMETER);
                                else if (CV.cameraOutput.TYPE == objectType.LINE) str += String.Format("Linija: ({0:0.0}, {1:0.0}), ({2:0.0}, {3:0.0}), {4:0.0} pix", CV.cameraOutput.POINT1.X, CV.cameraOutput.POINT1.Y, CV.cameraOutput.POINT2.X, CV.cameraOutput.POINT2.Y, CV.cameraOutput.PARAMETER);
                                test(this, str);
                            }

                        }//IG.mat != null
                    }// !useWebCamera
                }//!useWebCamera
                else
                {
                    // Prikaz slike s web camere na UI threadu
                    try
                    {
                        IG.mat = CV.GetImageFromWebcam();
                        Emgu.CV.CvInvoke.Resize(IG.mat, IG.mat, new Size(244, 205));
                        Emgu.CV.CvInvoke.Flip(IG.mat, IG.mat, Emgu.CV.CvEnum.FlipType.Vertical);
                        Emgu.CV.CvInvoke.Flip(IG.mat, IG.mat, Emgu.CV.CvEnum.FlipType.Horizontal);
                        pb_processed.BeginInvoke(new MethodInvoker(() => pb_processed.Image = new Bitmap(IG.mat.Bitmap)));
                    }
                    catch (Exception)
                    {
                        ImageProcThread.Abort();
                    }
                }
                Thread.Sleep(100); // was 20
            }

            // Stopping thread
            try
            {
                CV.CloseWebcam();
                //IG.Disconnect();
                pb_processed.BeginInvoke(new MethodInvoker(() => pb_processed.Image = null));
            }
            catch (Exception)
            {
                ImageProcThread.Abort();
            }
        }

        private void RunImageProcThread()
        {
            if (ImageProcThread != null) if (ImageProcThread.IsAlive) return;
            signalStopImageProc = false;
            ImageProcThread = new Thread(ImageProcMethod);
            ImageProcThread.Start();
        }

        private void StopImageProcThread()
        {
            signalStopImageProc = true;
        }

        // Start camera
        private void button2_Click(object sender, EventArgs e)
        {
            RunImageProcThread();
        }

        private void button1_Click_2(object sender, EventArgs e)
        {
            // Kalibracijska matrica se pohranjuje u datoteku "calibration.dat" i 
            // automatski učitava pri svakom pokretanju aplikacije.

            // Pozicije središta krugova na kalibracijskoj matrici u mm
            System.Drawing.PointF[] sortedPixelQuad = new PointF[4];
            sortedPixelQuad[0] = new PointF(1735.0f, 493.0f);
            sortedPixelQuad[1] = new PointF(665.0f, 492.5f);
            sortedPixelQuad[2] = new PointF(664.5f, 1562.0f);
            sortedPixelQuad[3] = new PointF(1734.0f, 1562.5f);

            CV.CalibData = C.CalculateCalibrationData(sortedPixelQuad);
            C.WriteCalibDataToFile("calibration.dat");
        }

        private void pb_processed_Click(object sender, EventArgs e)
        {

        }

        private void trackBar_CannyMin_Scroll(object sender, EventArgs e)
        {
            CV.thr1 = trackBar_CannyMin.Value;
            tb_cannyMin.Text = CV.thr1.ToString();
        }

        private void trackBar_cannyMax_Scroll(object sender, EventArgs e)
        {
            CV.thr2 = trackBar_cannyMax.Value;
            tb_cannyMax.Text = CV.thr2.ToString();
        }

    }

    public class CameraUIEventArgs : EventArgs
    {
        private CameraOutputType cameraOutput;
        public CameraOutputType CameraOutput
        {
            get { return this.cameraOutput; }
            set { this.cameraOutput = value; }
        }

        private CameraOutputForAutoTracking objectFound = new CameraOutputForAutoTracking();
        public CameraOutputForAutoTracking ObjectFound
        {
            get { return this.objectFound; }
            set { this.objectFound = value; }
        }

        public CameraUIEventArgs(CameraOutputType _cameraOutput, CameraOutputForAutoTracking _objectsFound)
        {
            cameraOutput = _cameraOutput;
            objectFound = _objectsFound;
        }
    }

    public class CameraOnlineEventArgs : EventArgs
    {
        private bool cameraOnline;
        public bool CameraOnline
        {
            get { return cameraOnline; }
            set { cameraOnline = value; }
        }

        public CameraOnlineEventArgs(bool _cameraOnline)
        {
            cameraOnline = _cameraOnline;
        }
    }

}
