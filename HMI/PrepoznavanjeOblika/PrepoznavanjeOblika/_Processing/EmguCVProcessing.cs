using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Forms;

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV.Features2D;
using Emgu.CV.WPF;
using Emgu.CV.Reflection;
using Emgu.Util;

namespace PrepoznavanjeOblika
{
    public enum objectType { NONE, ANGLE, CIRCLE, LINE, TWO_LINES };

    public class CameraOutputType
    {
        public System.Drawing.PointF POINT1; // Angle center, circle center or first line point
        public System.Drawing.PointF POINT2; // Second line point
        public float PARAMETER; // Angle value, Radius
        public objectType TYPE; // 0 - No object present, 1 - Angle, 2 - Circle, 3 - Line

        public CameraOutputType(System.Drawing.PointF _p1, System.Drawing.PointF _p2, float _parameter, objectType _type)
        {
            POINT1 = _p1;
            POINT2 = _p2;
            PARAMETER = _parameter;
            TYPE = _type;
        }

        public CameraOutputType()
        {
        }
    }

    public class CameraOutputForAutoTracking
    {
        public System.Drawing.PointF[] POINTS;
        public float PARAMETER; // Angle value, Radius
        public objectType TYPE; // 0 - No object present, 1 - Angle, 2 - Circle, 3 - Line

        public CameraOutputForAutoTracking(System.Drawing.PointF[] _points, float _parameter, objectType _type)
        {
            POINTS = _points;
            PARAMETER = _parameter;
            TYPE = _type;
        }

        public CameraOutputForAutoTracking()
        {

        }
    }

    class EmguCVProcessing
    {
        public Mat currentFrame;
        public Mat colorcurframe;
        public Mat frameToShow;
        List<System.Drawing.Point> edge_positions = new List<System.Drawing.Point>();
        public CameraOutputType cameraOutput;
        public CameraOutputForAutoTracking objectFound;
        public System.Drawing.PointF[] pixelQuad;
        public CalibrationData CalibData;

        // test
        public int thr1 = 160, thr2 = 10;

        private Capture capture;

        private static readonly Random rnd = new Random(Environment.TickCount);

        // Constructor
        public EmguCVProcessing()
        {
            frameToShow = new Mat();
            colorcurframe = new Mat();
            cameraOutput = new CameraOutputType();
            objectFound = new CameraOutputForAutoTracking();

            //capture = new Capture();
        }

        public Mat GetImageFromWebcam()
        {
            Mat frame = new Mat();
            if (capture == null) capture = new Capture();
            capture.Retrieve(frame);

            //Mat rotMat = new Mat();
            //Mat rotFrame = new Mat();
            //CvInvoke.GetRotationMatrix2D(new System.Drawing.Point(frame.Rows / 2, frame.Cols / 2), 270, 2.0, rotMat);
            //CvInvoke.WarpAffine(frame, rotFrame, rotMat, new System.Drawing.Size(0, 0));

            //int cropWidth = frame.Width / 4;
            //System.Drawing.Rectangle rect = new System.Drawing.Rectangle(cropWidth, 0, cropWidth, cropWidth/4);
            //Mat croppedRotFrame = new Mat(rotFrame, rect);

            return frame;
        }

        public void CloseWebcam()
        {
            if (capture != null)
            {
                capture.Dispose();
                capture = null;
            }
        }

        public void Process()
        {

            if (currentFrame != null)
            {
                cameraOutput.TYPE = objectType.NONE; // Nema ničeg dok se nešto ne pronađe :)
                objectFound.TYPE = objectType.NONE;

                CvInvoke.CvtColor(currentFrame, colorcurframe, ColorConversion.Gray2Rgb); // za prikaz u boji full res

                edge_positions = findEdgesWithGridSimple(currentFrame, 50, thr1);
                drawPoints(colorcurframe, edge_positions);

                #region CIRCLE DETECTION

                CircleF circ;
                if (findCircleByFitEllipse(edge_positions, out circ))
                {
                    // Nacrtaj (full res)
                    CvInvoke.Circle(colorcurframe, new System.Drawing.Point((int)circ.Center.X, (int)circ.Center.Y), (int)circ.Radius, new MCvScalar(0, 255, 0), 40);
                    CvInvoke.Circle(colorcurframe, new System.Drawing.Point((int)circ.Center.X, (int)circ.Center.Y), 20, new MCvScalar(0, 255, 0), 20);

                    // CameraOutput (za PLC)
                    cameraOutput.TYPE = objectType.CIRCLE; // krug

                    // Bez pretvorbe u mm
                    //cameraOutput.POINT1 = new System.Drawing.PointF(circ.Center.X + x1, circ.Center.Y + y1);
                    //cameraOutput.PARAMETER = circ.Radius;

                    // S pretvorbom u mm
                    cameraOutput.POINT1 = GetWorldFromPixel(new System.Drawing.PointF(circ.Center.X, circ.Center.Y));
                    cameraOutput.PARAMETER = GetMMFromPix(circ.Radius);

                    // Za auto tracking (za Šarliju)
                    objectFound.TYPE = objectType.CIRCLE;
                    objectFound.POINTS = new System.Drawing.PointF[1];
                    objectFound.POINTS[0] = cameraOutput.POINT1;
                    objectFound.PARAMETER = cameraOutput.PARAMETER;
                }
                #endregion CIRCLE DETECTION

                else
                    
                // Ako nema krugova, traži linije (kutove)
                #region LINE DETECTION
                {
                    // RANSAC za Linije
                    List<Tuple<System.Drawing.PointF, System.Drawing.PointF>> lines;
                    int ret = findLineByRANSAC(edge_positions, out lines);

                    // Crtanje svih pronađenih pravaca
                    //for (int i = 0; i < ret; i++)
                    //{
                    //    // crtanje
                    //    System.Drawing.Point p1 = new System.Drawing.Point((int)lines[i].Item1.X, (int)lines[i].Item1.Y);
                    //    System.Drawing.Point p2 = new System.Drawing.Point((int)lines[i].Item2.X, (int)lines[i].Item2.Y);

                    //    CvInvoke.Line(colorcurframe, p1, p2, new MCvScalar(255, 0, 255), 10);
                    //}

                    if (ret == 1)
                    {
                        // Pronađena je linija
                        var ep1F = lines[0].Item1;
                        var ep2F = lines[0].Item2;

                        // Za PLC
                        cameraOutput.TYPE = objectType.LINE; // linija

                        // Bez pretvorbe u mm
                        //cameraOutput.POINT1 = ep1F;
                        //cameraOutput.POINT2 = ep2F;
                        //cameraOutput.PARAMETER = Distance(ep1F, ep2F);  // duljina linije u pixelima

                        // S pretvorbom u mm
                        cameraOutput.POINT1 = GetWorldFromPixel(ep1F);
                        cameraOutput.POINT2 = GetWorldFromPixel(ep2F);
                        cameraOutput.PARAMETER = Distance(cameraOutput.POINT1, cameraOutput.POINT2);  // duljina linije u mm

                        // Za auto tracking
                        objectFound.TYPE = objectType.LINE;
                        objectFound.POINTS = new System.Drawing.PointF[2];
                        objectFound.POINTS[0] = ep1F;
                        objectFound.POINTS[1] = ep2F;
                        objectFound.PARAMETER = cameraOutput.PARAMETER;

                        // Nacrtaj zelenu liniju
                        CvInvoke.Line(colorcurframe, toPoint(ep1F), toPoint(ep2F), new MCvScalar(0, 255, 0), 40);
                    }
                    else if (ret == 2) //2
                    {
                        // 1. Naći špic
                        var spic = LineIntersectionPoint(lines[0].Item1, lines[0].Item2, lines[1].Item1, lines[1].Item2);
                        if (isPointInImage(spic, currentFrame.Size))
                        {
                            System.Drawing.PointF[] point_list = new System.Drawing.PointF[3];

                            point_list[0] = spic;

                            // 2. Naći rubnu točku na prvom pravcu
                            int grad1, grad2;
                            System.Drawing.Point ip1 = new System.Drawing.Point((int)lines[0].Item1.X, (int)lines[0].Item1.Y);
                            System.Drawing.Point ip2 = new System.Drawing.Point((int)lines[0].Item2.X, (int)lines[0].Item2.Y);
                            findPointOnBorder(currentFrame, ip1, out grad1);
                            findPointOnBorder(currentFrame, ip2, out grad2);
                            if (grad1 > 40 || grad2 > 40)
                            {
                                if (grad1 > grad2) point_list[1] = lines[0].Item1;
                                else point_list[1] = lines[0].Item2;
                            }
                            else
                            {
                                // Uzeti onaj koji bliže ima crvenu točkicu
                                float dist, ip1_min = 5000, ip2_min = 5000;
                                foreach (var p in edge_positions)
                                {
                                    dist = Distance(ip1, p);
                                    if (dist < ip1_min) ip1_min = dist;
                                    dist = Distance(ip2, p);
                                    if (dist < ip2_min) ip2_min = dist;
                                }
                                if (ip1_min < ip2_min) point_list[1] = lines[0].Item1;
                                else point_list[1] = lines[0].Item2;
                            }
                            

                            // 3. Naći rubnu točku na drugom pravcu
                            ip1 = new System.Drawing.Point((int)lines[1].Item1.X, (int)lines[1].Item1.Y);
                            ip2 = new System.Drawing.Point((int)lines[1].Item2.X, (int)lines[1].Item2.Y);
                            findPointOnBorder(currentFrame, ip1, out grad1);
                            findPointOnBorder(currentFrame, ip2, out grad2);
                            if (grad1 > 40 || grad2 > 40)
                            {
                                if (grad1 > grad2) point_list[2] = lines[1].Item1;
                                else point_list[2] = lines[1].Item2;
                            }
                            else
                            {
                                // Uzeti onaj koji bliže ima crvenu točkicu
                                float dist, ip1_min = 5000, ip2_min = 5000;
                                foreach (var p in edge_positions)
                                {
                                    dist = Distance(ip1, p);
                                    if (dist < ip1_min) ip1_min = dist;
                                    dist = Distance(ip2, p);
                                    if (dist < ip2_min) ip2_min = dist;
                                }
                                if (ip1_min < ip2_min) point_list[2] = lines[1].Item1;
                                else point_list[2] = lines[1].Item2;
                            }

                            // Za poslati rezultat klasi koja poziva ovu funkciju
                            cameraOutput.TYPE = objectType.ANGLE; // Kut

                            // Bez preračunavanja u mm
                            //cameraOutput.POINT1 = point_list[0];

                            // S pretvorbom u mm
                            System.Drawing.PointF[] world_list = GetWorldFromPixel(point_list);

                            cameraOutput.POINT1 = world_list[0];

                            // Izračunaj kut u stupnjevima
                            var x1 = world_list[1].X - world_list[0].X;
                            var y1 = world_list[1].Y - world_list[0].Y;
                            var x2 = world_list[2].X - world_list[0].X;
                            var y2 = world_list[2].Y - world_list[0].Y;
                            var p1l2 = x1 * x1 + y1 * y1;
                            var p2l2 = x2 * x2 + y2 * y2;
                            cameraOutput.PARAMETER = (float)(Math.Acos((x1 * x2 + y1 * y2) / Math.Sqrt(p1l2 * p2l2)) * 180 / Math.PI);

                            // Za auto tracking (pixeli ili mm?)
                            objectFound.TYPE = objectType.ANGLE;
                            objectFound.POINTS = point_list;
                            objectFound.PARAMETER = cameraOutput.PARAMETER;

                            // Nacrtaj rubne točke
                            //CvInvoke.Circle(colorcurframe, new System.Drawing.Point((int)point_list[1].X, (int)point_list[1].Y), 15, new MCvScalar(255, 255, 255), 5);
                            //CvInvoke.Circle(colorcurframe, new System.Drawing.Point((int)point_list[2].X, (int)point_list[2].Y), 15, new MCvScalar(255, 255, 255), 5);

                            // Crtanje linija (krakova) kuta (point_list[0] je spic, a point_list[1] i point_list[2] su rubne točke)
                            CvInvoke.Line(colorcurframe, toPoint(point_list[1]), toPoint(point_list[0]), new MCvScalar(0, 255, 0), 40);
                            CvInvoke.Line(colorcurframe, toPoint(point_list[0]), toPoint(point_list[2]), new MCvScalar(0, 255, 0), 40);
                        }
                        else
                        {
                            // Spic nije na slici -> pronađene su 2 linije
                            // Za auto tracking je bitno i ako se pojave 4 točke (2 linije)
                            // Prve dvi točke su rubne točke prve linije, a druge dvi druge linije
                            objectFound.TYPE = objectType.TWO_LINES;
                            objectFound.POINTS = new System.Drawing.PointF[4];
                            objectFound.POINTS[0] = lines[0].Item1;
                            objectFound.POINTS[1] = lines[0].Item2;
                            objectFound.POINTS[2] = lines[1].Item1;
                            objectFound.POINTS[3] = lines[1].Item2;
                        }
                    }
                    else if (ret == 3)
                    {
                        // Pronađene su tri linije

                        List<Tuple<System.Drawing.PointF, System.Drawing.PointF, System.Drawing.PointF, System.Drawing.PointF, System.Drawing.PointF>> spicevi = new List<Tuple<System.Drawing.PointF, System.Drawing.PointF, System.Drawing.PointF, System.Drawing.PointF, System.Drawing.PointF>>(3);

                        // Naći špiceve
                        spicevi.Add(new Tuple<System.Drawing.PointF, System.Drawing.PointF, System.Drawing.PointF,System.Drawing.PointF,System.Drawing.PointF>(
                            LineIntersectionPoint(lines[0].Item1, lines[0].Item2, lines[1].Item1, lines[1].Item2),
                            lines[0].Item1, lines[0].Item2, lines[1].Item1, lines[1].Item2));

                        spicevi.Add(new Tuple<System.Drawing.PointF, System.Drawing.PointF, System.Drawing.PointF,System.Drawing.PointF,System.Drawing.PointF>(
                            LineIntersectionPoint(lines[0].Item1, lines[0].Item2, lines[2].Item1, lines[2].Item2),
                            lines[0].Item1, lines[0].Item2, lines[2].Item1, lines[2].Item2));

                        spicevi.Add(new Tuple<System.Drawing.PointF, System.Drawing.PointF, System.Drawing.PointF,System.Drawing.PointF,System.Drawing.PointF>(
                            LineIntersectionPoint(lines[1].Item1, lines[1].Item2, lines[2].Item1, lines[2].Item2),
                            lines[1].Item1, lines[1].Item2, lines[2].Item1, lines[2].Item2));

                        // Naći koji su u slici i udaljenost do centra slike
                        List<Tuple<System.Drawing.PointF, System.Drawing.PointF, System.Drawing.PointF, System.Drawing.PointF, System.Drawing.PointF, float>> spicevi2 = new List<Tuple<System.Drawing.PointF, System.Drawing.PointF, System.Drawing.PointF, System.Drawing.PointF, System.Drawing.PointF, float>>(3);
                        foreach (var spic in spicevi)
                        {
                            if (isPointInImage(spic.Item1, currentFrame.Size))
                            {
                                float disttocenter = distanceToCenter(spic.Item1, currentFrame.Size);
                                spicevi2.Add(new Tuple<System.Drawing.PointF, System.Drawing.PointF, System.Drawing.PointF, System.Drawing.PointF, System.Drawing.PointF, float>(
                                    spic.Item1, spic.Item2, spic.Item3, spic.Item4, spic.Item5, disttocenter));
                            }
                        }

                        // Ako su sva 3 sjecišta upala na sliku
                        if (spicevi2.Count == 3)
                        {
                            float max_dist_to_center = 0.0f;
                            int max_dist_to_center_ind = 0;
                            // Izbaci onaj koji je najdalje od centra
                            for (int i = 0; i < spicevi2.Count; i++)
                            {
                                if (spicevi2[i].Item6 > max_dist_to_center)
                                {
                                    max_dist_to_center = spicevi2[i].Item6;
                                    max_dist_to_center_ind = i;
                                }
                            }
                            spicevi2.RemoveAt(max_dist_to_center_ind);
                        }

                        if (spicevi2.Count == 2)
                        {
                            // Naći točke na rubu slike (di je najveći gradijent) i kojem spicu 'pripadaju'
                            int grad;
                            const int GRAD_THRESH = 50;
                            const int LINE_WIDTH = 40;

                            // Prvi spic spicevi2[0]
                            System.Drawing.PointF border_point_1 = new System.Drawing.PointF();
                            findPointOnBorder(currentFrame, toPoint(spicevi2[0].Item2), out grad);
                            if (grad > GRAD_THRESH)
                            {
                                CvInvoke.Line(colorcurframe, toPoint(spicevi2[0].Item1), toPoint(spicevi2[0].Item2), new MCvScalar(0, 255, 0), LINE_WIDTH);
                                border_point_1 = spicevi2[0].Item2;
                            }

                            findPointOnBorder(currentFrame, toPoint(spicevi2[0].Item3), out grad);
                            if (grad > GRAD_THRESH)
                            {
                                CvInvoke.Line(colorcurframe, toPoint(spicevi2[0].Item1), toPoint(spicevi2[0].Item3), new MCvScalar(0, 255, 0), LINE_WIDTH);
                                border_point_1 = spicevi2[0].Item3;
                            }

                            findPointOnBorder(currentFrame, toPoint(spicevi2[0].Item4), out grad);
                            if (grad > GRAD_THRESH)
                            {
                                CvInvoke.Line(colorcurframe, toPoint(spicevi2[0].Item1), toPoint(spicevi2[0].Item4), new MCvScalar(0, 255, 0), LINE_WIDTH);
                                border_point_1 = spicevi2[0].Item4;
                            }

                            findPointOnBorder(currentFrame, toPoint(spicevi2[0].Item5), out grad);
                            if (grad > GRAD_THRESH)
                            {
                                CvInvoke.Line(colorcurframe, toPoint(spicevi2[0].Item1), toPoint(spicevi2[0].Item5), new MCvScalar(0, 255, 0),LINE_WIDTH);
                                border_point_1 = spicevi2[0].Item5;
                            }

                            // Drugi spic spicevi2[1]
                            System.Drawing.PointF border_point_2 = new System.Drawing.PointF();
                            findPointOnBorder(currentFrame, toPoint(spicevi2[1].Item2), out grad);
                            if (grad > GRAD_THRESH)
                            {
                                CvInvoke.Line(colorcurframe, toPoint(spicevi2[1].Item1), toPoint(spicevi2[1].Item2), new MCvScalar(0, 255, 0), LINE_WIDTH);
                                border_point_2 = spicevi2[1].Item2;
                            }

                            findPointOnBorder(currentFrame, toPoint(spicevi2[1].Item3), out grad);
                            if (grad > GRAD_THRESH)
                            {
                                CvInvoke.Line(colorcurframe, toPoint(spicevi2[1].Item1), toPoint(spicevi2[1].Item3), new MCvScalar(0, 255, 0), LINE_WIDTH);
                                border_point_2 = spicevi2[1].Item3;
                            }

                            findPointOnBorder(currentFrame, toPoint(spicevi2[1].Item4), out grad);
                            if (grad > GRAD_THRESH)
                            {
                                CvInvoke.Line(colorcurframe, toPoint(spicevi2[1].Item1), toPoint(spicevi2[1].Item4), new MCvScalar(0, 255, 0), LINE_WIDTH);
                                border_point_2 = spicevi2[1].Item4;
                            }

                            findPointOnBorder(currentFrame, toPoint(spicevi2[1].Item5), out grad);
                            if (grad > GRAD_THRESH)
                            {
                                CvInvoke.Line(colorcurframe, toPoint(spicevi2[1].Item1), toPoint(spicevi2[1].Item5), new MCvScalar(0, 255, 0), LINE_WIDTH);
                                border_point_2 = spicevi2[1].Item5;
                            }

                            // Nacrtaj i liniju između 2 spica
                            CvInvoke.Line(colorcurframe, toPoint(spicevi2[0].Item1), toPoint(spicevi2[1].Item1), new MCvScalar(0, 255, 0), LINE_WIDTH);

                            System.Drawing.PointF[] point_list = new System.Drawing.PointF[3];

                            // Odrediti kut između 2 pravca koji pripadaju spicu blizu centra slike
                            if (spicevi2[0].Item6 < spicevi2[1].Item6)
                            {
                                // 1. Kut  spicevi2[0]
                                point_list[0] = spicevi2[0].Item1;

                                // 2. Drugi kut
                                point_list[1] = spicevi2[1].Item1;

                                // 3. Točka na rubu u kojoj je veliki gradijent
                                point_list[2] = border_point_1;
                            }
                            else
                            {
                                // 1. Kut  spicevi2[1]
                                point_list[0] = spicevi2[1].Item1;

                                // 2. Drugi kut
                                point_list[1] = spicevi2[0].Item1;

                                // 3. Točka na rubu u kojoj je veliki gradijent
                                point_list[2] = border_point_2;
                            }

                            // Kružić oko odabranog spica
                            CvInvoke.Circle(colorcurframe, toPoint(point_list[0]), 55, new MCvScalar(0, 0, 255), 16);

                            // Za poslati rezultat klasi koja poziva ovu funkciju
                            cameraOutput.TYPE = objectType.ANGLE; // Kut

                            // S pretvorbom u mm
                            System.Drawing.PointF[] world_list = GetWorldFromPixel(point_list);

                            cameraOutput.POINT1 = world_list[0];

                            // Izračunaj kut u stupnjevima
                            var x1 = world_list[1].X - world_list[0].X;
                            var y1 = world_list[1].Y - world_list[0].Y;
                            var x2 = world_list[2].X - world_list[0].X;
                            var y2 = world_list[2].Y - world_list[0].Y;
                            var p1l2 = x1 * x1 + y1 * y1;
                            var p2l2 = x2 * x2 + y2 * y2;
                            cameraOutput.PARAMETER = (float)(Math.Acos((x1 * x2 + y1 * y2) / Math.Sqrt(p1l2 * p2l2)) * 180 / Math.PI);

                            // Za auto tracking (pixeli ili mm?)
                            objectFound.TYPE = objectType.ANGLE;
                            objectFound.POINTS = point_list;
                            objectFound.PARAMETER = cameraOutput.PARAMETER;
                        }

                    }

                }
                #endregion LINE DETECTION
                    
                // Za brži prikaz (u maloj rezoluciji)
                CvInvoke.Resize(colorcurframe, frameToShow, new System.Drawing.Size(currentFrame.Width / 4, currentFrame.Height / 4), 0, 0, Inter.Linear);

            }//currentFrame != null
        }//kraj funkcije

        private System.Drawing.Point toPoint(System.Drawing.PointF p)
        {
            return new System.Drawing.Point((int)p.X, (int)p.Y);
        }

        private bool isPointInImage(System.Drawing.PointF p, System.Drawing.Size size)
        {
            if (p.X < size.Width && p.X >= 0 && p.Y < size.Height && p.Y >= 0) return true;
            else return false;
        }

        float distancePointToLine(float A, float B, float C, System.Drawing.Point p)
        {
            return ( Math.Abs(A*p.X + B*p.Y + C) / (float)Math.Sqrt(A*A + B*B) );
            //return (Math.Abs(A * p.X + B * p.Y + C));
        }

        System.Drawing.PointF LineIntersectionPoint(System.Drawing.PointF ps1, System.Drawing.PointF pe1, System.Drawing.PointF ps2, System.Drawing.PointF pe2)
        {
            // Get A,B,C of first line - points : ps1 to pe1
            float A1 = pe1.Y - ps1.Y;
            float B1 = ps1.X - pe1.X;
            float C1 = A1 * ps1.X + B1 * ps1.Y;

            // Get A,B,C of second line - points : ps2 to pe2
            float A2 = pe2.Y - ps2.Y;
            float B2 = ps2.X - pe2.X;
            float C2 = A2 * ps2.X + B2 * ps2.Y;

            // Get delta and check if the lines are parallel
            float delta = A1 * B2 - A2 * B1;
            if (delta == 0)
            {
                //throw new System.Exception("Lines are parallel");
                return new System.Drawing.PointF(3000, 3000);
            }

            // now return the Vector2 intersection point
            return new System.Drawing.PointF(
                (B2 * C1 - B1 * C2) / delta,
                (A1 * C2 - A2 * C1) / delta
            );
        }

        // For mapping pixels to world coordinates (mm)
        public System.Drawing.PointF GetWorldFromPixel(System.Drawing.PointF pixel_point)
        {
            System.Drawing.PointF[] p = new System.Drawing.PointF[1] { pixel_point };
            return CvInvoke.PerspectiveTransform(p, CalibData.CalibrationMatrix)[0];
        }

        public System.Drawing.PointF[] GetWorldFromPixel(System.Drawing.PointF[] pixel_points)
        {
            return CvInvoke.PerspectiveTransform(pixel_points, CalibData.CalibrationMatrix);
        }

        public float GetMMFromPix(float pix_distance)
        {
            return pix_distance * CalibData.MMPerPix;
        }

        public bool ProcessCalibration()
        {
            if (currentFrame != null)
            {
                CvInvoke.CvtColor(currentFrame, colorcurframe, ColorConversion.Gray2Rgb); // za prikaz u boji full res

                double cannyThreshold = 180; //180 def
                double circleAccumulatorThreshold = 70; //120 def

                CircleF[] circles = CvInvoke.HoughCircles(
                    currentFrame, //input image
                    HoughType.Gradient, //method
                    2, // dp 2.0 default
                    500, // minDist
                    thr1, //cannyThreshold, //
                    thr2, //circleAccumulatorThreshold, //
                    200); //min radius
                
                int cnt = 0;
                if (circles.Length == 4)
                {
                    // Našao je sva 4 kruga
                    pixelQuad = new System.Drawing.PointF[4];
                    for (int i = 0; i < 4; i++)
                    {
                        // Pronađen je krug, sad treba preciznije naći njegovo središte i radijus
                        // Opcije: opet HoughCircles ili contours + boundingRect ili RANSAC ili DT
                        var circle = circles[i];

                        // Uzimam dio originalne slike na kojem je pronađeni krug
                        var x1 = (int)(circle.Center.X - circle.Radius) - 30;
                        var x2 = (int)(circle.Center.X + circle.Radius) + 30;
                        var y1 = (int)(circle.Center.Y - circle.Radius) - 30;
                        var y2 = (int)(circle.Center.Y + circle.Radius) + 30;
                        if (x1 < 0) x1 = 0;
                        if (x2 > currentFrame.Width) x2 = currentFrame.Width;
                        if (y1 < 0) y1 = 0;
                        if (y2 > currentFrame.Height) y2 = currentFrame.Height;

                        Mat circleImg = new Mat(currentFrame, new System.Drawing.Rectangle(x1, y1, x2 - x1, y2 - y1));

                        CircleF circ = new CircleF();
                        Mat circleImgEdges = new Mat();
                        CvInvoke.Canny(circleImg, circleImgEdges, 200, 120);
                        //bool circle_found = RANSAC(circleImgEdges, out circ);
                        //bool circle_found = FindCircleByAvg(circleImgEdges, out circ);
                        bool circle_found = FindCircleByBoundingRect(circleImgEdges, out circ);
                        //bool circle_found = FindCircleByDistanceTransform(circleImg, out circ);

                        if (circle_found)
                        {
                            pixelQuad[i] = circles[i].Center;

                            // Nacrtaj
                            CvInvoke.Circle(colorcurframe, new System.Drawing.Point((int)pixelQuad[i].X, (int)pixelQuad[i].Y), 10, new MCvScalar(0, 0, 255), 8);
                            CvInvoke.Circle(colorcurframe, new System.Drawing.Point((int)pixelQuad[i].X, (int)pixelQuad[i].Y), (int)circles[i].Radius, new MCvScalar(0, 0, 255), 8);
                            cnt++;
                        }
                    }
                }
                CvInvoke.Resize(colorcurframe, frameToShow, new System.Drawing.Size(currentFrame.Width / 4, currentFrame.Height / 4), 0, 0, Inter.Linear);
                if (cnt == 4) return true;
                else return false;
            }
            return false;
        }

        private bool isPointNearBorder(System.Drawing.PointF p, System.Drawing.Size img_size, uint distance_to_border)
        {
            if (p.X < distance_to_border) return true;
            if (p.Y < distance_to_border) return true;
            if (p.X > img_size.Width - distance_to_border) return true;
            if (p.Y > img_size.Height - distance_to_border) return true;
            return false;
        }

        private bool isPointNearBorder(System.Drawing.Point p, System.Drawing.Size img_size, uint distance_to_border)
        {
            if (p.X < distance_to_border) return true;
            if (p.Y < distance_to_border) return true;
            if (p.X > img_size.Width - distance_to_border) return true;
            if (p.Y > img_size.Height - distance_to_border) return true;
            return false;
        }

        private float distanceToCenter(System.Drawing.PointF p, System.Drawing.Size img_size)
        {
            float xc = img_size.Width / 2.0f;
            float yc = img_size.Height / 2.0f;

            float xp = p.X;
            float yp = p.Y;

            return (float)Math.Sqrt((float)((xc - xp) * (xc - xp) + (yc - yp) * (yc - yp)));
        }

        /* Za zadanu točku funkcija pronalazi točku s najvećim gradijentom na najbližem rubu slike 
         * u okolini (+/- 20 pixela) zadane točke. */
        private System.Drawing.Point findPointOnBorder(Mat currentFrame, System.Drawing.Point ep, out int gradient)
        {
            int[] d = new int[4];
            d[0] = ep.X;
            d[1] = currentFrame.Width - ep.X;
            d[2] = ep.Y;
            d[3] = currentFrame.Height - ep.Y;
            int minIndex = Array.IndexOf(d, d.Min());
            int range = 20;

            System.Drawing.Point pointOnBorder = new System.Drawing.Point();
            int max_diff = 0;

            switch (minIndex)
            {
                // Lijevi rub
                case 0:
                    {
                        int lower_limit = (ep.Y - range > 0) ? ep.Y - range : 0;
                        int upper_limit = (ep.Y + range < currentFrame.Height) ? ep.Y + range : currentFrame.Height;

                        var pix1 = ((int)currentFrame.GetData(lower_limit, 0)[0] + (int)currentFrame.GetData(lower_limit, 1)[0]) / 2;
                        int max_diff_ind = -1;
                        max_diff = 0;
                        for (int i = lower_limit + 1; i < upper_limit; i++)
                        {
                            var pix2 = ((int)currentFrame.GetData(i, 0)[0] + (int)currentFrame.GetData(i, 1)[0]) / 2;
                            var diff = Math.Abs((int)pix2 - (int)pix1);
                            if (diff > max_diff)
                            {
                                max_diff = diff;
                                max_diff_ind = i;
                            }
                            pix1 = pix2;
                        }
                        // Nacrtaj pronađenu točku na rubu
                        //CvInvoke.Circle(colorcurframe, new System.Drawing.Point(0, max_diff_ind), 15, new MCvScalar(255, 255, 255), 5);
                        pointOnBorder = new System.Drawing.Point(0, max_diff_ind);
                        break;
                    }

                // Desni rub
                case 1:
                    {
                        int lower_limit = (ep.Y - range > 0) ? ep.Y - range : 0;
                        int upper_limit = (ep.Y + range < currentFrame.Height) ? ep.Y + range : currentFrame.Height;

                        var pix1 = ((int)currentFrame.GetData(lower_limit, currentFrame.Width - 1)[0] + (int)currentFrame.GetData(lower_limit, currentFrame.Width - 2)[0]) / 2;
                        int max_diff_ind = -1;
                        max_diff = 0;
                        for (int i = lower_limit + 1; i < upper_limit; i++)
                        {
                            var pix2 = ((int)currentFrame.GetData(i, currentFrame.Width - 1)[0] + (int)currentFrame.GetData(i, currentFrame.Width - 2)[0]) / 2;
                            var diff = Math.Abs((int)pix2 - (int)pix1);
                            if (diff > max_diff)
                            {
                                max_diff = diff;
                                max_diff_ind = i;
                            }
                            pix1 = pix2;
                        }
                        // Nacrtaj pronađenu točku na rubu
                        //CvInvoke.Circle(colorcurframe, new System.Drawing.Point(currentFrame.Width - 1, max_diff_ind), 15, new MCvScalar(255, 255, 255), 5);
                        pointOnBorder = new System.Drawing.Point(currentFrame.Width - 1, max_diff_ind);
                        break;
                    }

                // Gornji rub
                case 2:
                    {
                        int lower_limit = (ep.X - range > 0) ? ep.X - range : 0;
                        int upper_limit = (ep.X + range < currentFrame.Width) ? ep.X + range : currentFrame.Width;

                        var pix1 = ((int)currentFrame.GetData(0, lower_limit)[0] + (int)currentFrame.GetData(1, lower_limit)[0]) / 2;
                        int max_diff_ind = -1;
                        max_diff = 0;
                        for (int i = lower_limit + 1; i < upper_limit; i++)
                        {
                            var pix2 = ((int)currentFrame.GetData(0, i)[0] + (int)currentFrame.GetData(1, i)[0]) / 2;
                            var diff = Math.Abs((int)pix2 - (int)pix1);
                            if (diff > max_diff)
                            {
                                max_diff = diff;
                                max_diff_ind = i;
                            }
                            pix1 = pix2;
                        }
                        // Nacrtaj pronađenu točku na rubu
                        //CvInvoke.Circle(colorcurframe, new System.Drawing.Point(max_diff_ind, 0), 15, new MCvScalar(255, 255, 255), 5);
                        pointOnBorder = new System.Drawing.Point(max_diff_ind, 0);
                        break;
                    }

                // Donji rub
                case 3:
                    {
                        int lower_limit = (ep.X - range > 0) ? ep.X - range : 0;
                        int upper_limit = (ep.X + range < currentFrame.Width) ? ep.X + range : currentFrame.Width;

                        var pix1 = ((int)currentFrame.GetData(currentFrame.Height - 1, lower_limit)[0] + (int)currentFrame.GetData(currentFrame.Height - 2, lower_limit)[0]) / 2;
                        int max_diff_ind = -1;
                        max_diff = 0;
                        for (int i = lower_limit + 1; i < upper_limit; i++)
                        {
                            var pix2 = ((int)currentFrame.GetData(currentFrame.Height - 1, i)[0] + (int)currentFrame.GetData(currentFrame.Height - 2, i)[0]) / 2;
                            var diff = Math.Abs((int)pix2 - (int)pix1);
                            if (diff > max_diff)
                            {
                                max_diff = diff;
                                max_diff_ind = i;
                            }
                            pix1 = pix2;
                        }
                        // Nacrtaj pronađenu točku na rubu
                        //CvInvoke.Circle(colorcurframe, new System.Drawing.Point(max_diff_ind, currentFrame.Height - 1), 15, new MCvScalar(255, 255, 255), 5);
                        pointOnBorder = new System.Drawing.Point(max_diff_ind, currentFrame.Height - 1);
                        break;
                    }
            }
            gradient = max_diff;
            return pointOnBorder;
        }


        /* Vraća točku koja je bliža prvoj ulaznoj točci. */
        private System.Drawing.PointF extrapolateToBorder(System.Drawing.Size im_size, System.Drawing.PointF p1, System.Drawing.PointF p2)
        {
            float k = (p1.Y - p2.Y) / (p1.X - p2.X);

            // y = y1 + k(x - x1) -> (y - y1)/k = x - x1 -> x = x1 + (y - y1)/k

            System.Drawing.PointF[] coords = new System.Drawing.PointF[4];

            // Sjecište s lijevim rubom, x = 0
            float yl = p1.Y - k * p1.X;
            coords[0] = new System.Drawing.PointF(0, yl);

            // Sjecište s desnim rubom, x = im_size.Width - 1
            float yr = p1.Y + k * (im_size.Width - 1 - p1.X);
            coords[1] = new System.Drawing.PointF(im_size.Width - 1, yr);

            // Sjecište s gornjim rubom, y = 0
            float xt = p1.X - p1.Y / k;
            coords[2] = new System.Drawing.PointF(xt, 0);

            // Sjecište s donjim rubom, y = im_size.Height - 1
            float xb = p1.X + (im_size.Height - 1 - p1.Y) / k;
            coords[3] = new System.Drawing.PointF(xb, im_size.Height - 1);

            int min_dist_ind = -1;
            float min_dist = im_size.Width * 2;
            for (int i = 0; i < 4; i++)
            {
                var d = Distance(coords[i], p1);
                if (d < min_dist)
                {
                    min_dist = d;
                    min_dist_ind = i;
                }
            }
            return coords[min_dist_ind];
        }

        public float Distance(System.Drawing.PointF p1, System.Drawing.PointF p2)
        {
            return (float)Math.Sqrt((float)((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y)));
        }

        private bool RANSAC(Mat img, out CircleF circle)
        {
            int max_iterations = 1000;

            circle = new CircleF(new System.Drawing.PointF(-1, -1), -1.0f);

            Mat bin_img = new Mat();
            Mat bin_img_inv = new Mat();
            CvInvoke.Threshold(img, bin_img, 127.0, 255.0, ThresholdType.Binary);
            CvInvoke.Threshold(img, bin_img_inv, 127, 255, ThresholdType.BinaryInv);
            // erode ?

            List<System.Drawing.PointF> edge_positions = getPointPositions(bin_img);
            if (edge_positions.Count < 50) return false;

            Mat dt = new Mat();
            CvInvoke.DistanceTransform(bin_img_inv, dt, null, DistType.L1, 3); // Udaljenost svih pixela do najbližih iznosa 0 (do ruba kruga)

            bool circle_found = false;
            float max_found_perc = 0.9f; // Treba više od 80 % da bi smatrali da je krug pronađen
            int nIterations = 0;

            while (true)
            {
                // RANSAC: randomly choose 3 points and create a circle:
                // TODO: choose randomly but more intelligent, 
                // so that it is more likely to choose three points of a circle. 
                // For example if there are many small circles, it is unlikely to randomly choose 3 points of the same circle.
                int idx1 = rnd.Next(0, edge_positions.Count);
                int idx2 = rnd.Next(0, edge_positions.Count);
                int idx3 = rnd.Next(0, edge_positions.Count);

                // We need 3 different samples:
                if (idx1 == idx2) continue;
                if (idx1 == idx3) continue;
                if (idx3 == idx2) continue;

                // Create circle from 3 points:
                System.Drawing.PointF c;
                float r;
                getCircle(edge_positions[idx1], edge_positions[idx2], edge_positions[idx3], out c, out r);

                // Verify or falsify the circle by inlier counting:
                float cPerc = verifyCircle(dt, c, r);

                if (cPerc > max_found_perc)
                {
                    circle_found = true;
                    max_found_perc = cPerc;
                    circle.Center = c;
                    circle.Radius = r;
                    if (cPerc > 0.95f) break;
                }

                nIterations++;
                if (nIterations > max_iterations) break;
            }
            return circle_found;
        }

        private float verifyCircle(Mat dt, System.Drawing.PointF center, float radius)
        {
            uint counter = 0;
            uint inlier = 0;
            float minInlierDist = 2.0f;
            float maxInlierDistMax = 100.0f;
            float maxInlierDist = radius / 50.0f; // was 25.0f

            if (maxInlierDist < minInlierDist) maxInlierDist = minInlierDist;
            if (maxInlierDist > maxInlierDistMax) maxInlierDist = maxInlierDistMax;

            // Choose samples along the circle and count inlier percentage
            for (float t = 0; t < 2 * (float)Math.PI; t += 0.05f)
            {
                counter++;

                // Točka na kružnici:
                float cX = radius * (float)Math.Cos(t) + center.X;
                float cY = radius * (float)Math.Sin(t) + center.Y;

                if (cX < dt.Cols && cX >= 0 && cY < dt.Rows && cY >= 0)
                {
                    // Udaljenost točke na kružnici do detektiranog ruba manja od praga:
                    if (dt.GetData((int)cY, (int)cX)[0] < maxInlierDist) inlier++;
                }
            }
            return (float)inlier / (float)counter;
        }

        private void getCircle(System.Drawing.PointF p1, System.Drawing.PointF p2, System.Drawing.PointF p3, out System.Drawing.PointF center, out float radius)
        {
            float x1 = p1.X;
            float x2 = p2.X;
            float x3 = p3.X;

            float y1 = p1.Y;
            float y2 = p2.Y;
            float y3 = p3.Y;

            center = new System.Drawing.PointF();

            center.X = (x1 * x1 + y1 * y1) * (y2 - y3) + (x2 * x2 + y2 * y2) * (y3 - y1) + (x3 * x3 + y3 * y3) * (y1 - y2);
            center.X /= (2 * (x1 * (y2 - y3) - y1 * (x2 - x3) + x2 * y3 - x3 * y2));

            center.Y = (x1 * x1 + y1 * y1) * (x3 - x2) + (x2 * x2 + y2 * y2) * (x1 - x3) + (x3 * x3 + y3 * y3) * (x2 - x1);
            center.Y /= (2 * (x1 * (y2 - y3) - y1 * (x2 - x3) + x2 * y3 - x3 * y2));

            radius = (float)Math.Sqrt((center.X - x1) * (center.X - x1) + (center.Y - y1) * (center.Y - y1));
        }

        private List<System.Drawing.PointF> getPointPositions(Mat mat)
        {
            List<System.Drawing.PointF> point_positions = new List<System.Drawing.PointF>();
            for (int y = 0; y < mat.Rows; y++)
            {
                for (int x = 0; x < mat.Cols; x++)
                {
                    if (mat.GetData(y, x)[0] > 0) point_positions.Add(new System.Drawing.PointF(x, y));
                }
            }
            return point_positions;
        }

        private bool FindCircleByAvg(Mat img, out CircleF circle)
        {
            int x_sum = 0, y_sum = 0, cnt = 0;
            for (int y = 0; y < img.Rows; y++)
            {
                for (int x = 0; x < img.Cols; x++)
                {
                    if (img.GetData(y, x)[0] > 127)
                    {
                        x_sum += x;
                        y_sum += y;
                        cnt++;
                    }
                }
            }
            float xc = (float)x_sum / cnt;
            float yc = (float)y_sum / cnt;
            circle = new CircleF(new System.Drawing.PointF(xc, yc), 0);
            return true;
        }

        private bool FindCircleByBoundingRect(Mat img, out CircleF circle)
        {
            int xmin = Int32.MaxValue, xmax = Int32.MinValue;
            int ymin = Int32.MaxValue, ymax = Int32.MinValue;

            List<System.Drawing.PointF> point_positions = new List<System.Drawing.PointF>();

            int k = ((img.Width + img.Height) / 2 + 399) / 400; // 0-400 = 1, 401-800 = 2

            for (int y = 0; y < img.Rows; y+=k)
            {
                for (int x = 0; x < img.Cols; x+=k)
                {
                    if (img.GetData(y, x)[0] > 0)
                    {
                        point_positions.Add(new System.Drawing.PointF(x, y));
                        if (x < xmin) xmin = x;
                        else if (x > xmax) xmax = x;
                        if (y < ymin) ymin = y;
                        else if (y > ymax) ymax = y;
                    }
                }
            }

            float xc = (float)(xmin + xmax) / 2;
            float yc = (float)(ymin + ymax) / 2;
            System.Drawing.PointF center = new System.Drawing.PointF(xc, yc);
            float r = ((ymax - ymin) + (xmax - xmin)) / 4;

            // Calculate mean distance between provided edge points and estimated circle’s edge
            float meanDistance = 0;

            for (int i = 0; i < point_positions.Count; i++)
            {
                meanDistance += (float)Math.Abs(Distance(center, point_positions[i]) - r);
            }
            meanDistance /= point_positions.Count;

            //float maxDistance = Math.Max(3.0f, r / 50);
            float maxDistance = 20.0f;

            circle = new CircleF(center, r);
            return (meanDistance <= maxDistance);
        }

        private bool FindCircleByDistanceTransform(Mat img, out CircleF circle)
        {
            circle = new CircleF();

            Mat bin_img_inv = new Mat();
            CvInvoke.Threshold(img, bin_img_inv, 127, 255, ThresholdType.BinaryInv);

            Mat dt = new Mat();
            CvInvoke.DistanceTransform(bin_img_inv, dt, null, DistType.L1, 3); // Udaljenost svih pixela do najbližih iznosa 0 (do ruba kruga)

            double[] minValues, maxValues;
            System.Drawing.Point[] minLocations, maxLocations;

            dt.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);

            if (maxValues.Length > 0)
            {
                circle.Center = maxLocations[0];
                circle.Radius = (float)maxValues[0];
                return true;
            }
            return false;
        }
        
        private unsafe bool findCircleByGrid(Mat img, out CircleF circle)
        {
            circle = new CircleF(new System.Drawing.PointF(0, 0), 0.0f);

            //List<System.Drawing.PointF> edge_positions = new List<System.Drawing.PointF>();
            //byte* ptr;
            //ptr = (byte*)img.DataPointer.ToPointer();

            //for (int y = 0; y < img.Rows; y+=50) // Horizontalne pruge (svaki 50-i red prođe cijeli
            //{
            //    for (int x = 1; x < img.Cols; x++)
            //    {
            //        //if ((img.GetData(y, x)[0] - img.GetData(y, x-1)[0]) > 40)
            //        if (Math.Abs(ptr[img.Step * y + x] - ptr[img.Step * y + x-1]) > 10)
            //        {
            //            edge_positions.Add(new System.Drawing.PointF(x, y));
            //        }
            //    }
            //}

            List<System.Drawing.Point> edge_positions = new List<System.Drawing.Point>();
            edge_positions = findEdgesWithGridSimple(img, 200, 120);
            drawPoints(colorcurframe, edge_positions);

            if (edge_positions.Count < 10) return false;

            int max_iterations = edge_positions.Count * 10;

            //bool circle_found = false;
            //float max_found_perc = 0.9f; // Treba više od 90 % da bi smatrali da je krug pronađen
            int nIterations = 0;
            float acc, dist;
            int skart;

            while (true)
            {
                // RANSAC: randomly choose 3 points and create a circle:
                // TODO: choose randomly but more intelligent, 
                // so that it is more likely to choose three points of a circle. 
                // For example if there are many small circles, it is unlikely to randomly choose 3 points of the same circle.
                int idx1 = rnd.Next(0, edge_positions.Count);
                int idx2 = rnd.Next(0, edge_positions.Count);
                int idx3 = rnd.Next(0, edge_positions.Count);

                // We need 3 different samples:
                if (idx1 == idx2) continue;
                if (idx1 == idx3) continue;
                if (idx3 == idx2) continue;

                // Create circle from 3 points:
                System.Drawing.PointF c;
                float r;
                getCircle(edge_positions[idx1], edge_positions[idx2], edge_positions[idx3], out c, out r);

                // Verify or falsify the circle by inlier counting:
                acc = 0.0f;
                skart = 0;
                foreach (var p in edge_positions)
                {
                    dist = Math.Abs(Distance(p, c) - r);
                    if (dist < 10) // Ako nije škart točka (udaljena cca 1.9 mm)
                    {
                        acc += dist;
                    }
                    else // ako je predaleko vjerojatno nije na krugu
                    {
                        skart++;
                    }
                }
                float avg_error = acc / (float)(edge_positions.Count - skart);

                //if (avg_error < edge_positions.Count * 2) -- cili krug mora biti na slici, manje od 10% škart točaka
                if (avg_error < 0.5f && (((float)skart/(float)edge_positions.Count) < 0.2f))
                    if (isPointInImage(c, img.Size))
                        if (isPointInImage(new System.Drawing.PointF(c.X, c.Y+r), img.Size))
                            if (isPointInImage(new System.Drawing.PointF(c.X+r, c.Y), img.Size))
                                if (isPointInImage(new System.Drawing.PointF(c.X-r, c.Y), img.Size))
                                    if (isPointInImage(new System.Drawing.PointF(c.X, c.Y - r), img.Size))
                                    {
                                        circle.Center = c;
                                        circle.Radius = r;
                                        return true; // Mozda zapamtiti najbolji, ali tražiti postoji li još bolji
                                    }

                nIterations++;
                if (nIterations > max_iterations) break;
            }
            return false;
        } // findCircleByGrid

        private bool findCircleByFitEllipse(List<System.Drawing.Point> edge_positions, out CircleF circle)
        {
            circle = new CircleF(new System.Drawing.PointF(0, 0), 0.0f);

            if (edge_positions.Count < 10) return false;
            int cnt = edge_positions.Count;

            VectorOfPoint v = new VectorOfPoint();
            v.Push(edge_positions.ToArray());
            RotatedRect r = CvInvoke.FitEllipse(v);
            circle.Center = r.Center;
            circle.Radius = (r.Size.Width + r.Size.Height) / 4;

            // Verify or falsify the circle by inlier counting:
            float acc = 0.0f, dist;
            int skart = 0;
            List<System.Drawing.Point> edge_positions2 = new List<System.Drawing.Point>(edge_positions.Count);

            foreach (var p in edge_positions)
            {
                dist = Math.Abs(Distance(p, circle.Center) - circle.Radius);
                if (dist < 20) // Ako nije škart točka
                {
                    acc += dist;
                    edge_positions2.Add(p);
                }
                else // ako je predaleko vjerojatno nije na krugu
                {
                    skart++;
                }
            }
            
            float avg_error = acc / (float)(cnt - skart);

            // Ako ima vise od 30% skarta nije pronađen krug
            if ((float)skart / (float)cnt > 0.3f || avg_error > 10) return false;

            // Ako nema škarta i odstupanje je malo krug je pronađen
            else if (skart == 0 && avg_error < 1) return true;

            else
            // traži krug opet, ali na setu s izbačenim škart točkama
            {
                v.Clear();
                v.Push(edge_positions2.ToArray());
                r = CvInvoke.FitEllipse(v);
                circle.Center = r.Center;
                circle.Radius = (r.Size.Width + r.Size.Height) / 4;
            }

            acc = 0.0f;
            skart = 0;
            foreach (var p in edge_positions2)
            {
                dist = Math.Abs(Distance(p, circle.Center) - circle.Radius);
                if (dist < 10) // Ako nije škart točka
                {
                    acc += dist;
                }
                else // ako je predaleko vjerojatno nije na krugu
                {
                    skart++;
                }
            }
            avg_error = acc / (float)(cnt - skart);
            if (skart > 1 || avg_error > thr2) return false;

            return true;
        }

        public unsafe List<System.Drawing.Point> findEdgesWithGrid(Mat sourceImage, int gridDistance, int thresh_min, int thresh_max)
        {
            IntPtr sourceData = sourceImage.DataPointer;
            List<System.Drawing.Point> ZeroCrossX = new List<System.Drawing.Point>();
            List<System.Drawing.Point> ZeroCrossY = new List<System.Drawing.Point>();
            
            // HORIZONTALNO SKENIRANJE
            for (int i = 0; i < (sourceImage.Rows); i += gridDistance)
            {
                int max = -1;
                int min = -1;
                bool max_found = false;
                bool min_found = false;
                for (int j = 2; j < sourceImage.Cols - 3; j++)
                {
                    //Traženje druge derivacije konvolucijskim vektorom [1,-2,1]
                    //int point1 = sourceImage.GetData(i, j - 2)[0];
                    //int point2 = sourceImage.GetData(i, j - 1)[0];
                    //int point4 = sourceImage.GetData(i, j + 1)[0];
                    //int point5 = sourceImage.GetData(i, j + 2)[0];

                    byte* ptr;
                    ptr = (byte*)sourceData.ToPointer();

                    byte point1 = ptr[sourceImage.Step * i + (j - 2)];
                    byte point2 = ptr[sourceImage.Step * i + (j - 1)];
                    // int point3 = ((byte[])sourceArray.GetValue(sourceImage.Step * (j) + i))[0];
                    byte point4 = ptr[sourceImage.Step * i + (j + 1)];
                    byte point5 = ptr[sourceImage.Step * i + (j + 2)];

                    //byte point4 = ((byte[])sourceArray.GetValue(sourceImage.Step * (j - 1) + i))[0];
                    //byte point5 = ((byte[])sourceArray.GetValue(sourceImage.Step * (j - 2) + i))[0];
                    
                    int sum = point1 - point2 - point4 + point5;

                    //slijede provjere za tražanje sjecišta s nulom grafa druge derivacije

                    if ((sum > thresh_max) && !max_found) //čekira se prvi maximum
                    {
                        max = j;
                        max_found = true;
                    }
                    if ((sum < thresh_min) && !min_found) //čekira se prvi minmum
                    {
                        min = j;
                        min_found = true;
                    }
                    if (max_found && min_found) //ako su nađeni prvi minimum i maksimum, slijedi provjera sjecišta s nulom i resetiranje markera max_found i min_found za traženje sljedeće točke
                    {
                        if (max > min)
                        {
                            for (int c = min + 1; c < max; c++)
                            {
                                //int pointl = sourceImage.GetData(i, c - 1)[0];
                                //int pointr = sourceImage.GetData(i, c + 1)[0];

                                int pointl = ptr[sourceImage.Step * i + (c - 1)];
                                int pointr = ptr[sourceImage.Step * i + (c + 1)];

                                if ((pointl <= 0) && (pointr >= 0))
                                {
                                    ZeroCrossX.Add(new System.Drawing.Point(c, i));
                                }
                            }
                        }
                        else if (min > max)
                        {
                            for (int c = max + 1; c < min; c++)
                            {
                                int pointl = ptr[sourceImage.Step * i + (c - 1)];
                                int pointr = ptr[sourceImage.Step * i + (c + 1)];
                                if ((pointl >= 0) && (pointr <= 0))
                                {
                                    ZeroCrossY.Add(new System.Drawing.Point(c, i));
                                }
                            }
                        }
                        ZeroCrossY.Add(new System.Drawing.Point((min+max)/2,i));
                        j = j + 50;
                        max_found = false;
                        min_found = false;
                    }
                }
            }

            // VERTIKALNO SKENIRANJE
            //for (int j = 0; j < (sourceImage.Rows / gridDistance); j = j + gridDistance)
            //{
            //    int max = -1;
            //    int min = -1;
            //    bool max_found = false;
            //    bool min_found = false;
            //    for (int i = 2; i < sourceImage.Cols - 2; i++)
            //    {
            //        //Traženje druge derivacije konvolucijskim vektorom [1,-2,1]
            //        int point1 = ((byte[])sourceArray.GetValue(sourceImage.Step * j + i - 2))[0];
            //        int point2 = ((byte[])sourceArray.GetValue(sourceImage.Step * j + i - 1))[0];
            //        //   int point3 = ((byte[])sourceArray.GetValue(sourceImage.Step * j + i))[0];
            //        int point4 = ((byte[])sourceArray.GetValue(sourceImage.Step * j + i + 1))[0];
            //        int point5 = ((byte[])sourceArray.GetValue(sourceImage.Step * j + i + 2))[0];
            //        int sum = point1 - point2 - point4 + point5;

            //        //slijede provjere za tražanje sjecišta s nulom grafa druge derivacije

            //        if ((sum > thresh_max) && !max_found) //čekira se prvi maximum
            //        {
            //            max = i;
            //            max_found = true;
            //        }
            //        if ((sum < thresh_min) && !min_found) //čekira se prvi minmum
            //        {
            //            min = i;
            //            min_found = true;
            //        }
            //        if (max_found && min_found) //ako su nađeni prvi minimum i maksimum, slijedi provjera sjecišta s nulom i resetiranje markera max_found i min_found za traženje sljedeće točke
            //        {
            //            if (max > min)
            //            {
            //                for (int c = min + 1; c < max; c++)
            //                {
            //                    int pointl = ((byte[])sourceArray.GetValue(sourceImage.Step * j + c - 1))[0];
            //                    int pointr = ((byte[])sourceArray.GetValue(sourceImage.Step * j + c + 1))[0];
            //                    if ((pointl <= 0) && (pointr >= 0))
            //                    {
            //                        ZeroCrossX.Add(new System.Drawing.Point(j, c));
            //                    }
            //                }
            //            }
            //            else if (min > max)
            //            {
            //                for (int c = max + 1; c < min; c++)
            //                {
            //                    int pointl = ((byte[])sourceArray.GetValue(sourceImage.Step * j - 1 + c))[0];
            //                    int pointr = ((byte[])sourceArray.GetValue(sourceImage.Step * j + 1 + c))[0];
            //                    if ((pointl >= 0) && (pointr <= 0))
            //                    {
            //                        ZeroCrossY.Add(new System.Drawing.Point(j, c));
            //                    }
            //                }
            //            }
            //            j = j + 50;
            //            max_found = false;
            //            min_found = false;
            //        }
            //    }
            //}
            List<System.Drawing.Point> rlist = new List<System.Drawing.Point>();
            rlist.AddRange(ZeroCrossX);
            rlist.AddRange(ZeroCrossY);
            return rlist;
        }

        Mat drawPoints(Mat img, List<System.Drawing.Point> points)
        {
            foreach(var p in points)
            {
                CvInvoke.Circle(img, p, 8, new MCvScalar(0, 0, 255), 6);
            }
            return img;
        }

        Mat drawPoints(Mat img, List<System.Drawing.PointF> points)
        {
            foreach (var p in points)
            {
                CvInvoke.Circle(img, new System.Drawing.Point((int)p.X, (int)p.Y), 8, new MCvScalar(0, 0, 255), 6);
            }
            return img;
        }

        public unsafe List<System.Drawing.Point> findEdgesWithGridSimple(Mat sourceImage, int gridDistance, int thresh)
        {
            List<System.Drawing.Point> edge_points = new List<System.Drawing.Point>();
            
            byte point0;
            byte point1;
            byte point2;
            byte point4;
            byte point5;
            byte point6;
            int res;
            int max;

            byte* ptr;
            byte* p;
            ptr = (byte*)sourceImage.DataPointer.ToPointer();
            int iMultByStep;
            //bool foundOneMax;

            // HORIZONTALNO SKENIRANJE
            for (int i = 0; i < (sourceImage.Rows); i += gridDistance)
            {
                iMultByStep = sourceImage.Step * i;
                max = 0;
                for (int j = 3; j < sourceImage.Cols - 4; j++)
                {
                    p = ptr + iMultByStep + j - 3;

                    //point0 = ptr[iMultByStepPlusj - 3];
                    //point1 = ptr[iMultByStepPlusj - 2];
                    //point2 = ptr[iMultByStepPlusj - 1];
                    //point4 = ptr[iMultByStepPlusj + 1];
                    //point5 = ptr[iMultByStepPlusj + 2];
                    //point6 = ptr[iMultByStepPlusj + 3];

                    // Prva derivacija (naći di je najveća, a ne prvo misto di prelazi threshold)
                    //res = Math.Abs((point0 + point1 + point2) - (point4 + point5 + point6));
                    res = Math.Abs( *p + *(p+1) + *(p+2) - *(p+4) - *(p+5) - *(p+6) );
                    //if (res > max && res > thresh) max = res;
                    if (res > thresh)
                    {
                        if (res > max) max = res;
                        else
                        {
                            // Provjera varijance u okolini pronađene točke (j-1,i) [7x5] ?
                            //float mean = 0.0f;
                            //int min_k = (i - 3 < 0 ? 0 : i - 3);
                            //int max_k = (i + 3 <= sourceImage.Rows ? i + 3 : sourceImage.Rows);

                            //for (int k = min_k; k <= max_k; k++) // rows
                            //{
                            //    for (int l = j-3; l <= j+1; l++) // cols
                            //    {
                            //        mean += ptr[k * sourceImage.Step + l];
                            //    }
                            //}
                            //mean /= (max_k - min_k) * 5;

                            //// variance
                            //float variance = 0.0f;
                            //for (int k = min_k; k <= max_k; k++) // rows
                            //{
                            //    for (int l = j - 3; l <= j + 1; l++) // cols
                            //    {
                            //        variance += Math.Abs(ptr[k * sourceImage.Step + l] - mean);
                            //    }
                            //}
                            //if (variance > 100)
                            //{
                                edge_points.Add(new System.Drawing.Point(j - 1, i)); // U prethodnoj točki je bio maksimum
                                j += 50; // Da se ne pronađu dvije bliske točke
                            //}
                        }
                    }
                    else
                    {
                        max = 0;
                    }
                }
            }

            // VERTIKALNO SKENIRANJE
            for (int j = 0; j < (sourceImage.Cols); j += gridDistance)
            {
                //iMultByStep = sourceImage.Step * i;
                max = 0;
                for (int i = 3; i < sourceImage.Rows - 4; i++)
                {
                    //p = ptr + iMultByStep + j - 3;

                    point0 = ptr[sourceImage.Step * (i - 3) + j];
                    point1 = ptr[sourceImage.Step * (i - 2) + j];
                    point2 = ptr[sourceImage.Step * (i - 1) + j];
                    point4 = ptr[sourceImage.Step * (i + 1) + j];
                    point5 = ptr[sourceImage.Step * (i + 2) + j];
                    point6 = ptr[sourceImage.Step * (i + 3) + j];

                    // Prva derivacija (naći di je najveća, a ne prvo misto di prelazi threshold)
                    res = Math.Abs((point0 + point1 + point2) - (point4 + point5 + point6));
                    //res = Math.Abs(*p + *(p + 1) + *(p + 2) - *(p + 4) - *(p + 5) - *(p + 6));
                    //if (res > max && res > thresh) max = res;
                    if (res > thresh)
                    {
                        if (res > max) max = res;
                        else
                        {
                            // Provjera varijance u okolini pronađene točke [7x5] ?

                            edge_points.Add(new System.Drawing.Point(j, i - 1)); // U prethodnoj točki je bio maksimum
                            i += 50; // Da se ne pronađu dvije bliske točke
                        }
                    }
                    else
                    {
                        max = 0;
                    }
                }
            }

            // Zeznuto, ali možda može pomoći
            // Gleda se udaljenost svake točke prema svim ostalim, one koje su usamljene se izbacuju
            List<System.Drawing.Point> edge_points2 = new List<System.Drawing.Point>(edge_points.Count);
            for (int i = 0; i < edge_points.Count; i++)
            {
                for (int j = 0; j < edge_points.Count; j++)
                {
                    if (i == j) continue;
                    if (Distance(edge_points[i], edge_points[j]) < gridDistance*Math.Sqrt(2))
                    {
                        // Blizu su, ok
                        edge_points2.Add(edge_points[i]);
                        break;
                    }
                }
            }

            return edge_points2;
        }

        private void bestFitLine(List<System.Drawing.Point> points, out System.Drawing.PointF avg, out float k)
        {
            // Best fit line kroz sve točke
            // Nađi srednju vrijednost X i Y
            float avgX = 0.0f, avgY = 0.0f;
            foreach (var p in points)
            {
                avgX += p.X;
                avgY += p.Y;
            }
            avgX /= (float)points.Count;
            avgY /= (float)points.Count;

            float acc1 = 0.0f, acc2 = 0.0f;
            foreach (var p in points)
            {
                acc1 += (p.X - avgX) * (p.Y - avgY);
                acc2 += (p.X - avgX) * (p.X - avgX);
            }
            avg = new System.Drawing.PointF(avgX, avgY);
            k = acc1 / acc2; // y-avgY = slope * (x-avgX) -> y = avgY + slope * (x-avgX)
        }

        private void twoPointsToImplEq(System.Drawing.PointF p1, System.Drawing.PointF p2, out float A, out float B, out float C)
        {
            A = (p2.Y -p1.Y) / (p2.X - p1.X);
            B = -1;
            C = p1.Y - A * p1.X;
        }

        private int findLineByRANSAC(List<System.Drawing.Point> edge_positions, out List<Tuple<System.Drawing.PointF, System.Drawing.PointF>> lines)
        {
            lines = new List<Tuple<System.Drawing.PointF,System.Drawing.PointF>>(3);
            lines.Add(new Tuple<System.Drawing.PointF, System.Drawing.PointF>(new System.Drawing.PointF(), new System.Drawing.PointF()));
            lines.Add(new Tuple<System.Drawing.PointF, System.Drawing.PointF>(new System.Drawing.PointF(), new System.Drawing.PointF()));
            lines.Add(new Tuple<System.Drawing.PointF, System.Drawing.PointF>(new System.Drawing.PointF(), new System.Drawing.PointF()));

            if (edge_positions.Count < 10) return 0;
            float k, yl, A, B, C;
            List<System.Drawing.Point> edge_positions2 = new List<System.Drawing.Point>(edge_positions.Count);

            // Zeznuto, ali možda može pomoći
            // Gleda se udaljenost svake točke prema svim ostalim
            //for (int i = 0; i < edge_positions.Count; i++)
            //{
            //    for (int j = 0; j < edge_positions.Count; j++)
            //    {
            //        if (i == j) continue;
            //        if (Distance(edge_positions[i], edge_positions[j]) < 150)
            //        {
            //            // Blizu su, ok
            //            edge_positions2.Add(edge_positions[i]);
            //            break;
            //        }
            //    }
            //}
            //edge_positions.Clear();
            //edge_positions.AddRange(edge_positions2);

            //if (edge_positions.Count < 9) return 0;

            // Best fit line kroz sve točke
            // Nađi srednju vrijednost X i Y
            System.Drawing.PointF avg;
            bestFitLine(edge_positions, out avg, out k);
            
            // Sjecište s lijevim rubom, x = 0
            yl = avg.Y - k * avg.X; // ps=(0, y1), pe=(avgX, avgY)

            // Oblik jednadžbe pravca zgodan za računanje udaljenosti
            twoPointsToImplEq(avg, new System.Drawing.PointF(0, yl), out A, out B, out C);

            float dist;
            edge_positions2.Clear();

            foreach (var p in edge_positions)
            {
                dist = distancePointToLine(A, B, C, p);
                if (dist < 4) // Ako je skoro na pravcu // was 1 // Možda povećati još ovaj broj jer je to ipak odstupanje od best fit linije
                {
                    edge_positions2.Add(p);
                }
            }

            // Ako je većina točaka na pravcu (više od 80%) na slici je jedna linija, samo opet treba best fit kroz te dobre točke
            if ((float)edge_positions2.Count / (float)edge_positions.Count > 0.8f)
            {
                bestFitLine(edge_positions2, out avg, out k); // Treba li nakon ovoga provjera odstupanja?

                // Sjecište s lijevim rubom, x = 0
                yl = avg.Y - k * avg.X;

                lines[0] = findLineEndPoints(avg, new System.Drawing.PointF(0, yl));
                return 1; // Jedna linija pronađena
            }


            // Traži dvi-tri linije - RANSAC
            int max_iterations = edge_positions.Count * 10;

            int nIterations = 0;
            int lineNum = 0;
            List<System.Drawing.Point> dobre_tocke = new List<System.Drawing.Point>();

            while (true)
            {
                // RANSAC: randomly choose 2 points
                int idx1 = rnd.Next(0, edge_positions.Count);
                int idx2 = rnd.Next(0, edge_positions.Count);

                // We need 2 different samples and distance between points greater then 200
                if (Distance(edge_positions[idx1], edge_positions[idx2]) < 50) continue; // cca. 4 mm

                // Create line from 2 points:
                twoPointsToImplEq(edge_positions[idx1], edge_positions[idx2], out A, out B, out C);

                // Verify or falsify the line (dobra je ako bar 5 točaka 'dobro' leži na njoj)
                int broj_dobrih_tocaka = 0;
                edge_positions2.Clear();
                edge_positions2.AddRange(edge_positions);
                dobre_tocke.Clear();

                foreach (var p in edge_positions)
                {
                    dist = distancePointToLine(A, B, C, p);
                    if (dist < 3) // Ako je na pravcu više-manje // Možda i tu malo veći broj // was 3
                    {
                        broj_dobrih_tocaka++;
                        edge_positions2.Remove(p); // U edge_positions2 ostaju samo točke koje nisu na ovom pravcu
                        dobre_tocke.Add(p);
                    }
                }

                if (broj_dobrih_tocaka >= 20) // was 5
                {
                    // Linija pronađena
                    // Best fit kroz točke koje skoro su na liniji
                    bestFitLine(dobre_tocke, out avg, out k);
                    yl = avg.Y - k * avg.X;
                    lines[lineNum++] = findLineEndPoints(avg, new System.Drawing.PointF(0, yl));

                    //lines[lineNum++] = findLineEndPoints(edge_positions[idx1], edge_positions[idx2]);
                    edge_positions.Clear();
                    edge_positions.AddRange(edge_positions2);
                    
                    //izbriši sve točke koje približno leže na ovom pravcu, da ne smetaju dalje ransac-u
                    twoPointsToImplEq(avg, new System.Drawing.PointF(0, yl), out A, out B, out C);
                    foreach (var p in edge_positions2)
                    {
                        dist = distancePointToLine(A, B, C, p);
                        if (dist < 20) // Ako je skoro na pravcu //
                        {
                            edge_positions.Remove(p);
                        }
                    }


                    // Ako su pronađene 3 linije ili je ostalo manje od 5 točaka -> kraj
                    if (lineNum == 3 || edge_positions.Count < 10) return lineNum;
                }

                nIterations++;
                if (nIterations > max_iterations) return 0;
            }

        }


        private Tuple<System.Drawing.PointF, System.Drawing.PointF> findLineEndPoints(System.Drawing.PointF p1, System.Drawing.PointF p2)
        {
            float avgX, avgY, k;
            System.Drawing.PointF[] coords = new System.Drawing.PointF[4];

            avgX = p1.X;
            avgY = p1.Y;
            k = (p1.Y - p2.Y) / (p1.X - p2.X);

            // Sjecište s lijevim rubom, x = 0
            float yl = avgY - k * avgX;
            coords[0] = new System.Drawing.PointF(0, yl);

            // Sjecište s desnim rubom, x = im_size.Width - 1
            float yr = avgY + k * (currentFrame.Width - 1 - avgX);
            coords[1] = new System.Drawing.PointF(currentFrame.Width - 1, yr);

            // Sjecište s gornjim rubom, y = 0
            float xt = avgX - avgY / k;
            coords[2] = new System.Drawing.PointF(xt, 0);

            // Sjecište s donjim rubom, y = im_size.Height - 1
            float xb = avgX + (currentFrame.Height - 1 - avgY) / k;
            coords[3] = new System.Drawing.PointF(xb, currentFrame.Height - 1);

            // Tražim sjecišta s rubom vidnog polja (ona koja se stvarno vide)
            System.Drawing.PointF pt1 = new System.Drawing.PointF(), pt2 = new System.Drawing.PointF();
            bool pt1set = false;
            if (isPointInImage(coords[0], currentFrame.Size)) { pt1 = coords[0]; pt1set = true; }
            if (isPointInImage(coords[1], currentFrame.Size)) { if (!pt1set) { pt1 = coords[1]; pt1set = true; } else pt2 = coords[1]; }
            if (isPointInImage(coords[2], currentFrame.Size)) { if (!pt1set) { pt1 = coords[2]; pt1set = true; } else pt2 = coords[2]; }
            if (isPointInImage(coords[3], currentFrame.Size)) { if (!pt1set) { pt1 = coords[3]; pt1set = true; } else pt2 = coords[3]; }

            return new Tuple<System.Drawing.PointF, System.Drawing.PointF>(pt1, pt2);
        }

    }
}
