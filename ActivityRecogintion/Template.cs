using System.Windows;
using System.Windows.Media;

namespace ActivityRecogintion
{
    public class Template
    {
        public string Name { get; set; }
        public string FileDir;
        public int Width;
        public int Height;
        public int Slide_width;
        public int Slide_height;
        public float[,] Data;
        public Point TopLeft;
        public SolidColorBrush Brush;

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
        }
    }
}
