using Impinj.OctaneSdk;
using System;
using System.Timers;
using System.IO;
using System.Collections.Generic;

namespace ActivityRecognition
{
    public class ObjectDetector
    {
        // Object info
        public static Dictionary<Object.Objects, Object> Objects;
        public static bool IsRFIDOpen = false;

        // Reader
        public ImpinjReader Reader;

        // Timer
        private Timer updateTimer;

        //private int cnt = 0;

        public ObjectDetector(System.Windows.Controls.ListBox listBox, System.Windows.Controls.ListView listView)
        {
            Objects = new Dictionary<Object.Objects, Object>();
            Objects.Add(Object.Objects.Book, new Object(Object.Objects.Book.ToString(), "0000 0000 0000 0000 0000 001E"));
            Objects.Add(Object.Objects.Bowl, new Object(Object.Objects.Bowl.ToString(), "0000 0000 0000 0000 0000 0017"));
            Objects.Add(Object.Objects.Cup, new Object(Object.Objects.Cup.ToString(), "0000 0000 0000 0000 0000 0016"));
            Objects.Add(Object.Objects.Marker, new Object(Object.Objects.Marker.ToString(), "0000 0000 0000 0000 0000 0010"));
            Objects.Add(Object.Objects.Mouse, new Object(Object.Objects.Mouse.ToString(), "0000 0000 0000 0000 0000 0205"));
            listBox.ItemsSource = Objects;
            listView.ItemsSource = Objects;

            Reader = new ImpinjReader();
            updateTimer = new Timer();
        }

        public void Run()
        {
            try
            {
                // Connect to the reader.
                // Change the ReaderHostname constant in SolutionConstants.cs 
                // to the IP address or hostname of your reader.
                Reader.Connect(Properties.Resources.ReaderHost);

                // Get the default settings
                // We'll use these as a starting point
                // and then modify the settings we're 
                // interested in.
                Impinj.OctaneSdk.Settings settings = Reader.QueryDefaultSettings();

                // Tell the reader to include the antenna number, RSSI, Frequency and Phase
                // in all tag reports. Other fields can be added
                // to the reports in the same way by setting the 
                // appropriate Report.IncludeXXXXXXX property.
                settings.Report.IncludeAntennaPortNumber = true;
                settings.Report.IncludePeakRssi = true;
                settings.Report.IncludeDopplerFrequency = true;
                settings.Report.IncludePhaseAngle = true;
                settings.Report.IncludeLastSeenTime = true;
                settings.Report.IncludeChannel = true;

                // Set the reader mode, search mode and session
                // settings.ReaderMode = ReaderMode.AutoSetDenseReader;
                settings.ReaderMode = ReaderMode.MaxMiller;
                settings.SearchMode = SearchMode.DualTarget;
                settings.Session = 2;

                // Enable all antennas
                settings.Antennas.DisableAll();
                settings.Antennas.GetAntenna(1).IsEnabled = true;
                settings.Antennas.GetAntenna(2).IsEnabled = true;
                settings.Antennas.GetAntenna(3).IsEnabled = true;
                settings.Antennas.GetAntenna(4).IsEnabled = true;

                // Set the Transmit Power and 
                // Receive Sensitivity to the maximum.
                settings.Antennas.GetAntenna(1).MaxTxPower = true;
                settings.Antennas.GetAntenna(1).MaxRxSensitivity = true;
                settings.Antennas.GetAntenna(2).MaxTxPower = true;
                settings.Antennas.GetAntenna(2).MaxRxSensitivity = true;
                settings.Antennas.GetAntenna(3).MaxTxPower = true;
                settings.Antennas.GetAntenna(3).MaxRxSensitivity = true;
                settings.Antennas.GetAntenna(4).MaxTxPower = true;
                settings.Antennas.GetAntenna(4).MaxRxSensitivity = true;
                // You can also set them to specific values like this...
                //settings.Antennas.GetAntenna(1).TxPowerInDbm = 20;
                //settings.Antennas.GetAntenna(1).RxSensitivityInDbm = -70;

                // Apply the newly modified settings.
                Reader.ApplySettings(settings);

                // Assign the TagsReported event handler.
                // This specifies which method to call
                // when tags reports are available.
                Reader.TagsReported += OnTagsReported;

                //Reader.ConnectionLost += Reader_ConnectionLost;
                //Reader.ReaderStarted += Reader_ReaderStarted;
                //Reader.ReaderStopped += Reader_ReaderStopped;

                // Start reading.
                Reader.Start();

                // Start timer
                updateTimer.Elapsed += new ElapsedEventHandler(UpdateObjectStatus);
                updateTimer.Interval = 1 * 1000;
                updateTimer.Enabled = true;


                // Wait for the user to press enter.
                Console.WriteLine("RFID started!");

                //System.Threading.Thread.Sleep(5000);
                //Stop();
            }
            catch (OctaneSdkException e)
            {
                // Handle Octane SDK errors.
                Console.WriteLine("Octane SDK exception: {0}", e.Message);
                ErrorHandler.ProcessRFIDConnectionError();
            }
            catch (Exception e)
            {
                // Handle other .NET errors.
                Console.WriteLine("Exception : {0}", e.Message);
                ErrorHandler.ProcessRFIDConnectionError();
            }
        }

        private void Reader_ConnectionLost(object sender)
        {
            Console.WriteLine("connection losted");
        }

        private void Reader_ReaderStarted(object sender, ReaderStartedEvent e)
        {
            Console.WriteLine("reader started");
        }

        private void Reader_ReaderStopped(object sender, ReaderStoppedEvent e)
        {
            Console.WriteLine("reader stoped");
        }

        private void ClearRSSI()
        {
            foreach (KeyValuePair<Object.Objects, Object> obj in Objects)
            {
                obj.Value.RSSIAccumularor = 0;
                obj.Value.ReadTimes = 0;
            }
        }

        // Update object use status
        private void UpdateObjectStatus(object source, ElapsedEventArgs e)
        {
            //Console.WriteLine("interupt 2");

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

            //using (StreamWriter writer = new StreamWriter("book_4.txt", true))
            //{
            //    Console.WriteLine(cnt++);
            //    //writer.WriteLine("{0} {1} {2} {3} {4}", mouse.IsInUse ? 1 : 0, cup.IsInUse ? 1 : 0, bowl.IsInUse ? 1 : 0, marker.IsInUse ? 1 : 0, book.IsInUse ? 1 : 0);
            //    writer.WriteLine("{0}", book.IsInUse ? 1 : 0);
            //}

            ClearRSSI();
        }

        // Handler
        void OnTagsReported(ImpinjReader sender, TagReport report)
        {
            //Console.WriteLine("interupt 1");

            if (!IsRFIDOpen) Stop();

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

        public void Stop()
        {
            updateTimer.Close();

            if (Reader.IsConnected)
            {
                try
                {
                    Reader.Stop();
                    Console.WriteLine("stoped in RFID");
                    //System.Threading.Thread.Sleep(500);
                    Reader.Disconnect();
                    Console.WriteLine("disconnect in RFID");
                    //System.Threading.Thread.Sleep(500);
                }
                catch (System.Threading.ThreadInterruptedException e)
                {
                    Console.WriteLine(e);
                }
                
            }            
        }
    }
}
