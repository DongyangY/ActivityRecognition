/**
 * @author Dongyang Yao
 * @email dongyang1111yao@gmail.com
 */

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
    public partial class MainWindow : Window
    {
        public static readonly double TiltAngle = 9; // Degree

        // Kinect info
        private KinectSensor kinectSensor;
        private MultiSourceFrameReader multiSourceFrameReader;
        bool isKinectConnected = false;

        // Person info
        private Body[] bodies;
        public static Person[] persons;

        // Activity info
        private LinkedList<Activity> activities; 

        // Depth source info
        public ImageSource DepthSource { get { return _depthSource; } }
        private DrawingImage _depthSource;
        private DrawingGroup drawingGroup;
        private ushort[] depthFramePixels;

        // Activity area selection
        private bool isMouseDown = false;
        private Point mouseDownPosition;
        private Point mouseUpPosition;

        // Record
        private bool isRecordingOn = false;

        // RFID
        private bool isRFIDAvailable = false;
        private Thread rfidThread;
        private ObjectDetector rfid;

        // System
        System.Timers.Timer startConnect;
        System.Timers.Timer stopRecording;
        private static readonly double STOP_RECORDING_INTERVAL = 5000;
        private bool isStopingRecording = false;

        // Gesture
        private List<PostureDetector> gestureDetectorList;
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
            _depthSource = new DrawingImage(drawingGroup);
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

            startConnect = new System.Timers.Timer();
            startConnect.AutoReset = false;
            startConnect.Elapsed += StartConnect_Elapsed;
            startConnect.Interval = 2000;
            startConnect.Enabled = true;

            resetEnvironment = new System.Timers.Timer();
            resetEnvironment.AutoReset = true;
            resetEnvironment.Elapsed += ResetEnvironment_Elaped;     
            resetEnvironment.Interval = 10000;
            resetEnvironment.Enabled = true;


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

        private void ResetEnvironment_Elaped(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!isSegmentInHeight && !TemplateDetector.isProcessing)
            {
                isSegmentInHeight = true;
            }
        }

        private void StartConnect_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
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
                ErrorHandler.ProcessDisconnectError();
            }
        }

        // Handler for each arrived frame
        private void MultiSourceFrameReader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
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

                                    Console.WriteLine(depthFrame.FrameDescription.Height * depthFrame.FrameDescription.Width);

                                    TemplateDetector.heightLow = -2.4f;
                                    TemplateDetector.heightHigh = -1.9f;
                                    TemplateDetector.pointDiameter = 4;
                                    TemplateDetector.canvas_width = Canvas_Position_Background.Width;
                                    TemplateDetector.canvas_height = Canvas_Position_Background.Height;
                                    TemplateDetector.canvas_environment = Canvas_Position_Environment;

                                    Console.WriteLine("segmentation start");
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
                                        this.gestureDetectorList[i].TrackingId = trackingId;
                                        this.gestureDetectorList[i].IsPaused = trackingId == 0;
                                    }
                                    

                                    if (bodies[i].IsTracked)
                                    {
                                        // Update tracking status
                                        persons[i].IsTracked = true;

                                        persons[i].ID = bodies[i].TrackingId;

                                        persons[i].Color = Plot.BodyColors[i];

                                        // Update position
                                        CameraSpacePoint headPositionCamera = bodies[i].Joints[JointType.Head].Position; // Meter
                                        CameraSpacePoint headPositionGournd = Transformation.RotateBackFromTilt(TiltAngle, true, headPositionCamera);
                                        Transformation.ConvertGroundSpaceToPlane(headPositionGournd, persons[i]);

                                        // Update body orientation
                                        CameraSpacePoint leftShoulderPositionGround = Transformation.RotateBackFromTilt(TiltAngle, true, bodies[i].Joints[JointType.ShoulderLeft].Position);
                                        CameraSpacePoint rightShoulderPositionGround = Transformation.RotateBackFromTilt(TiltAngle, true, bodies[i].Joints[JointType.ShoulderRight].Position);
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

                                if (isRecordingOn)
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
            Plot.DrawSystemStatus(Canvas_Position_Foreground, isRecordingOn);
        }

        private void StopRecording_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Console.WriteLine("timer: stop recording");

            if (isRFIDAvailable)
            {
                ObjectDetector.IsRFIDOpen = false;
                System.Threading.Thread.Sleep(1000);

                Console.WriteLine(rfid.Reader.IsConnected);

                if (rfidThread != null)
                {
                    rfidThread.Abort();
                    Console.WriteLine("stoped in KINECT");
                    rfidThread = null;
                }
            }

            if (rfid != null) rfid = null;

            isRecordingOn = false;
            isStopingRecording = false;
        }

        private void DetermineSystemStatus()
        {
            // Condition for starting activity recoginition and record
            if (Transformation.GetNumberOfPeople(persons) >= 1)
            {
                if (!isRecordingOn)
                {
                    ActivityRecognition.Record.StartTime = DateTime.Now.ToString("M-d-yyyy_HH-mm-ss");

                    if (rfid == null) rfid = new ObjectDetector(ListBox_Object, ListView_Object);

                    if (isRFIDAvailable)
                    {
                        ObjectDetector.IsRFIDOpen = true;

                        if (rfidThread == null)
                        {
                            rfidThread = new Thread(new ThreadStart(rfid.Run));
                            rfidThread.Start();
                        }
                    }

                    isRecordingOn = true;
                }
            }

            if (Transformation.GetNumberOfPeople(persons) < 1)
            {
                if (isRecordingOn)
                {

                    if (!isStopingRecording)
                    {
                        isStopingRecording = true;

                        stopRecording = new System.Timers.Timer();
                        stopRecording.AutoReset = false;
                        stopRecording.Elapsed += StopRecording_Elapsed;
                        stopRecording.Interval = STOP_RECORDING_INTERVAL;
                        stopRecording.Enabled = true;
                    }
                }
            }
            else
            {
                if (isStopingRecording)
                {
                    isStopingRecording = false;
                    stopRecording.Enabled = false;
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
            if (rfid != null)
            {
                if (rfid.Reader != null)
                {
                    if (rfid.Reader.IsConnected) rfid.Stop();
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
            foreach (KeyValuePair<Object.Objects, Object> pair in ListBox_Object.SelectedItems) objects.AddLast(pair.Key);
            foreach (Requirement req in ListBox_Requirement.SelectedItems) requirements.AddLast(req);
            activities.AddLast(new Activity(area, orientations, objects, requirements, name, minPeopleCount));
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
        }

        private void Button_ClearSettings(object sender, RoutedEventArgs e)
        {
            activities.Clear();
        }

        private void Button_ShowHeight(object sender, RoutedEventArgs e)
        {
            Popup_Area.IsOpen = true;
        }

    }
}
