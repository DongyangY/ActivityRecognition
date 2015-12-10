using System.ComponentModel;

namespace ActivityRecogintion
{
    public class Object : INotifyPropertyChanged
    {
        private bool isInUse;
        private double rssi;

        public enum Objects { Book, Mouse, Cup, Marker, Bowl };
        public bool IsInUse 
        {
            get { return isInUse; }
            set
            {
                isInUse = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("IsInUse")); 
            }
        }
        public string Name { get; set; }
        public string ID { get; set; }
        public double RSSIAccumularor;
        public int ReadTimes;
        public double RSSI 
        {
            get { return rssi; } 
            set 
            {
                rssi = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("RSSI")); 
            } 
        }

        public Object(string name, string id)
        {
            Name = name;
            ReadTimes = 0;
            ID = id;
            RSSIAccumularor = 0;
            RSSI = 0;
            IsInUse = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
