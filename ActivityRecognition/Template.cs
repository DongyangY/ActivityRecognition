//------------------------------------------------------------------------------
// <summary>
// Description of a template
// </summary>
// <author> Dongyang Yao (dongyang.yao@rutgers.edu) </author>
//------------------------------------------------------------------------------

using System.Windows;
using System.Windows.Media;

namespace ActivityRecognition
{
    public class Template
    {
        /// <summary>
        /// Template name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The file directory of template data
        /// </summary>
        public string FileDir;

        /// <summary>
        /// The detection window width of template
        /// </summary>
        public int Width;

        /// <summary>
        /// The detectiin window height of template
        /// </summary>
        public int Height;

        /// <summary>
        /// The window slide width in detection
        /// </summary>
        public int Slide_width;

        /// <summary>
        /// The window slide height in detection
        /// </summary>
        public int Slide_height;

        /// <summary>
        /// The 2D matrix of template data
        /// </summary>
        public float[,] Data;

        /// <summary>
        /// The detected top left position of template
        /// </summary>
        public Point TopLeft;

        /// <summary>
        /// The color for template labelling
        /// </summary>
        public SolidColorBrush Brush;

        /// <summary>
        /// The location of template on top view canvas
        /// </summary>
        public Rect location;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="n"></param>
        /// <param name="f"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="sw"></param>
        /// <param name="sh"></param>
        /// <param name="b"></param>
        public Template(string n, string f, int w, int h, int sw, int sh, SolidColorBrush b)
        {
            Name = n;
            FileDir = f;
            Width = w;
            Height = h;
            Slide_width = sw;
            Slide_height = sh;
            Brush = b;
            Data = new float[Height, Width];
            TopLeft = new Point();
            location = new Rect(new Point(0, 0), new Size(0, 0));
        }
    }
}
