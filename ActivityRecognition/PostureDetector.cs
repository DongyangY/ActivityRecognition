using System;
using System.Collections.Generic;
using Microsoft.Kinect;
using Microsoft.Kinect.VisualGestureBuilder;

namespace ActivityRecognition
{
    class PostureDetector
    {
        private readonly string GESTURE_DB = @"Gestures\AR.gbd";

        public VisualGestureBuilderFrameSource vgbFrameSource;
        private VisualGestureBuilderFrameReader vgbFrameReader;

        private int index;
        private System.Windows.Controls.Canvas canvas;

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

            //Console.WriteLine(System.IO.File.Exists(GESTURE_DB));
            using (VisualGestureBuilderDatabase db = new VisualGestureBuilderDatabase(GESTURE_DB))
            {
                vgbFrameSource.AddGestures(db.AvailableGestures);
            }
           
        }

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
                                    //Console.WriteLine("Gesture: {0}, Detected: {1}, Confidence: {2}", gesture.Name, result.Detected, result.Confidence);
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

        private void Source_TrackingIdLost(object sender, TrackingIdLostEventArgs e)
        {
            Console.WriteLine("Id: {0}, Lost", e.TrackingId);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

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
