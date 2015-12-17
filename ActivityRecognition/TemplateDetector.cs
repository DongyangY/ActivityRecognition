using Microsoft.Kinect;
using System.Windows;
using System.IO;
using System;
using System.Windows.Media;
using System.ComponentModel;
using System.Collections.Generic;

namespace ActivityRecognition
{
    public class TemplateDetector
    {
        public static float heightLow;
        public static float heightHigh;
        public static double pointDiameter;
        public static double canvas_width;
        public static double canvas_height;
        public static System.Windows.Controls.Canvas canvas_environment;
        public static Point canvasPoint;
        public static Brush brush;

        public static int area_width = 900;
        public static int area_height = 450;
        public static float[,] area;
        public static byte[] pixels;
        public static bool isDrawDone;
        public static bool isProcessing;

        public static LinkedList<Template> templates;
        public static int extension_area = 100;

        public static void DoInBackgrond(object sender, DoWorkEventArgs e)
        {
            //Console.WriteLine("segmentation thread");

            isProcessing = true;

            area = new float[area_height, area_width];
            pixels = new byte[area_width * area_height];

            for (int i = 0; i < MainWindow.cameraSpacePoints.Length; i++) 
            {
                CameraSpacePoint point = Transformation.RotateBackFromTilt(MainWindow.TiltAngle, true, MainWindow.cameraSpacePoints[i]);

                if (point.Y >= heightLow && point.Y <= heightHigh)
                {
                    MainWindow.segmentedDepthFramePixels[i] = ushort.MaxValue;
                }

                if (!float.IsNegativeInfinity(point.X) && !float.IsNegativeInfinity(point.Y) && !float.IsNegativeInfinity(point.Z))
                {
                    Point planePoint = Transformation.ConvertGroundSpaceToPlane(point);

                    //canvasPoint = Transformation.ConvertGroundPlaneToCanvas(new Point(planePoint.X - pointDiameter / 2, planePoint.Y + pointDiameter / 2), canvas_width, canvas_height);

                    float height = point.Y * 100;

                    //using (StreamWriter writer = new StreamWriter("area.txt", true))
                    //{
                    //    writer.WriteLine("{0},{1},{2}", -point.X * 100, point.Z * 100, height);
                    //}

                    // Discard the ceil and underground
                    if (height < -50 && height > -255) 
                    {
                        //brush = new SolidColorBrush(Color.FromArgb(0, 0, (byte)(height + 255), 0));
                        //(sender as BackgroundWorker).ReportProgress(0);

                        int x = (int) (-point.X * 100) + area_width / 2;
                        int y = (int) (point.Z * 100);

                        if (x >= 0 && x < area_width && y >= 0 && y < area_height)
                        {
                            area[y , x] = height + 255;
                            pixels[x + y * area_width] = (byte)(height + 255);
                        }
                    }
                }
            }

            foreach (Template t in templates)
            {
                detectTemplate(t);
            }
            
            isDrawDone = true;
        
        }

        public static void loadTemplate(System.Windows.Controls.ListBox listBox)
        {
            //Console.WriteLine("load templates");

            templates = new LinkedList<Template>();
            templates.AddLast(new Template("Table", "Table.txt", 150, 70, 30, 20, Brushes.Red));
            //templates.AddLast(new Template("Cart", "Cart.txt", 70, 50, 30, 30, Brushes.Green));

            listBox.ItemsSource = templates;

            foreach (Template t in templates)
            {
                using (StreamReader sr = new StreamReader(t.FileDir))
                {
                    String line;
                    int index = 0;

                    while ((line = sr.ReadLine()) != null)
                    {
                        int x = index % t.Width;
                        int y = index / t.Width;

                        t.Data[y, x] = float.Parse(line);
                        index++;
                    }
                }
            }
        }

        public static void detectTemplate(Template t)
        {
            int tl_x = 0;
            int tl_y = 0;

            float distance = float.MaxValue;
            while (tl_y + t.Height <= area_height)
            {      
                while (tl_x + t.Width <= area_width)
                {
                    //Console.WriteLine("template: {0}, detect: {1}, {2}", t.Name, tl_x, tl_y);
                    float distance_local = compareTemplate(tl_x, tl_y, t);

                    if (distance_local < distance)
                    {
                        distance = distance_local;
                        t.TopLeft.X = tl_x;
                        t.TopLeft.Y = tl_y;
                    }

                    tl_x += t.Slide_width;
                }

                tl_x = 0;
                tl_y += t.Slide_height;
            }

            //Console.WriteLine("template: {0}, top left x: {1}, y: {2}",t.Name, t.TopLeft.X, t.TopLeft.Y);
        }

        public static float compareTemplate(int tl_x, int tl_y, Template t)
        {
            float distance = 0;

            for (int i = 0; i < t.Height; i++)
            {
                for (int j = 0; j < t.Width; j++)
                {
                    distance += Math.Abs(t.Data[i, j] - area[tl_y + i, tl_x + j]);
                }
            }

            return distance;
        }


        public static void OnProgress(object sender, ProgressChangedEventArgs e)
        {
            // Too big workload on UI
            Plot.DrawEllipse(pointDiameter, pointDiameter, canvasPoint.X, canvasPoint.Y, brush, canvas_environment);
        }


        public static void OnPostExecute(object sender, RunWorkerCompletedEventArgs e)
        {
            //Console.WriteLine("segmentation done");

            canvas_environment.Children.Clear();

            foreach (Template t in templates)
            {
                Point canvasPoint = Transformation.ConvertGroundPlaneToCanvas(new Point(t.TopLeft.X - area_width / 2, t.TopLeft.Y + t.Height), canvas_width, canvas_height);
                Plot.DrawRectangle(t.Width, t.Height, canvasPoint.X, canvasPoint.Y, t.Brush, canvas_environment);

                t.location = new Rect(new Point(canvasPoint.X - extension_area / 2, canvasPoint.Y - extension_area / 2), 
                            new Size(t.Width + extension_area, t.Height + extension_area));
            }
            
            //MainWindow.isSegmented = true;
            isProcessing = false;
        }

    }
}
