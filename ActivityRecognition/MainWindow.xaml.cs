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
        private static readonly double KINECT_CONNECTION_CHECK_INTERVAL = 1000 * 2; // Millisecond

        /// <summary>
        /// Latency for stop recording
        /// Designed for a leaved person coming back in a short period
        /// </summary>
        System.Timers.Timer recordStop;
        private static readonly double RECORD_STOP_INTERVAL = 1000 * 5; // Millisecond

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

        /// <summary>
        /// Status for starting to find templates
        /// </summary>
        private bool isFindingTemplate;

        /// <summary>
        /// Status for segmentation in depth frame by height segment
        /// </summary>
        public static bool isHeightSegmented;

        /// <summary>
        /// The height segmented depth frame pixels for display
        /// </summary>
        public static ushort[] segmentedDepthFramePixels;

        /// <summary>
        /// The 3D points in camera coordinates system mapped from depth view 
        /// </summary>
        public static CameraSpacePoint[] cameraSpacePoints;

        /// <summary>
        /// Binding source property for top view display
        /// Intensity represents for height
        /// </summary>
        public ImageSource TopViewInHeight { get { return topViewInHeight; } }

        /// <summary>
        /// Private top view image
        /// </summary>
        private DrawingImage topViewInHeight;

        /// <summary>
        /// Drawing group for top view
        /// </summary>
        private DrawingGroup drawingGroup_topView;

        /// <summary>
        /// Timer for reset template position found
        /// </summary>
        private System.Timers.Timer templateSearch;
        private static readonly double TEMPLATE_SEARCH_INTERVAL = 1000 * 10; // Millisecond

        /// <summary>
        /// Entry
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            kinectSensor = KinectSensor.GetDefault();

            bodies = new Body[kinectSensor.BodyFrameSource.BodyCount];
            persons = new Person[kinectSensor.BodyFrameSource.BodyCount];
            activities = new LinkedList<Activity>();
            drawingGroup = new DrawingGroup();
            depthSource = new DrawingImage(drawingGroup);
            drawingGroup_topView = new DrawingGroup();
            topViewInHeight = new DrawingImage(drawingGroup_topView);
            DataContext = this;

            depthFramePixels = new ushort[kinectSensor.DepthFrameSource.FrameDescription.Width *
                                         kinectSensor.DepthFrameSource.FrameDescription.Height];

            segmentedDepthFramePixels = new ushort[kinectSensor.DepthFrameSource.FrameDescription.Width *
                                         kinectSensor.DepthFrameSource.FrameDescription.Height];

            Settings.Load(activities);
            Requirement.LoadRequirements(ListBox_Requirement);
            Plot.InitBackgroundCanvas(Canvas_Position_Background);
            TemplateDetector.loadTemplate(ListBox_Area);

            // Select source type needed, e.g., depth, color, body
            multiSourceFrameReader = kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Body | FrameSourceTypes.Depth);
            multiSourceFrameReader.MultiSourceFrameArrived += MultiSourceFrameReader_MultiSourceFrameArrived;
            kinectSensor.IsAvailableChanged += KinectSensor_IsAvailableChanged;
            kinectSensor.Open();

            kinectConnectionCheck = new System.Timers.Timer();
            kinectConnectionCheck.AutoReset = false;
            kinectConnectionCheck.Elapsed += kinectConnectionCheck_Elapsed;
            kinectConnectionCheck.Interval = KINECT_CONNECTION_CHECK_INTERVAL;
            kinectConnectionCheck.Enabled = true;

            templateSearch = new System.Timers.Timer();
            templateSearch.AutoReset = true;
            templateSearch.Elapsed += TemplateSearch_Elaped;     
            templateSearch.Interval = TEMPLATE_SEARCH_INTERVAL;
            templateSearch.Enabled = true;

            applicationRestart = new System.Timers.Timer();

            gestureDetectorList = new List<PostureDetector>();
            postures = new LinkedList<Posture>();
            
            for (int i = 0; i < kinectSensor.BodyFrameSource.BodyCount; ++i)
            {
                PostureDetector detector = new PostureDetector(kinectSensor, i, Canvas_Position_Foreground);
                gestureDetectorList.Add(detector);

                // Init postures from trained PostureDetector.GESTURE_DB once
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

        /// <summary>
        /// Restart application
        /// Timer callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ApplicationRestart_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            System.Diagnostics.Process.Start(Application.ResourceAssembly.Location);
            isDownApplication = true;
        }

        /// <summary>
        /// Search templates
        /// Timer callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TemplateSearch_Elaped(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!isFindingTemplate && !TemplateDetector.isProcessing)
            {
                isFindingTemplate = true;
            }
        }

        /// <summary>
        /// Check Kinect connection when starting the application
        /// Timer callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void kinectConnectionCheck_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!kinectSensor.IsAvailable)
            {
                ErrorHandler.ProcessDisconnectError();
            }

            ((System.Timers.Timer)sender).Close();
        }
        
        /// <summary>
        /// Handle the processing when Kinect connection status is changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KinectSensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            if (!isKinectConnected && e.IsAvailable)
            {
                isKinectConnected = true;
                ErrorHandler.ProcessConnectNotification();
            }
            else if (isKinectConnected && !e.IsAvailable)
            {
                isKinectConnected = false;

                // Bug: conflict with restarting application
                //ErrorHandler.ProcessDisconnectError();
            }
        }


        /// <summary>
        /// Handle the processing when Kinect frame arrived
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
                                // Refresh the foreground of 2D top view for positioning
                                Plot.RefreshForegroundCanvas(Canvas_Position_Foreground, activities);

                                // Find templates
                                if (isFindingTemplate)
                                {
                                    ushort[] depthFrameData = new ushort[depthFrame.FrameDescription.Height * depthFrame.FrameDescription.Width];
                                    depthFrame.CopyFrameDataToArray(depthFrameData);

                                    cameraSpacePoints = new CameraSpacePoint[depthFrame.FrameDescription.Height * depthFrame.FrameDescription.Width];
                                    kinectSensor.CoordinateMapper.MapDepthFrameToCameraSpace(depthFrameData, cameraSpacePoints);

                                    TemplateDetector.heightLow = -2.4f;
                                    TemplateDetector.heightHigh = -1.9f;
                                    TemplateDetector.pointDiameter = 4;
                                    TemplateDetector.canvas_width = Canvas_Position_Background.Width;
                                    TemplateDetector.canvas_height = Canvas_Position_Background.Height;
                                    TemplateDetector.canvas_environment = Canvas_Position_Environment;

                                    BackgroundWorker worker = new BackgroundWorker();
                                    worker.WorkerReportsProgress = true;
                                    worker.DoWork += TemplateDetector.DoInBackgrond;
                                    worker.ProgressChanged += TemplateDetector.OnProgress;
                                    worker.RunWorkerCompleted += TemplateDetector.OnPostExecute;
                                    worker.RunWorkerAsync();

                                    isFindingTemplate = false;
                                }                            

                                // Display depth frame
                                // Uncomment to enable the display for height segmentation result
                                //if (!isHeightSegmented)
                                if (true)
                                {
                                    drawingContext.DrawImage(Transformation.ToBitmap(depthFrame, depthFramePixels, true),
                                                        new Rect(0.0, 0.0, kinectSensor.DepthFrameSource.FrameDescription.Width, kinectSensor.DepthFrameSource.FrameDescription.Height));
                                }
                                else
                                {
                                    drawingContext.DrawImage(Transformation.ToBitmap(depthFrame, segmentedDepthFramePixels, false),
                                                        new Rect(0.0, 0.0, kinectSensor.DepthFrameSource.FrameDescription.Width, kinectSensor.DepthFrameSource.FrameDescription.Height));
                                }

                                // Display top view in height
                                if (TemplateDetector.isDrawDone)
                                {
                                    using (DrawingContext drawingContext_heightview = drawingGroup_topView.Open())
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
                               
                                // Load raw body joints info from Kinect
                                bodyFrame.GetAndRefreshBodyData(bodies);                                                   

                                // Update personal infomation from raw joints
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

                                        // Assign color to person in the top view for positioning
                                        persons[i].Color = Plot.BodyColors[i];

                                        // Get person's 3D postion in camera's coordinate system
                                        CameraSpacePoint headPositionCamera = bodies[i].Joints[JointType.Head].Position; // Meter

                                        // Convert to 3D position in horizontal coordinate system
                                        CameraSpacePoint headPositionGournd = Transformation.RotateBackFromTilt(TILT_ANGLE, true, headPositionCamera);

                                        // Convert to 2D top view position on canvas
                                        Transformation.ConvertGroundSpaceToPlane(headPositionGournd, persons[i]);

                                        // Determine body orientation using shoulder joints
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

                                // Recognize and record activities when recording requirements are satisfied
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

        /// <summary>
        /// Record each person's activitie in timeline
        /// Record each person's position in timeline
        /// </summary>
        public void Record()
        {
            ActivityRecognition.Record.RecordActivity(activities);
            ActivityRecognition.Record.RecordPosition(persons);
        }

        /// <summary>
        /// Draw recording system status
        /// </summary>
        private void DrawSystemStatus()
        {
            Plot.DrawSystemStatus(Canvas_Position_Foreground, isRecording);
        }

        /// <summary>
        /// Start to stop recording after a latency
        /// Callback for timer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RecordStop_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            applicationRestart.Enabled = false;

            if (isRFIDRequired)
            {
                ObjectDetector.IsRFIDOpen = false;
                System.Threading.Thread.Sleep(1000);

                if (rfidThread != null)
                {
                    rfidThread.Abort();
                    rfidThread = null;
                }
            }

            if (objectDetector != null) objectDetector = null;

            isRecording = false;
            isStoppingRecord = false;
        }

        /// <summary>
        /// Determine recording system status with requirements
        /// </summary>
        private void DetermineSystemStatus()
        {
            // Take number of people in the view for the requirements
            // Start recording
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

            // Stop recording
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

        /// <summary>
        /// Decide each person's activity
        /// Decide each activity's status - acted or not
        /// </summary>
        private void CheckActivity()
        {
            Activity.DecideActivityForPeople(activities, persons, Canvas_Position_Foreground);
            Activity.DecideStatusOfActivity(activities, persons, Canvas_Position_Foreground);
        }

        /// <summary>
        /// Draw person on top view for positioning
        /// </summary>
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

        /// <summary>
        /// Draw text of each person's activity on top view for positioning
        /// </summary>
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

        /// <summary>
        /// Label each person's head and some joints in depth frame view
        /// </summary>
        /// <param name="drawingContext"></param>
        private void DrawPeopleOnDepth(DrawingContext drawingContext)
        {
            for (int i = 0; i < persons.Length; i++) 
            {
                if (persons[i].IsTracked)
                {
                    LinkedList<CameraSpacePoint> jointPoints = new LinkedList<CameraSpacePoint>();

                    // Select the joints needed to display
                    jointPoints.AddLast(bodies[i].Joints[JointType.Head].Position);
                    jointPoints.AddLast(bodies[i].Joints[JointType.ShoulderLeft].Position);
                    jointPoints.AddLast(bodies[i].Joints[JointType.ShoulderRight].Position);

                    Plot.DrawJointsOnDepth(kinectSensor.CoordinateMapper, jointPoints, drawingContext, kinectSensor);
                    Plot.DrawTrackedHead(kinectSensor.CoordinateMapper, persons[i].Color, bodies[i].Joints[JointType.Neck].Position, bodies[i].Joints[JointType.Head].Position, drawingContext, kinectSensor);
                }
            }
        }

        /// <summary>
        /// Dispose unreleased references when closing the window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Kinect
            if (multiSourceFrameReader != null) multiSourceFrameReader.Dispose();
            if (kinectSensor != null) kinectSensor.Close();

            // Object detector - RFID
            if (objectDetector != null)
            {
                if (objectDetector.Reader != null)
                {
                    if (objectDetector.Reader.IsConnected) objectDetector.Stop();
                }
                if (rfidThread != null) rfidThread.Abort();
            }

            // Posture detector
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

        /// <summary>
        /// Get the start mouse position for area when defining activity
        /// Mouse event callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            isMouseDown = true;
            mouseDownPosition = e.GetPosition(Canvas_Position_Foreground);
            Canvas.SetLeft(Rectangle_SelectArea, mouseDownPosition.X);
            Canvas.SetTop(Rectangle_SelectArea, mouseDownPosition.Y);
            Rectangle_SelectArea.Width = 0;
            Rectangle_SelectArea.Height = 0;
        }

        /// <summary>
        /// Change the rectangle - selected area when mose is moving
        /// Mouse event callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

        /// <summary>
        /// Get the end mouse position for area when defining activity
        /// Mouse event callback
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            isMouseDown = false;
            mouseUpPosition = e.GetPosition(Canvas_Position_Foreground);
        }

        /// <summary>
        /// Add a defined activity
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_AddActivity(object sender, RoutedEventArgs e)
        {
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

        /// <summary>
        /// Pop up the window to add activity
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_PopupActivity(object sender, RoutedEventArgs e)
        {
            Popup_AddActivity.IsOpen = true;
        }

        /// <summary>
        /// Pop up the window to save or clear defined activities
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_PopupSetting(object sender, RoutedEventArgs e)
        {
            Popup_Settings.IsOpen = true;
        }

        /// <summary>
        /// Save defined activities
        /// Load automatically when next start
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_SaveSettings(object sender, RoutedEventArgs e)
        {
            Settings.Save(activities);

            Popup_Settings.IsOpen = false;
        }

        /// <summary>
        /// Clear defined activities
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_ClearSettings(object sender, RoutedEventArgs e)
        {
            activities.Clear();

            Popup_Settings.IsOpen = false;
        }

        /// <summary>
        /// Popup the view to display the top view in height
        /// For template detection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_ShowHeight(object sender, RoutedEventArgs e)
        {
            Popup_Area.IsOpen = true;
        }

    }
}
