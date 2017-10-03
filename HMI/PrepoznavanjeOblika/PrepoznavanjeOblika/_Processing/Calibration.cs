using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.InteropServices;

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV.Features2D;
using Emgu.CV.WPF;
using Emgu.CV.Reflection;
using Emgu.Util;
using System.IO;

namespace PrepoznavanjeOblika
{
    [Serializable]
    public class CalibrationData
    {
        public Mat CalibrationMatrix;
        public float MMPerPix;

        public CalibrationData()
        {

        }

        public CalibrationData(Mat _calibrationMatrix, float _MMPerPix)
        {
            CalibrationMatrix = _calibrationMatrix;
            MMPerPix = _MMPerPix;
        }
    }

    public class Calibration
    {
        private System.Drawing.PointF[] pixelQuad;
        private System.Drawing.PointF[] worldQuad;
        private CalibrationData calibData = new CalibrationData();
        //private Mat calibrationMatrix;

        public CalibrationData CalibData
        {
            get { return calibData; }
            set { calibData = value; }
        }

        // Constructor - reads calibration matrix from file. If file does not exist matrix is set to unity matrix.
        public Calibration(System.Drawing.PointF[] _worldQuad)
        {
            worldQuad = _worldQuad;

            calibData.CalibrationMatrix = new Mat(new System.Drawing.Size(3, 3), DepthType.Cv64F, 1);

            if (System.IO.File.Exists("calibration.dat"))
            {
                // Read calibration matrix from file
                calibData = ReadCalibDataFromFile("calibration.dat");
            }
            else
            {
                ResetCalibrationMatrix();
            }
        }

        public void ResetCalibrationMatrix()
        {
            List<System.Drawing.PointF> pixelQuadrilateral = new List<System.Drawing.PointF>();
            pixelQuadrilateral.Add(new System.Drawing.PointF(-1, -1));
            pixelQuadrilateral.Add(new System.Drawing.PointF(1, -1));
            pixelQuadrilateral.Add(new System.Drawing.PointF(1, 1));
            pixelQuadrilateral.Add(new System.Drawing.PointF(-1, 1));

            List<System.Drawing.PointF> worldQuadrilateral = new List<System.Drawing.PointF>();
            worldQuadrilateral.Add(new System.Drawing.PointF(-1, -1));
            worldQuadrilateral.Add(new System.Drawing.PointF(1, -1));
            worldQuadrilateral.Add(new System.Drawing.PointF(1, 1));
            worldQuadrilateral.Add(new System.Drawing.PointF(-1, 1));

            CalculateCalibrationData(pixelQuadrilateral.ToArray(), worldQuadrilateral.ToArray());
            WriteCalibDataToFile("calibration.dat");
        }

        public CalibrationData CalculateCalibrationData(System.Drawing.PointF[] pixelQuad, System.Drawing.PointF[] worldQuad)
        {
            // Kalibracijska matrica (za kasnije mapiranje točke u točku)
            calibData.CalibrationMatrix = CvInvoke.GetPerspectiveTransform(pixelQuad, worldQuad);

            // Izračun "duljine" jednog pixela (za kasnije preračunavanje radijusa kruga iz pixela u mm)
            // Srednja vrijednost sva 4 brida kalibracijskog uzorka
            var d1p = Distance(pixelQuad[0], pixelQuad[1]);
            var d2p = Distance(pixelQuad[1], pixelQuad[2]);
            var d3p = Distance(pixelQuad[2], pixelQuad[3]);
            var d4p = Distance(pixelQuad[3], pixelQuad[0]);
            var d1w = Distance(worldQuad[0], worldQuad[1]);
            var d2w = Distance(worldQuad[1], worldQuad[2]);
            var d3w = Distance(worldQuad[2], worldQuad[3]);
            var d4w = Distance(worldQuad[3], worldQuad[0]);
            calibData.MMPerPix = (d1w / d1p + d2w / d2p + d3w / d3p + d4w / d4p) / 4;

            return calibData;
        }

        public CalibrationData CalculateCalibrationData(System.Drawing.PointF[] pixelQuad)
        {
            return CalculateCalibrationData(pixelQuad, worldQuad);
        }

        public void SetWorldQuad(System.Drawing.PointF[] _worldQuad)
        {
            worldQuad = _worldQuad;
        }

        public System.Drawing.PointF GetWorldFromPixel(System.Drawing.PointF pixel_point)
        {
            System.Drawing.PointF[] p = new System.Drawing.PointF[1] { pixel_point };
            return CvInvoke.PerspectiveTransform(p, calibData.CalibrationMatrix)[0];
        }

        public System.Drawing.PointF[] GetWorldFromPixel(System.Drawing.PointF[] pixel_points)
        {
            return CvInvoke.PerspectiveTransform(pixel_points, calibData.CalibrationMatrix);
        }

        public CalibrationData ReadCalibDataFromFile(String filename)
        {
            BinaryFormatter bf = new BinaryFormatter();
            System.IO.FileStream file = System.IO.File.Open(filename, System.IO.FileMode.Open);
            //Mat mat = (Mat)bf.Deserialize(file);
            calibData = (CalibrationData)bf.Deserialize(file);
            file.Close();
            return calibData;

            //Marshal.Copy(arr, 0, calibrationMatrix.DataPointer, 9);
        }

        public void WriteCalibDataToFile(String filename)
        {
            BinaryFormatter bf = new BinaryFormatter();
            System.IO.FileStream file = System.IO.File.Create(filename);
            //bf.Serialize(file, CalibrationMatrix);
            bf.Serialize(file, calibData);
            file.Close();
        }

        public Mat WarpPerspective(Mat img)
        {
            Mat warpedImg = new Mat();
            CvInvoke.WarpPerspective(img, warpedImg, calibData.CalibrationMatrix, img.Size);
            return warpedImg;
        }

        public float Distance(System.Drawing.PointF p1, System.Drawing.PointF p2)
        {
            return (float)Math.Sqrt((float)((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y)));
        }

    } // class Calibration

} // namespace PrepoznavanjeOblika
