//------------------------------------------------------------------------------
// <summary>
// Determine object use status 
// Using RSSI value of each tagged object from RFID reader
// </summary>
// <author> 
// Dongyang Yao (dongyang.yao@rutgers.edu) 
// </author>
//------------------------------------------------------------------------------

using Impinj.OctaneSdk;
using System;
using System.Timers;
using System.IO;
using System.Collections.Generic;

namespace ActivityRecognition
{
    public class ObjectDetector
    {
        /// <summary>
        /// All defined objects' status
        /// Updated when RFID reader callbacks
        /// Read by controller for activity recognition
        /// </summary>
        public static Dictionary<Object.Objects, Object> Objects;

        /// <summary>
        /// To stop RFID from another thread
        /// </summary>
        public static bool IsOpenRFID = false;

        /// <summary>
        /// RFID reader reference
        /// </summary>
        public ImpinjReader Reader;

        /// <summary>
        /// Timer to update objects' status
        /// </summary>
        private Timer ObjectUpdate;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="listBox"></param>
        /// <param name="listView"></param>
        public ObjectDetector(System.Windows.Controls.ListBox listBox, System.Windows.Controls.ListView listView)
        {
            Objects = new Dictionary<Object.Objects, Object>();

            // Add tag IDs attched to objects
            Objects.Add(Object.Objects.Book, new Object(Object.Objects.Book.ToString(), "0000 0000 0000 0000 0000 001E"));
            Objects.Add(Object.Objects.Bowl, new Object(Object.Objects.Bowl.ToString(), "0000 0000 0000 0000 0000 0017"));
            Objects.Add(Object.Objects.Cup, new Object(Object.Objects.Cup.ToString(), "0000 0000 0000 0000 0000 0016"));
            Objects.Add(Object.Objects.Marker, new Object(Object.Objects.Marker.ToString(), "0000 0000 0000 0000 0000 0010"));
            Objects.Add(Object.Objects.Mouse, new Object(Object.Objects.Mouse.ToString(), "0000 0000 0000 0000 0000 0205"));

            listBox.ItemsSource = Objects;
            listView.ItemsSource = Objects;

            Reader = new ImpinjReader();
            ObjectUpdate = new Timer();
        }

        /// <summary>
        /// Start method from main thread
        /// </summary>
        public void Run()
        {
            try
            {
                Reader.Connect(Properties.Resources.ReaderHost);

                Impinj.OctaneSdk.Settings settings = Reader.QueryDefaultSettings();

                settings.Report.IncludeAntennaPortNumber = true;
                settings.Report.IncludePeakRssi = true;
                settings.Report.IncludeDopplerFrequency = true;
                settings.Report.IncludePhaseAngle = true;
                settings.Report.IncludeLastSeenTime = true;
                settings.Report.IncludeChannel = true;

                settings.ReaderMode = ReaderMode.MaxMiller;
                settings.SearchMode = SearchMode.DualTarget;
                settings.Session = 2;

                settings.Antennas.DisableAll();
                settings.Antennas.GetAntenna(1).IsEnabled = true;
                settings.Antennas.GetAntenna(2).IsEnabled = true;
                settings.Antennas.GetAntenna(3).IsEnabled = true;
                settings.Antennas.GetAntenna(4).IsEnabled = true;

                settings.Antennas.GetAntenna(1).MaxTxPower = true;
                settings.Antennas.GetAntenna(1).MaxRxSensitivity = true;
                settings.Antennas.GetAntenna(2).MaxTxPower = true;
                settings.Antennas.GetAntenna(2).MaxRxSensitivity = true;
                settings.Antennas.GetAntenna(3).MaxTxPower = true;
                settings.Antennas.GetAntenna(3).MaxRxSensitivity = true;
                settings.Antennas.GetAntenna(4).MaxTxPower = true;
                settings.Antennas.GetAntenna(4).MaxRxSensitivity = true;

                Reader.ApplySettings(settings);

                Reader.TagsReported += OnTagsReported;

                Reader.Start();

                ObjectUpdate.Elapsed += new ElapsedEventHandler(UpdateObjectStatus);
                ObjectUpdate.Interval = 1 * 1000;
                ObjectUpdate.Enabled = true;
            }
            catch (OctaneSdkException e)
            {
                Console.WriteLine("Octane SDK exception: {0}", e.Message);
                ErrorHandler.ProcessRFIDConnectionError();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception : {0}", e.Message);
                ErrorHandler.ProcessRFIDConnectionError();
            }
        }

        /// <summary>
        /// Handler for reader connection lost
        /// </summary>
        /// <param name="sender"></param>
        private void Reader_ConnectionLost(object sender)
        {
            Console.WriteLine("connection losted");
        }

        /// <summary>
        /// Handler for reader started
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Reader_ReaderStarted(object sender, ReaderStartedEvent e)
        {
            Console.WriteLine("reader started");
        }

        /// <summary>
        /// Hander for reader stopped
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Reader_ReaderStopped(object sender, ReaderStoppedEvent e)
        {
            Console.WriteLine("reader stoped");
        }

        /// <summary>
        /// Clear the recorded RSSIs for all objects
        /// </summary>
        private void ClearRSSI()
        {
            foreach (KeyValuePair<Object.Objects, Object> obj in Objects)
            {
                obj.Value.RSSIAccumularor = 0;
                obj.Value.ReadTimes = 0;
            }
        }

        /// <summary>
        /// Callback for updating object status
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void UpdateObjectStatus(object source, ElapsedEventArgs e)
        {
            Object mouse =  Objects[Object.Objects.Mouse];
            mouse.RSSI = mouse.RSSIAccumularor / mouse.ReadTimes;
            mouse.IsInUse = (mouse.ReadTimes == 0 || mouse.RSSI >= -55) ? true : false;

            Object cup = Objects[Object.Objects.Cup];
            cup.RSSI = cup.RSSIAccumularor / cup.ReadTimes;
            cup.IsInUse = (cup.ReadTimes == 0 || cup.RSSI >= -65) ? true : false;

            Object bowl = Objects[Object.Objects.Bowl];
            bowl.RSSI = bowl.RSSIAccumularor / bowl.ReadTimes;
            bowl.IsInUse = (bowl.ReadTimes == 0 || bowl.RSSI >= -65) ? true : false;

            Object marker = Objects[Object.Objects.Marker];
            marker.RSSI = marker.RSSIAccumularor / marker.ReadTimes;
            marker.IsInUse = (marker.ReadTimes == 0 || marker.RSSI >= -55) ? true : false;

            Object book = Objects[Object.Objects.Book];
            book.RSSI = book.RSSIAccumularor / book.ReadTimes;
            book.IsInUse = (book.ReadTimes == 0 || book.RSSI >= -60) ? true : false;

            ClearRSSI();
        }

        /// <summary>
        /// RFID reader callback when tag data received
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="report"></param>
        void OnTagsReported(ImpinjReader sender, TagReport report)
        {
            if (!IsOpenRFID) Stop();

            foreach (Tag tag in report)
            {
                Object mouse = Objects[Object.Objects.Mouse];
                if (tag.Epc.ToString() == mouse.ID && tag.AntennaPortNumber == 4)
                {
                    mouse.ReadTimes++;
                    mouse.RSSIAccumularor += tag.PeakRssiInDbm;
                }

                Object cup = Objects[Object.Objects.Cup];
                if (tag.Epc.ToString() == cup.ID && tag.AntennaPortNumber == 4)
                {
                    cup.ReadTimes++;
                    cup.RSSIAccumularor += tag.PeakRssiInDbm;
                }

                Object bowl = Objects[Object.Objects.Bowl];
                if (tag.Epc.ToString() == bowl.ID && tag.AntennaPortNumber == 4)
                {
                    bowl.ReadTimes++;
                    bowl.RSSIAccumularor += tag.PeakRssiInDbm;
                }

                Object marker = Objects[Object.Objects.Marker];
                if (tag.Epc.ToString() == marker.ID && tag.AntennaPortNumber == 3) 
                {
                    marker.ReadTimes++;
                    marker.RSSIAccumularor += tag.PeakRssiInDbm;
                }

                Object book = Objects[Object.Objects.Book];
                if (tag.Epc.ToString() == book.ID && tag.AntennaPortNumber == 4)
                {
                    book.ReadTimes++;
                    book.RSSIAccumularor += tag.PeakRssiInDbm;
                }
            }
        }

        /// <summary>
        /// Stop RFID
        /// </summary>
        public void Stop()
        {
            ObjectUpdate.Close();

            if (Reader.IsConnected)
            {
                try
                {
                    Reader.Stop();
                    Reader.Disconnect();
                }
                catch (System.Threading.ThreadInterruptedException e)
                {
                    Console.WriteLine(e);
                }               
            }            
        }
    }
}
