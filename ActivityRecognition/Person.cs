namespace ActivityRecognition
{
    public class Person
    {
        public bool IsTracked;
        public ulong ID;
        public System.Windows.Point Position;
        public BodyOrientation.Orientations Orientation;
        public System.Windows.Media.Brush Color;
        public string Activities;

        public Person()
        {
            IsTracked = false;
            ID = 0;
            Position = new System.Windows.Point(0.0, 0.0);
            Orientation = BodyOrientation.Orientations.Front;
            Color = System.Windows.Media.Brushes.Red;
            Activities = "";
        }
    }
}
