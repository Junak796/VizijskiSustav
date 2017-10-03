using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using System.Windows.Media.Imaging;

using Emgu.CV;
using Emgu.CV.WPF;

using GigEVisionSDK_NET;

namespace PrepoznavanjeOblika
{
    public class GrabberEventArgs : EventArgs
    {
        private Bitmap bitmap;
        public Bitmap Bitmap
        {
            get { return this.bitmap; }
            set { this.bitmap = value; }
        }
    }
    
    class ImageGrabber
    {
        public delegate void FinishedHandler(ImageGrabber sender, GrabberEventArgs e);
        public event FinishedHandler Finished;
        
        Bitmap bitmap;
        public Bitmap Bitmap
        {
            get { return this.bitmap; }
            set { this.bitmap = value; }
        }

        public BitmapSource bs;
        public Mat mat = null;
        gige.IGigEVisionAPI gigeVisionApi;

        Rectangle m_rect;
        PixelFormat m_pixelFormat;
        UInt32 m_pixelType;

        gige.IDevice m_device;
        gige.IImageProcAPI m_imageProcApi;

        gige.IAlgorithm m_gammaAlg;
        gige.IParams m_gammaParams;
        gige.IResults m_gammaResults;
        gige.IImageBitmap m_gammaBitmap;

        gige.IAlgorithm m_demosaicAlg;
        gige.IParams m_demosaicParams;
        gige.IResults m_demosaicResults;
        gige.IImageBitmap m_demosaicBitmap;

        gige.IAlgorithm m_whiteBalanceAlg;
        gige.IParams m_whiteBalanceParams;
        gige.IResults m_whiteBalanceResults;

        gige.IAlgorithm m_averagePixelAlg;
        gige.IParams m_averagePixelParams;
        gige.IResults m_averagePixelResults;

        gige.IAlgorithm m_adjustGainAlg;
        gige.IParams m_adjustGainParams;
        gige.IResults m_adjustGainResults;
        gige.IImageBitmap m_adjustGainBitmap;

        gige.IAlgorithm m_histogramAlg;
        gige.IResults m_histogramResults;

        gige.IAlgorithm m_sharpenAlg;
        gige.IParams m_sharpenParams;
        gige.IResults m_sharpenResults;
        gige.IImageBitmap m_sharpenBitmap;

        gige.IAlgorithm m_lutAlg;
        gige.IParams m_lutParams;
        gige.IResults m_lutResults;
        gige.IImageBitmap m_lutBitmap;

        gige.IAlgorithm m_colorGimpAlg;
        gige.IParams m_colorGimpParams;
        gige.IResults m_colorGimpResults;
        gige.IImageBitmap m_colorGimpBitmap;

        gige.IAlgorithm m_matrixAlg;
        gige.IParams m_matrixParams;
        gige.IResults m_matrixResults;
        gige.IImageBitmap m_matrixBitmap;

        gige.IAlgorithm m_changeBitDepthAlg;
        gige.IParams m_changeBitDepthParams;
        gige.IResults m_changeBitDepthResults;
        gige.IImageBitmap m_changeBitDepthBitmap;

        gige.IAlgorithm m_flipRotateAlg;
        gige.IParams m_flipRotateParams;
        gige.IResults m_flipRotateResults;
        gige.IImageBitmap m_flipRotateBitmap;

        double[,] m_matrix3x3RGB = new double[3, 3];
        Int64[] m_8bitRedHistogram = new Int64[256];
        Int64[] m_8bitGreenHistogram = new Int64[256];
        Int64[] m_8bitBlueHistogram = new Int64[256];

        public ImageGrabber(PictureBox pb)
        {
            //mat = new Mat();
            bitmap = (Bitmap)pb.BackgroundImage;
            createDevice();
        }

        private void createDevice()
        {
            // initialize GigEVision API
            //gige.GigEVisionSDK.InitGigEVisionAPI();
            //gigeVisionApi = gige.GigEVisionSDK.GetGigEVisionAPI();
            //if (!gigeVisionApi.IsUsingKernelDriver())
            //{
            //    //Text = Text + " (Warning: Smartek Filter Driver not loaded.)";
            //}

            //// initialize ImageProcessing API
            //gige.GigEVisionSDK.InitImageProcAPI();
            //m_imageProcApi = gige.GigEVisionSDK.GetImageProcAPI();

            //m_gammaAlg = m_imageProcApi.GetAlgorithmByName("Gamma");
            //m_gammaAlg.CreateParams(ref m_gammaParams);
            //m_gammaAlg.CreateResults(ref m_gammaResults);
            //m_imageProcApi.CreateBitmap(ref m_gammaBitmap);

            //m_demosaicAlg = m_imageProcApi.GetAlgorithmByName("DemosaicBilinear");
            ////m_demosaicAlg = m_imageProcApi.GetAlgorithmByName("DemosaicHQLinear");
            ////m_demosaicAlg = m_imageProcApi.GetAlgorithmByName("DemosaicPixelGroup");
            ////m_demosaicAlg = m_imageProcApi.GetAlgorithmByName("DemosaicColorized");
            //m_demosaicAlg.CreateParams(ref m_demosaicParams);
            //m_demosaicAlg.CreateResults(ref m_demosaicResults);
            //m_imageProcApi.CreateBitmap(ref m_demosaicBitmap);

            //m_whiteBalanceAlg = m_imageProcApi.GetAlgorithmByName("WhiteBalance");
            //m_whiteBalanceAlg.CreateParams(ref m_whiteBalanceParams);
            //m_whiteBalanceAlg.CreateResults(ref m_whiteBalanceResults);

            //m_averagePixelAlg = m_imageProcApi.GetAlgorithmByName("Average");
            //m_averagePixelAlg.CreateParams(ref m_averagePixelParams);
            //m_averagePixelAlg.CreateResults(ref m_averagePixelResults);

            //m_adjustGainAlg = m_imageProcApi.GetAlgorithmByName("AdjustRGBGain");
            //m_adjustGainAlg.CreateParams(ref m_adjustGainParams);
            //m_adjustGainAlg.CreateResults(ref m_adjustGainResults);
            //m_imageProcApi.CreateBitmap(ref m_adjustGainBitmap);

            ////m_histogramAlg = m_imageProcApi.GetAlgorithmByName("Histogram");
            ////m_histogramAlg.CreateResults(ref m_histogramResults);

            ////m_sharpenAlg = m_imageProcApi.GetAlgorithmByName("Sharpen");
            ////m_sharpenAlg.CreateParams(ref m_sharpenParams);
            ////m_sharpenAlg.CreateResults(ref m_sharpenResults);
            ////m_imageProcApi.CreateBitmap(ref m_sharpenBitmap);

            ////m_lutAlg = m_imageProcApi.GetAlgorithmByName("LUTTable");
            ////m_lutAlg.CreateParams(ref m_lutParams);
            ////m_lutAlg.CreateResults(ref m_lutResults);
            ////m_imageProcApi.CreateBitmap(ref m_lutBitmap);

            ////m_lutParams.SetStringNodeValue("LUTSelector", "Red");
            ////for (int i = 0; i < 256; i++)
            ////{
            ////    m_lutParams.SetIntegerNodeValue("LUTIndex", i);
            ////    m_lutParams.SetIntegerNodeValue("LUTValue", 255 - i);
            ////}
            ////m_lutParams.SetStringNodeValue("LUTSelector", "Green");
            ////for (int i = 0; i < 256; i++)
            ////{
            ////    m_lutParams.SetIntegerNodeValue("LUTIndex", i);
            ////    m_lutParams.SetIntegerNodeValue("LUTValue", 255 - i);
            ////}
            ////m_lutParams.SetStringNodeValue("LUTSelector", "Blue");
            ////for (int i = 0; i < 256; i++)
            ////{
            ////    m_lutParams.SetIntegerNodeValue("LUTIndex", i);
            ////    m_lutParams.SetIntegerNodeValue("LUTValue", 255 - i);
            ////}

            ////m_colorGimpAlg = m_imageProcApi.GetAlgorithmByName("ColorGimp");
            ////m_colorGimpAlg.CreateParams(ref m_colorGimpParams);
            ////m_colorGimpAlg.CreateResults(ref m_colorGimpResults);
            ////m_imageProcApi.CreateBitmap(ref m_colorGimpBitmap);

            ////m_colorGimpParams.SetFloatNodeValue("HueAll", 90);			// min -180 max 180		default 0
            ////m_colorGimpParams.SetFloatNodeValue("LightnessAll", 50);		// min -100 max 100		default 0
            ////m_colorGimpParams.SetFloatNodeValue("SaturationAll", 50);		// min -100 max 100		default 0
            ////m_colorGimpParams.SetFloatNodeValue("Overlay", 50);			// min 0 max 100		default 0

            ////m_matrixAlg = m_imageProcApi.GetAlgorithmByName("MatrixMultRGB");
            ////m_matrixAlg.CreateParams(ref m_matrixParams);
            ////m_matrixAlg.CreateResults(ref m_matrixResults);
            ////m_imageProcApi.CreateBitmap(ref m_matrixBitmap);

            ////// example of color correction matrix
            ////m_matrix3x3RGB[0, 0] = 1.0;
            ////m_matrix3x3RGB[0, 1] = 0.0;
            ////m_matrix3x3RGB[0, 2] = 0.0;
            ////m_matrix3x3RGB[1, 0] = 0.0;
            ////m_matrix3x3RGB[1, 1] = 1.0;
            ////m_matrix3x3RGB[1, 2] = 0.0;
            ////m_matrix3x3RGB[2, 0] = 0.0;
            ////m_matrix3x3RGB[2, 1] = 0.0;
            ////m_matrix3x3RGB[2, 2] = 1.0;

            ////for (int i = 0; i < 3; i++)
            ////{
            ////    for (int j = 0; j < 3; j++)
            ////    {
            ////        //m_matrix3x3RGB[i,j] = (i == j) ? 1.0 : 0.0;	// set matrix to do nothing
            ////        string matrix = "Matrix" + i.ToString() + j.ToString();
            ////        m_matrixParams.SetFloatNodeValue(matrix, m_matrix3x3RGB[i, j]);
            ////    }
            ////}

            ////m_changeBitDepthAlg = m_imageProcApi.GetAlgorithmByName("ChangeBitDepth");
            ////m_changeBitDepthAlg.CreateParams(ref m_changeBitDepthParams);
            ////m_changeBitDepthAlg.CreateResults(ref m_changeBitDepthResults);
            ////m_imageProcApi.CreateBitmap(ref m_changeBitDepthBitmap);

            //m_flipRotateAlg = m_imageProcApi.GetAlgorithmByName("ImageFlipRotate");
            //m_flipRotateAlg.CreateParams(ref m_flipRotateParams);
            //m_flipRotateAlg.CreateResults(ref m_flipRotateResults);
            //m_imageProcApi.CreateBitmap(ref m_flipRotateBitmap);

            ////m_flipRotateParams.SetStringNodeValue("FlipType", "Vertical");
            //m_flipRotateParams.SetIntegerNodeValue("RotateValue", 180);

           

        }

        public bool ConnectToCamera()
        {
            // Discover all devices on network
            gigeVisionApi.FindAllDevices(1.0); // was 3.0
            gige.IDevice[] devices = gigeVisionApi.GetAllDevices();

            if (devices.Length > 0)
            {
                // Take first device in list
                m_device = devices[0];

                // to change number of images in image buffer from default 10 images 
                // call SetImageBufferFrameCount() method before Connect() method
                m_device.SetImageBufferFrameCount(1);

                if (m_device != null && m_device.Connect())
                {
                    //Text1 = "Camera address:";
                    //Text2 = Common.IpAddrToString(m_device.GetIpAddress());

                    // disable trigger mode
                    bool status = m_device.SetStringNodeValue("TriggerMode", "Off");

                    // set continuous acquisition mode
                    status = m_device.SetStringNodeValue("AcquisitionMode", "Continuous");
                    status = m_device.SetStringNodeValue("ExposureMode", "Timed");
                    status = m_device.SetFloatNodeValue("ExposureTime", 400000);
                    status = m_device.SetFloatNodeValue("Gain", 20);
                    

                    // start acquisition
                    status = m_device.SetIntegerNodeValue("TLParamsLocked", 1);
                    status = m_device.CommandNodeExecute("AcquisitionStart");
                    return true;
                }
            }
            return false;
        }

        public bool isCameraConnected()
        {
            return (m_device != null && m_device.IsConnected());
        }

        public void GrabImage()
        {
            if (isCameraConnected())
            {
                if (!m_device.IsBufferEmpty())
                {
                    gige.IImageInfo imageInfo = null;
                    m_device.GetImageInfo(ref imageInfo);
                    if (imageInfo != null)
                    {
                        double avgRed, avgGreen, avgBlue;
                        double redGain, greenGain, blueGain;

                        // image processing
                        // calculate Average RGB
                        m_imageProcApi.ExecuteAlgorithm(m_averagePixelAlg, imageInfo, m_adjustGainBitmap, m_averagePixelParams, m_averagePixelResults);
                        m_averagePixelResults.GetFloatNodeValue("RedAverage", out avgRed);
                        m_averagePixelResults.GetFloatNodeValue("GreenAverage", out avgGreen);
                        m_averagePixelResults.GetFloatNodeValue("BlueAverage", out avgBlue);

                        // whiteBalance
                        // Calculate new gain values with white balance algorithm
                        m_whiteBalanceParams.SetFloatNodeValue("RedAverage", avgRed);
                        m_whiteBalanceParams.SetFloatNodeValue("GreenAverage", avgGreen);
                        m_whiteBalanceParams.SetFloatNodeValue("BlueAverage", avgBlue);
                        m_imageProcApi.ExecuteAlgorithm(m_whiteBalanceAlg, imageInfo, m_adjustGainBitmap, m_whiteBalanceParams, m_whiteBalanceResults);

                        // read new gain values
                        m_whiteBalanceResults.GetFloatNodeValue("RedGain", out redGain);
                        m_whiteBalanceResults.GetFloatNodeValue("GreenGain", out greenGain);
                        m_whiteBalanceResults.GetFloatNodeValue("BlueGain", out blueGain);

                        // adjust rgb gain
                        m_adjustGainParams.SetFloatNodeValue("RedGain", redGain);
                        m_adjustGainParams.SetFloatNodeValue("GreenGain", greenGain);
                        m_adjustGainParams.SetFloatNodeValue("BlueGain", blueGain);
                        m_imageProcApi.ExecuteAlgorithm(m_adjustGainAlg, imageInfo, m_adjustGainBitmap, m_adjustGainParams, m_adjustGainResults);

                        // do demosaic on image
                        m_imageProcApi.ExecuteAlgorithm(m_demosaicAlg, m_adjustGainBitmap, m_demosaicBitmap, m_demosaicParams, m_demosaicResults);

                        // image histogram for 8 bit image
                        //m_imageProcApi.ExecuteAlgorithm(m_histogramAlg, m_demosaicBitmap, new gige.IImageBitmap(), new gige.IParams((System.IntPtr)null), m_histogramResults);
                        //m_histogramResults.SetStringNodeValue("HistogramSelector", "Red");
                        //for (int i = 0; i < 256; i++) 
                        //{		
                        //    m_histogramResults.SetIntegerNodeValue("ItemIndex", i);
                        //    m_histogramResults.GetIntegerNodeValue("ItemValue", out m_8bitRedHistogram[i]);
                        //}
                        //m_histogramResults.SetStringNodeValue("HistogramSelector", "Green");
                        //for (int i = 0; i < 256; i++) 
                        //{		
                        //    m_histogramResults.SetIntegerNodeValue("ItemIndex", i);
                        //    m_histogramResults.GetIntegerNodeValue("ItemValue", out m_8bitGreenHistogram[i]);
                        //}
                        //m_histogramResults.SetStringNodeValue("HistogramSelector", "Blue");
                        //for (int i = 0; i < 256; i++) 
                        //{		
                        //    m_histogramResults.SetIntegerNodeValue("ItemIndex", i);
                        //    m_histogramResults.GetIntegerNodeValue("ItemValue", out m_8bitBlueHistogram[i]);
                        //}

                        // lut
                        //m_imageProcApi.ExecuteAlgorithm(m_lutAlg, m_demosaicBitmap, m_lutBitmap, m_lutParams, m_lutResults);

                        // matrix
                        //m_imageProcApi.ExecuteAlgorithm(m_matrixAlg, m_demosaicBitmap, m_matrixBitmap, m_matrixParams, m_matrixResults);

                        // gamma
                        //m_gammaParams.SetFloatNodeValue("EnableInverse", true);
                        //m_gammaParams.SetFloatNodeValue("RedGamma", 2.0);
                        //m_gammaParams.SetFloatNodeValue("GreenGamma", 1.0);
                        //m_gammaParams.SetFloatNodeValue("BlueGamma", 1.0);
                        //m_gammaParams.SetFloatNodeValue("RedGain", 1.0);
                        //m_gammaParams.SetFloatNodeValue("GreenGain", 1.0);
                        //m_gammaParams.SetFloatNodeValue("BlueGain", 1.0);
                        //m_gammaParams.SetFloatNodeValue("RedOffset", 0.0);
                        //m_gammaParams.SetFloatNodeValue("GreenOffset", 0.0);
                        //m_gammaParams.SetFloatNodeValue("BlueOffset", 0.0);

                        //m_imageProcApi.ExecuteAlgorithm(m_gammaAlg, m_demosaicBitmap, m_gammaBitmap, m_gammaParams, m_gammaResults);

                        // color gimp
                        //m_imageProcApi.ExecuteAlgorithm(m_colorGimpAlg, m_demosaicBitmap, m_colorGimpBitmap, m_colorGimpParams, m_colorGimpResults);

                        // sharpen image
                        //m_imageProcApi.ExecuteAlgorithm(m_sharpenAlg, m_demosaicBitmap, m_sharpenBitmap, m_sharpenParams, m_sharpenResults);

                        // flip/rotate image
                        m_imageProcApi.ExecuteAlgorithm(m_flipRotateAlg, m_demosaicBitmap, m_flipRotateBitmap, m_flipRotateParams, m_flipRotateResults);

                        //ImageUtils.CopyToMat(m_demosaicBitmap, ref mat, ref bs);
                        ImageUtils.CopyToMat(m_flipRotateBitmap, ref mat, ref bs); // Ako treba rotirati sliku


                        //// display image
                        //if (bd != null)
                        //{
                        //    lock (CameraUI.pictureLock)
                        //    {
                        //        CameraUI.mainControl.umBitmap = new UnmanagedImage(bd);
                        //    }
                        //}
                        //bitmap.UnlockBits(bd);
                        
                        //GrabberEventArgs g = new GrabberEventArgs();
                        //g.Bitmap=bitmap;
                        //Finished(this, g);
                        
                        //pb_processed.BackgroundImage = bitmap;
                        //show_im(bitmap);
                    }

                    // remove (pop) image from image buffer
                    m_device.PopImage(imageInfo);
                    // empty buffer
                    m_device.ClearImageBuffer();
                }
            }
            else
            {
                m_device = null;
                //gige.GigEVisionSDK.ExitGigEVisionAPI();
                //gige.GigEVisionSDK.ExitImageProcAPI();
                //createDevice();
            }
        }

        public void Disconnect()
        {
            if (m_device != null && m_device.IsConnected())
            {
                bool status = m_device.CommandNodeExecute("AcquisitionStop");
                status = m_device.SetIntegerNodeValue("TLParamsLocked", 0);
                m_device.Disconnect();
            }
        }

        public void Dispose()
        {
            if (m_device != null && m_device.IsConnected())
            {
                bool status = m_device.CommandNodeExecute("AcquisitionStop");
                status = m_device.SetIntegerNodeValue("TLParamsLocked", 0);
                m_device.Disconnect();
            }

            m_gammaAlg.DestroyParams(m_gammaParams);
            m_gammaAlg.DestroyResults(m_gammaResults);
            m_imageProcApi.DestroyBitmap(m_gammaBitmap);

            m_demosaicAlg.DestroyParams(m_demosaicParams);
            m_demosaicAlg.DestroyResults(m_demosaicResults);
            m_imageProcApi.DestroyBitmap(m_demosaicBitmap);

            m_whiteBalanceAlg.DestroyParams(m_whiteBalanceParams);
            m_whiteBalanceAlg.DestroyResults(m_whiteBalanceResults);

            m_averagePixelAlg.DestroyResults(m_averagePixelResults);
            m_averagePixelAlg.DestroyParams(m_averagePixelParams);

            m_adjustGainAlg.DestroyResults(m_adjustGainResults);
            m_adjustGainAlg.DestroyParams(m_adjustGainParams);
            m_imageProcApi.DestroyBitmap(m_adjustGainBitmap);

            m_histogramAlg.DestroyResults(m_histogramResults);

            m_sharpenAlg.DestroyParams(m_sharpenParams);
            m_sharpenAlg.DestroyResults(m_sharpenResults);
            m_imageProcApi.DestroyBitmap(m_sharpenBitmap);

            m_lutAlg.DestroyResults(m_lutResults);
            m_lutAlg.DestroyParams(m_lutParams);
            m_imageProcApi.DestroyBitmap(m_lutBitmap);

            m_colorGimpAlg.DestroyParams(m_colorGimpParams);
            m_colorGimpAlg.DestroyResults(m_colorGimpResults);
            m_imageProcApi.DestroyBitmap(m_colorGimpBitmap);

            m_matrixAlg.DestroyParams(m_matrixParams);
            m_matrixAlg.DestroyResults(m_matrixResults);
            m_imageProcApi.DestroyBitmap(m_matrixBitmap);

            m_changeBitDepthAlg.DestroyParams(m_changeBitDepthParams);
            m_changeBitDepthAlg.DestroyResults(m_changeBitDepthResults);
            m_imageProcApi.DestroyBitmap(m_changeBitDepthBitmap);

            m_flipRotateAlg.DestroyParams(m_flipRotateParams);
            m_changeBitDepthAlg.DestroyResults(m_flipRotateResults);
            m_imageProcApi.DestroyBitmap(m_flipRotateBitmap);

            gige.GigEVisionSDK.ExitGigEVisionAPI();
            gige.GigEVisionSDK.ExitImageProcAPI();
        }
    }
}
