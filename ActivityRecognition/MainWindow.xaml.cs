//------------------------------------------------------------------------------
// <summary>
// Start and process Kinect frame callbacks, control RFID system
// </summary>
// <author> Dongyang Yao (dongyang.yao@rutgers.edu) </author>
//------------------------------------------------------------------------------

using Microsoft.Kinect;
using Microsoft.Kinect.VisualGestureBuilder;
using System.Collections.Generic;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Input;
using System.Threading;
using System.ComponentModel;

namespace ActivityRecognition
{
    /// <summary>
    /// Entry for the application
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Tilt angle of Kinect
        /// Since V2, we cannot obtain it programmly
        /// </summary>
        public static readonly double TILT_ANGLE = Double.Parse(Properties.Resources.TiltAngle); // Degree

        /// <summary>
        /// Kinect sensor reference
        /// </summary>
        private KinectSensor kinectSensor;

        /// <summary>
        /// Reader for multi-source, e.g., depth, color, body
        /// </summary>
        private MultiSourceFrameReader multiSourceFrameReader;

        /// <summary>
        /// Status of Kinect connection
        /// Used for Kinect connection error handling
        /// </summary>
        bool isKinectConnected = false;

        /// <summary>
        /// Body joints raw info from Kinect body frame
        /// </summary>
        private Body[] bodies;

        /// <summary>
        /// Person info for activity recognition
        /// </summary>
        public static Person[] persons;

        /// <summary>
        /// Defined activities
        /// </summary>
        private LinkedList<Activity> activities; 

        /// <summary>
        /// Binding source property for depth display
        /// </summary>
        public ImageSource DepthSource { get { return depthSource; } }

        /// <summary>
        /// Private variable for depth frame image
        /// </summary>
        private DrawingImage depthSource;

        /// <summary>
        /// Drawing group for depth display
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Stored depth frame pixels
        /// </summary>
        private ushort[] depthFramePixels;

        /// <summary>
        /// Mouse status for activity area selection
        /// </summary>
        private bool isMouseDown = false;

        /// <summary>
        /// Mouse start position for activity area selection
        /// </summary>
        private Point mouseDownPosition;

        /// <summary>
        /// Mouse end position for activity area selection
        /// </summary>
        private Point mouseUpPosition;

        /// <summary>
        /// Status for activity, position, RFID recording
        /// </summary>
        private bool isRecording = false;

        /// <summary>
        /// Turn off this boolean if no RFID system available
        /// Only use Kinect to recognize activity
        /// No object use detection
        /// </summary>
        private bool isRFIDRequired = false;

        /// <summary>
        /// A thread for RFID system
        /// </summary>
        private Thread rfidThread;

        /// <summary>
        /// Detector for object use
        /// </summary>
        private ObjectDetector objectDetector;

        /// <summary>
        /// Timer for checking Kinect connection when starting the application
        /// </summary>
        System.Timers.Timer kinectConnectionCheck;
        private static readonly double KINECT_CONNECTION_CHECK_INTERVAL = 2000; // Millisecond

        /// <summary>
        /// Latency for stop recording
        /// Designed for a leaved person coming back in a short period
        /// </summary>
        System.Timers.Timer recordStop;
        private static readonly double RECORD_STOP_INTERVAL = 5000; // Millisecond

        /// <summary>
        /// Timer for restarting the application
        /// </summary>
        System.Timers.Timer applicationRestart;
        private static readonly double APPLICATION_RESTART_INTERVAL = 1000 * 60 * 60; // Millisecond

        /// <summary>
        /// Check if it is in the record stop period
        /// </summary>
        private bool isStoppingRecord = false;

        /// <summary>
        /// Used for shuting down application in UI thread from a background thread
        /// </summary>
        private bool isDownApplication = false;

        /// <summary>
        /// Posture detectors for each person
        /// </summary>
        private List<PostureDetector> gestureDetectorList;

        /// <summary>
        /// Defined postures using visual gesture builder
        /// </summary>
        public static LinkedList<Posture> postures;

        // Segementation
        private bool isSegmentInHeight;
        public static bool isSegmented;
        public static ushort[] segmentedDepthFramePixels;
        public static CameraSpacePoint[] cameraSpacePoints;
        public ImageSource HeightView { get { return _heightview; } }
        private DrawingImage _heightview;
        private DrawingGroup drawingGroup_heightview;
        private System.Timers.Timer resetEnvironment;

        public MainWindow()
        {
            InitializeComponent();

            // Init
            kinectSensor = KinectSensor.GetDefault();
            bodies = new Body[kinectSensor.BodyFrameSource.BodyCount];
            persons = new Person[kinectSensor.BodyFrameSource.BodyCount];
            activities = new LinkedList<Activity>();
            drawingGroup = new DrawingGroup();
            depthSource = new DrawingImage(drawingGroup);
            drawingGroup_heightview = new DrawingGroup();
            _heightview = new DrawingImage(drawingGroup_heightview);
            DataContext = this;

            depthFramePixels = new ushort[kinectSensor.DepthFrameSource.FrameDescription.Width *
                                         kinectSensor.DepthFrameSource.FrameDescription.Height];

            segmentedDepthFramePixels = new ushort[kinectSensor.DepthFrameSource.FrameDescription.Width *
                                         kinectSensor.DepthFrameSource.FrameDescription.Height];

            Settings.Load(activities);
            Requirement.LoadRequirements(ListBox_Requirement);
            Plot.InitBackgroundCanvas(Canvas_Position_Background);
            TemplateDetector.loadTemplate(ListBox_Area);

            multiSourceFrameReader = kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.Depth);
            multiSourceFrameReader.MultiSourceFrameArrived += MultiSourceFrameReader_MultiSourceFrameArrived;
            kinectSensor.IsAvailableChanged += KinectSensor_IsAvailableChanged;
            kinectSensor.Open();

            kinectConnectionCheck = new System.Timers.Timer();
            kinectConnectionCheck.AutoReset = false;
            kinectConnectionCheck.Elapsed += kinectConnectionCheck_Elapsed;
            kinectConnectionCheck.Interval = KINECT_CONNECTION_CHECK_INTERVAL;
            kinectConnectionCheck.Enabled = true;

            resetEnvironment = new System.Timers.Timer();
            resetEnvironment.AutoReset = true;
            resetEnvironment.Elapsed += ResetEnvironment_Elaped;     
            resetEnvironment.Interval = 10000;  // 10s
            resetEnvironment.Enabled = true;

            applicationRestart = new System.Timers.Timer();

            gestureDetectorList = new List<PostureDetector>();
            postures = new LinkedList<Posture>();
            
            for (int i = 0; i < kinectSensor.BodyFrameSource.BodyCount; ++i)
            {
                PostureDetector detector = new PostureDetector(kinectSensor, i, Canvas_Position_Foreground);
                gestureDetectorList.Add(detector);

                if (i == 0)
                {
                    foreach (Gesture gesture in detector.vgbFrameSource.Gestures)
                    {
                        postures.AddLast(new Posture(gesture.Name));
                    }

                    ListBox_Posture.ItemsSource = postures;
                }
            }
        }

        private void ApplicationRestart_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
            isDownApplication = true;
        }

        private void ResetEnvironment_Elaped(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!isSegmentInHeight && !TemplateDetector.isProcessing)
            {
                isSegmentInHeight = true;
            }
        }

        private void kinectConnectionCheck_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!kinectSensor.IsAvailable)
            {
                ErrorHandler.ProcessDisconnectError();
            }

            ((System.Timers.Timer)sender).Close();
        }

        private void KinectSensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            if (!isKinectConnected && e.IsAvailable)
            {
                isKinectConnected = true;
                //Console.WriteLine("connected");
                ErrorHandler.ProcessConnectNotification();
            }
            else if (isKinectConnected && !e.IsAvailable)
            {
                isKinectConnected = false;
                //Console.WriteLine("disconnected");

                //ErrorHandler.ProcessDisconnectError();
            }
        }

        // Handler for each arrived frame
        private void MultiSourceFrameReader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            if (isDownApplication) Application.Current.Shutdown();

            MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();
          
            if (multiSourceFrame != null)
            {
                using (DepthFrame depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame())
                {
                    using (BodyFrame bodyFrame = multiSourceFrame.BodyFrameReference.AcquireFrame())
                    {
                        using (DrawingContext drawingContext = drawingGroup.Open())
                        {
                            if (depthFrame != null && bodyFrame != null)
                            {
                                // Refresh
                                Plot.RefreshForegroundCanvas(Canvas_Position_Foreground, activities);

                                // Segment in height
                                if (isSegmentInHeight)
                                {
                                    ushort[] depthFrameData = new ushort[depthFrame.FrameDescription.Height * depthFrame.FrameDescription.Width];
                                    depthFrame.CopyFrameDataToArray(depthFrameData);

                                    cameraSpacePoints = new CameraSpacePoint[depthFrame.FrameDescription.Height * depthFrame.FrameDescription.Width];
                                    kinectSensor.CoordinateMapper.MapDepthFrameToCameraSpace(depthFrameData, cameraSpacePoints);

                                    //Console.WriteLine(depthFrame.FrameDescription.Height * depthFrame.FrameDescription.Width);

                                    TemplateDetector.heightLow = -2.4f;
                                    TemplateDetector.heightHigh = -1.9f;
                                    TemplateDetector.pointDiameter = 4;
                                    TemplateDetector.canvas_width = Canvas_Position_Background.Width;
                                    TemplateDetector.canvas_height = Canvas_Position_Background.Height;
                                    TemplateDetector.canvas_environment = Canvas_Position_Environment;

                                    //Console.WriteLine("segmentation start");
                                    BackgroundWorker worker = new BackgroundWorker();
                                    worker.WorkerReportsProgress = true;
                                    worker.DoWork += TemplateDetector.DoInBackgrond;
                                    worker.ProgressChanged += TemplateDetector.OnProgress;
                                    worker.RunWorkerCompleted += TemplateDetector.OnPostExecute;
                                    worker.RunWorkerAsync();

                                    isSegmentInHeight = false;
                                }                            

                                // Load and display depth frame     

                                if (!isSegmented)
                                {
                                    drawingContext.DrawImage(Transformation.ToBitmap(depthFrame, depthFramePixels, true),
                                                        new Rect(0.0, 0.0, kinectSensor.DepthFrameSource.FrameDescription.Width, kinectSensor.DepthFrameSource.FrameDescription.Height));
                                }
                                else
                                {
                                    drawingContext.DrawImage(Transformation.ToBitmap(depthFrame, segmentedDepthFramePixels, false),
                                                        new Rect(0.0, 0.0, kinectSensor.DepthFrameSource.FrameDescription.Width, kinectSensor.DepthFrameSource.FrameDescription.Height));
                                }

                                if (TemplateDetector.isDrawDone)
                                {
                                    using (DrawingContext drawingContext_heightview = drawingGroup_heightview.Open())
                                    {
                                        drawingContext_heightview.DrawImage(Transformation.ToBitmap(TemplateDetector.area_width, TemplateDetector.area_height, TemplateDetector.pixels), 
                                                        new Rect(0.0, 0.0, TemplateDetector.area_width, TemplateDetector.area_height));

                                        foreach (Template t in TemplateDetector.templates)
                                        {
                                            drawingContext_heightview.DrawRectangle(null, new Pen(t.Brush, 2),
                                            new Rect(new Point(t.TopLeft.X, t.TopLeft.Y), new Size(t.Width, t.Height)));
                                        }
                                        
                                    }

                                    TemplateDetector.isDrawDone = false;
                                }
                               
                                // Load and display each body info 
                                bodyFrame.GetAndRefreshBodyData(bodies);                                                   

                                for (int i = 0; i < kinectSensor.BodyFrameSource.BodyCount; ++i)
                                {
                                    if (persons[i] == null) persons[i] = new Person();
                                    
                                    ulong trackingId = bodies[i].TrackingId;
                                    if (trackingId != gestureDetectorList[i].TrackingId)
                                    {
                                        gestureDetectorList[i].TrackingId = trackingId;
                                        gestureDetectorList[i].IsPaused = trackingId == 0;
                                    }
                                    

                                    if (bodies[i].IsTracked)
                                    {
                                        // Update tracking status
                                        persons[i].IsTracked = true;

                                        persons[i].ID = bodies[i].TrackingId;

                                        persons[i].Color = Plot.BodyColors[i];

                                        // Update position
                                        CameraSpacePoint headPositionCamera = bodies[i].Joints[JointType.Head].Position; // Meter
                                        CameraSpacePoint headPositionGournd = Transformation.RotateBackFromTilt(TILT_ANGLE, true, headPositionCamera);
                                        Transformation.ConvertGroundSpaceToPlane(headPositionGournd, persons[i]);

                                        // Update body orientation
                                        CameraSpacePoint leftShoulderPositionGround = Transformation.RotateBackFromTilt(TILT_ANGLE, true, bodies[i].Joints[JointType.ShoulderLeft].Position);
                                        CameraSpacePoint rightShoulderPositionGround = Transformation.RotateBackFromTilt(TILT_ANGLE, true, bodies[i].Joints[JointType.ShoulderRight].Position);
                                        BodyOrientation.DecideOrientation(leftShoulderPositionGround, rightShoulderPositionGround, persons[i], 
                                                                         Transformation.CountZeroInRec(depthFramePixels, kinectSensor.CoordinateMapper.MapCameraPointToDepthSpace(headPositionCamera), 
                                                                         16, kinectSensor.DepthFrameSource.FrameDescription.Width), Canvas_Position_Foreground);

                                    }
                                    else persons[i].IsTracked = false;
                                }

                                DrawPeopleOnDepth(drawingContext);
                                DrawPeopleOnCanvas();
                                DetermineSystemStatus();
                                DrawSystemStatus();

                                if (isRecording)
                                {                                   
                                    CheckActivity();
                                    DrawActivityOnCanvas();
                                    Record(); 
                                }                      
                            }
                        }                     
                    }
                }
            } 
        }

        public void Record()
        {
            ActivityRecognition.Record.RecordActivity(activities);
            ActivityRecognition.Record.RecordPosition(persons);
        }

        private void DrawSystemStatus()
        {
            Plot.DrawSystemStatus(Canvas_Position_Foreground, isRecording);
        }

        private void RecordStop_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            //Console.WriteLine("timer: stop recording");

            applicationRestart.Enabled = false;

            if (isRFIDRequired)
            {
                ObjectDetector.IsRFIDOpen = false;
                System.Threading.Thread.Sleep(1000);

                //Console.WriteLine(rfid.Reader.IsConnected);

                if (rfidThread != null)
                {
                    rfidThread.Abort();
                    //Console.WriteLine("stoped in KINECT");
                    rfidThread = null;
                }
            }

            if (objectDetector != null) objectDetector = null;

            isRecording = false;
            isStoppingRecord = false;
        }

        private void DetermineSystemStatus()
        {
            // Condition for starting activity recoginition and record
            if (Transformation.GetNumberOfPeople(persons) >= 1)
            {
                if (!isRecording)
                {
                    ActivityRecognition.Record.StartTime = DateTime.Now.ToString("M-d-yyyy_HH-mm-ss");

                    if (objectDetector == null) objectDetector = new ObjectDetector(ListBox_Object, ListView_Object);

                    if (isRFIDRequired)
                    {
                        ObjectDetector.IsRFIDOpen = true;

                        if (rfidThread == null)
                        {
                            rfidThread = new Thread(new ThreadStart(objectDetector.Run));
                            rfidThread.Start();
                        }
                    }

                    applicationRestart.AutoReset = false;
                    applicationRestart.Elapsed += ApplicationRestart_Elapsed;
                    applicationRestart.Interval = APPLICATION_RESTART_INTERVAL;
                    applicationRestart.Enabled = true;

                    isRecording = true;
                }
            }

            if (Transformation.GetNumberOfPeople(persons) < 1)
            {
                if (isRecording)
                {

                    if (!isStoppingRecord)
                    {
                        isStoppingRecord = true;

                        recordStop = new System.Timers.Timer();
                        recordStop.AutoReset = false;
                        recordStop.Elapsed += RecordStop_Elapsed;
                        recordStop.Interval = RECORD_STOP_INTERVAL;
                        recordStop.Enabled = true;
                    }
                }
            }
            else
            {
                if (isStoppingRecord)
                {
                    isStoppingRecord = false;
                    recordStop.Enabled = false;
                }
            }
        }

        private void CheckActivity()
        {
            Activity.DecideActivityForPeople(activities, persons, Canvas_Position_Foreground);
            Activity.DecideStatusOfActivity(activities, persons, Canvas_Position_Foreground);
        }

        private void DrawPeopleOnCanvas()
        {
            foreach (Person person in persons)
            {
                if (person.IsTracked)
                {
                    Plot.DrawPeopleFromKinectOnCanvas(Plot.EllipseDiameter, person.Position.X, person.Position.Y, person.Color, Canvas_Position_Foreground);
                    Plot.DrawOrientationOnCanvas(Plot.EllipseDiameter, Plot.TriangleSideLength, person.Orientation, person.Position.X, person.Position.Y, System.Windows.Media.Brushes.Black, Canvas_Position_Foreground);
                }
            }
        }

        private void DrawActivityOnCanvas()
        {
            foreach (Person person in persons)
            {
                if (person.IsTracked)
                {
                    Plot.DrawActivitiesOnCanvas(person, Canvas_Position_Foreground);
                }
            }
        }

        private void DrawPeopleOnDepth(DrawingContext drawingContext)
        {
            for (int i = 0; i < persons.Length; i++) 
            {
                if (persons[i].IsTracked)
                {
                    LinkedList<CameraSpacePoint> jointPoints = new LinkedList<CameraSpacePoint>();
                    jointPoints.AddLast(bodies[i].Joints[JointType.Head].Position);
                    jointPoints.AddLast(bodies[i].Joints[JointType.ShoulderLeft].Position);
                    jointPoints.AddLast(bodies[i].Joints[JointType.ShoulderRight].Position);
                    Plot.DrawJointsOnDepth(kinectSensor.CoordinateMapper, jointPoints, drawingContext, kinectSensor);
                    Plot.DrawTrackedHead(kinectSensor.CoordinateMapper, persons[i].Color, bodies[i].Joints[JointType.Neck].Position, bodies[i].Joints[JointType.Head].Position, drawingContext, kinectSensor);
                }
            }
        }

        // Dispose unreleased references when exiting
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (multiSourceFrameReader != null) multiSourceFrameReader.Dispose();
            if (kinectSensor != null) kinectSensor.Close();
            if (objectDetector != null)
            {
                if (objectDetector.Reader != null)
                {
                    if (objectDetector.Reader.IsConnected) objectDetector.Stop();
                }
                if (rfidThread != null) rfidThread.Abort();
            }

            
            if (this.gestureDetectorList != null)
            {
                foreach (PostureDetector detector in this.gestureDetectorList)
                {
                    detector.Dispose();
                }

                this.gestureDetectorList.Clear();
                this.gestureDetectorList = null;
            }
            

        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            isMouseDown = true;
            mouseDownPosition = e.GetPosition(Canvas_Position_Foreground);
            Canvas.SetLeft(Rectangle_SelectArea, mouseDownPosition.X);
            Canvas.SetTop(Rectangle_SelectArea, mouseDownPosition.Y);
            Rectangle_SelectArea.Width = 0;
            Rectangle_SelectArea.Height = 0;
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isMouseDown)
            {
                Point currentMousePosition = e.GetPosition(Canvas_Position_Foreground);

                double width = currentMousePosition.X - mouseDownPosition.X;
                double height = currentMousePosition.Y - mouseDownPosition.Y;

                if (width > 0) Rectangle_SelectArea.Width = width;
                else
                {
                    Rectangle_SelectArea.Width = - width;
                    Canvas.SetLeft(Rectangle_SelectArea, currentMousePosition.X);
                }

                if (height > 0) Rectangle_SelectArea.Height = height;
                else
                {
                    Rectangle_SelectArea.Height = - height;
                    Canvas.SetTop(Rectangle_SelectArea, currentMousePosition.Y);
                }
            }
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isMouseDown = false;
            mouseUpPosition = e.GetPosition(Canvas_Position_Foreground);
        }

        private void Button_AddActivity(object sender, RoutedEventArgs e)
        {
            // Add activity
            Rect area = new Rect(new Point(mouseDownPosition.X > mouseUpPosition.X ? mouseUpPosition.X : mouseDownPosition.X,
                                           mouseDownPosition.Y > mouseUpPosition.Y ? mouseUpPosition.Y : mouseDownPosition.Y),
                                 new Size(System.Math.Abs(mouseDownPosition.X - mouseUpPosition.X), System.Math.Abs(mouseDownPosition.Y - mouseUpPosition.Y)));
            string name = TextBox_ActivityName.Text;
            BodyOrientation.Orientations orientations = 0;
            if (CheckBox_OrientationFront.IsChecked == true) orientations |= BodyOrientation.Orientations.Front;
            if (CheckBox_OrientationLeft.IsChecked == true) orientations |= BodyOrientation.Orientations.Left;
            if (CheckBox_OrientationRight.IsChecked == true) orientations |= BodyOrientation.Orientations.Right;
            if (CheckBox_OrientationBack.IsChecked == true) orientations |= BodyOrientation.Orientations.Back;
            int minPeopleCount = ComboxBox_MinPeople.SelectedIndex + 1;
            LinkedList<Object.Objects> objects = new LinkedList<Object.Objects>();
            LinkedList<Requirement> requirements = new LinkedList<Requirement>();
            LinkedList<Posture> pos = new LinkedList<Posture>();
            foreach (KeyValuePair<Object.Objects, Object> pair in ListBox_Object.SelectedItems) objects.AddLast(pair.Key);
            foreach (Requirement req in ListBox_Requirement.SelectedItems) requirements.AddLast(req);
            foreach (Posture p in ListBox_Posture.SelectedItems) pos.AddFirst(p);

            if (ListBox_Area.SelectedIndex == -1)
            {
                activities.AddLast(new Activity(area, orientations, pos, objects, requirements, name, minPeopleCount));
            }
            else
            {
                foreach (Template t in ListBox_Area.SelectedItems)
                {
                    activities.AddLast(new Activity(t.Name, orientations, pos, objects, requirements, name, minPeopleCount));
                }
            }

            Popup_AddActivity.IsOpen = false;

        }

        private void Button_PopupActivity(object sender, RoutedEventArgs e)
        {
            Popup_AddActivity.IsOpen = true;
        }

        private void Button_PopupSetting(object sender, RoutedEventArgs e)
        {
            Popup_Settings.IsOpen = true;
        }

        private void Button_SaveSettings(object sender, RoutedEventArgs e)
        {
            Settings.Save(activities);

            Popup_Settings.IsOpen = false;
        }

        private void Button_ClearSettings(object sender, RoutedEventArgs e)
        {
            activities.Clear();

            Popup_Settings.IsOpen = false;
        }

        private void Button_ShowHeight(object sender, RoutedEventArgs e)
        {
            Popup_Area.IsOpen = true;
        }

    }
}
