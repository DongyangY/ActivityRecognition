//------------------------------------------------------------------------------
// <summary>
// Dynamically detect furnitures, e.g., desk, using templating method
// </summary>
// <author> Dongyang Yao (dongyang.yao@rutgers.edu) </author>
//------------------------------------------------------------------------------

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
        /// <summary>
        /// Lowest height for segmentation
        /// </summary>
        public static float heightLow;

        /// <summary>
        /// Highest height for segementation
        /// </summary>
        public static float heightHigh;

        /// <summary>
        /// Canvas width of top view 
        /// </summary>
        public static double canvas_width;

        /// <summary>
        /// Canvas height of top view
        /// </summary>
        public static double canvas_height;

        /// <summary>
        /// Canvas for environment
        /// </summary>
        public static System.Windows.Controls.Canvas canvas_environment;

        /// <summary>
        /// Detection area width
        /// </summary>
        public static int area_width = 900;

        /// <summary>
        /// Detection area height
        /// </summary>
        public static int area_height = 450;

        /// <summary>
        /// 2D matrix for area height data
        /// </summary>
        public static float[,] area;

        /// <summary>
        /// 1D array for area height data 
        /// </summary>
        public static byte[] pixels;

        /// <summary>
        /// Is drawing height view done
        /// </summary>
        public static bool isDrawDone;

        /// <summary>
        /// Is currently calculating the template
        /// </summary>
        public static bool isProcessing;

        /// <summary>
        /// List of templates for detection
        /// </summary>
        public static LinkedList<Template> templates;

        /// <summary>
        /// The extended area from template for activity recognition
        /// </summary>
        public static int extension_area = 100;

        /// <summary>
        /// Search templates in background thread
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void DoInBackgrond(object sender, DoWorkEventArgs e)
        {
            isProcessing = true;

            area = new float[area_height, area_width];
            pixels = new byte[area_width * area_height];

            for (int i = 0; i < MainWindow.cameraSpacePoints.Length; i++) 
            {
                CameraSpacePoint point = Transformation.RotateBackFromTilt(MainWindow.TILT_ANGLE, true, MainWindow.cameraSpacePoints[i]);

                // Segment in Height
                if (point.Y >= heightLow && point.Y <= heightHigh)
                {
                    MainWindow.segmentedDepthFramePixels[i] = ushort.MaxValue;
                }

                // Get the detection area height data
                if (!float.IsNegativeInfinity(point.X) && !float.IsNegativeInfinity(point.Y) && !float.IsNegativeInfinity(point.Z))
                {
                    Point planePoint = Transformation.ConvertGroundSpaceToPlane(point);

                    float height = point.Y * 100;

                    //using (StreamWriter writer = new StreamWriter("area.txt", true))
                    //{
                    //    writer.WriteLine("{0},{1},{2}", -point.X * 100, point.Z * 100, height);
                    //}

                    // Discard the ceil and underground
                    if (height < -50 && height > -255) 
                    {
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

            // Search template
            foreach (Template t in templates)
            {
                detectTemplate(t);
            }
            
            isDrawDone = true;
        
        }

        /// <summary>
        /// Load templates from file
        /// </summary>
        /// <param name="listBox"></param>
        public static void loadTemplate(System.Windows.Controls.ListBox listBox)
        {
            templates = new LinkedList<Template>();

            // Add templates
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

        /// <summary>
        /// Find minimum error position for template
        /// </summary>
        /// <param name="t"></param>
        public static void detectTemplate(Template t)
        {
            int tl_x = 0;
            int tl_y = 0;

            float distance = float.MaxValue;
            while (tl_y + t.Height <= area_height)
            {      
                while (tl_x + t.Width <= area_width)
                {
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
        }

        /// <summary>
        /// Calculate the error for one position
        /// </summary>
        /// <param name="tl_x"></param>
        /// <param name="tl_y"></param>
        /// <param name="t"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Progress on UI thread 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void OnProgress(object sender, ProgressChangedEventArgs e)
        {

        }

        /// <summary>
        /// Post execute on UI thread
        /// Update the template location on top view canvas
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void OnPostExecute(object sender, RunWorkerCompletedEventArgs e)
        {
            canvas_environment.Children.Clear();

            foreach (Template t in templates)
            {
                Point canvasPoint = Transformation.ConvertGroundPlaneToCanvas(new Point(t.TopLeft.X - area_width / 2, t.TopLeft.Y + t.Height), canvas_width, canvas_height);
                Plot.DrawRectangle(t.Width, t.Height, canvasPoint.X, canvasPoint.Y, t.Brush, canvas_environment);

                t.location = new Rect(new Point(canvasPoint.X - extension_area / 2, canvasPoint.Y - extension_area / 2), 
                            new Size(t.Width + extension_area, t.Height + extension_area));
            }
            
            MainWindow.isHeightSegmented = true;
            isProcessing = false;
        }

    }
}
