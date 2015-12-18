//------------------------------------------------------------------------------
// <summary>
// Detect person's posture with joints
// According to pre-trained postures database with visual gesture builder
// </summary>
// <author> Dongyang Yao (dongyang.yao@rutgers.edu) </author>
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Microsoft.Kinect;
using Microsoft.Kinect.VisualGestureBuilder;

namespace ActivityRecognition
{
    class PostureDetector
    {
        /// <summary>
        /// Posture database directory
        /// </summary>
        private readonly string GESTURE_DB = @"Gestures\AR.gbd";

        /// <summary>
        /// Visual gesture builder frame source
        /// </summary>
        public VisualGestureBuilderFrameSource vgbFrameSource;

        /// <summary>
        /// Visual gesture builder frame Reader
        /// </summary>
        private VisualGestureBuilderFrameReader vgbFrameReader;

        /// <summary>
        /// The index of maintained person array in MainWindow.xaml.cs
        /// To update the correct person
        /// </summary>
        private int index;

        /// <summary>
        /// Foreground canvas of top view
        /// </summary>
        private System.Windows.Controls.Canvas canvas;

        /// <summary>
        /// Get or set reader paused status
        /// </summary>
        public bool IsPaused
        {
            get
            {
                return vgbFrameReader.IsPaused;
            }

            set
            {
                if (vgbFrameReader.IsPaused != value)
                {
                    vgbFrameReader.IsPaused = value;
                }
            }
        }

        /// <summary>
        /// Get or set person's tracking id
        /// </summary>
        public ulong TrackingId
        {
            get
            {
                return vgbFrameSource.TrackingId;
            }

            set
            {
                if (vgbFrameSource.TrackingId != value)
                {
                    vgbFrameSource.TrackingId = value;
                }
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="kinectSensor"></param>
        /// <param name="i"></param>
        /// <param name="c"></param>
        public PostureDetector(KinectSensor kinectSensor, int i, System.Windows.Controls.Canvas c)
        {
            index = i;
            canvas = c;

            vgbFrameSource = new VisualGestureBuilderFrameSource(kinectSensor, 0);
            vgbFrameSource.TrackingIdLost += Source_TrackingIdLost;

            vgbFrameReader = vgbFrameSource.OpenReader();
            if (vgbFrameReader != null)
            {
                vgbFrameReader.IsPaused = true;
                vgbFrameReader.FrameArrived += Reader_GestureFrameArrived;
            }

            using (VisualGestureBuilderDatabase db = new VisualGestureBuilderDatabase(GESTURE_DB))
            {
                vgbFrameSource.AddGestures(db.AvailableGestures);
            }
           
        }

        /// <summary>
        /// Callback when posture is detected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Reader_GestureFrameArrived(object sender, VisualGestureBuilderFrameArrivedEventArgs e)
        {
            VisualGestureBuilderFrameReference frameReference = e.FrameReference;
            using (VisualGestureBuilderFrame frame = frameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    IReadOnlyDictionary<Gesture, DiscreteGestureResult> discreteResults = frame.DiscreteGestureResults;

                    if (discreteResults != null)
                    {
                        Dictionary<string, float> gestures = new Dictionary<string, float>();

                        MainWindow.persons[index].postures.Clear();

                        foreach (Gesture gesture in vgbFrameSource.Gestures)
                        {
                            if (gesture.GestureType == GestureType.Discrete)
                            {
                                DiscreteGestureResult result = null;
                                discreteResults.TryGetValue(gesture, out result);

                                if (result != null)
                                {
                                    if (result.Detected) {
                                        gestures.Add(gesture.Name, result.Confidence);
                                        MainWindow.persons[index].postures.AddLast(new Posture(gesture.Name));
                                    }
                                }
                            }
                        }

                        //Plot.DrawGesturesOnCanvas(MainWindow.persons[index], gestures, canvas);
                    }
                }
            }
        }

        /// <summary>
        /// Callback when person is becoming not tracked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Source_TrackingIdLost(object sender, TrackingIdLostEventArgs e)
        {
            Console.WriteLine("Id: {0}, Lost", e.TrackingId);
        }

        /// <summary>
        /// Dispose method
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose instances
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (vgbFrameReader != null)
                {
                    vgbFrameReader.FrameArrived -= Reader_GestureFrameArrived;
                    vgbFrameReader.Dispose();
                    vgbFrameReader = null;
                }

                if (vgbFrameSource != null)
                {
                    vgbFrameSource.TrackingIdLost -= Source_TrackingIdLost;
                    vgbFrameSource.Dispose();
                    vgbFrameSource = null;
                }
            }
        }

    }
}
