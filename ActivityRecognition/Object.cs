//------------------------------------------------------------------------------
// <summary>
// Description for a tagged object
// </summary>
// <author> Dongyang Yao (dongyang.yao@rutgers.edu) </author>
//------------------------------------------------------------------------------

using System.ComponentModel;

namespace ActivityRecognition
{
    public class Object : INotifyPropertyChanged
    {
        /// <summary>
        /// Defined object types
        /// </summary>
        public enum Objects { Book, Mouse, Cup, Marker, Bowl };

        /// <summary>
        /// Automatically change the display of use status 
        /// </summary>
        public bool IsInUse 
        {
            get { return isInUse; }
            set
            {
                isInUse = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("IsInUse")); 
            }
        }

        /// <summary>
        /// Use status of the object
        /// </summary>
        private bool isInUse;

        /// <summary>
        /// Object name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Tag ID for the object
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// The sum of RSSI for a period
        /// </summary>
        public double RSSIAccumularor;

        /// <summary>
        /// The read times for a period
        /// </summary>
        public int ReadTimes;

        /// <summary>
        /// Automatically change the RSSI in display 
        /// </summary>
        public double RSSI 
        {
            get { return rssi; } 
            set 
            {
                rssi = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("RSSI")); 
            } 
        }

        /// <summary>
        /// RSSI value for the object
        /// </summary>
        private double rssi;

        /// <summary>
        /// Constructor for object
        /// </summary>
        /// <param name="name"></param>
        /// <param name="id"></param>
        public Object(string name, string id)
        {
            Name = name;
            ReadTimes = 0;
            ID = id;
            RSSIAccumularor = 0;
            RSSI = 0;
            IsInUse = false;
        }

        /// <summary>
        /// handler for property changed
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
