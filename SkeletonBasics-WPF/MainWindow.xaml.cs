//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.SkeletonBasics
{
    using System;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using Microsoft.Kinect;
    using Kinova.DLL.Data.Jaco.Control;
    using Kinova.API.Jaco;
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Width of output drawing
        /// </summary>
        private const float RenderWidth = 640.0f;
        //SerialPort _serialPort; 
        /// <summary>
        /// Height of our output drawing
        /// </summary>
        private const float RenderHeight = 480.0f;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of body center ellipse
        /// </summary>
        private const double BodyCenterThickness = 10;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Brush used to draw skeleton center point
        /// </summary>
        private readonly Brush centerPointBrush = Brushes.Blue;

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently tracked
        /// </summary>
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor sensor;

        /// <summary>
        /// Drawing group for skeleton rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        /// <summary>
        /// Drawing image that we will display
        /// </summary>
        private DrawingImage imageSource;


        /// <summary>
        /// Custom Variables
        /// </summary>
        /// 
        public bool destroywindow { get; set; }
        MODE state;

        CPointsTrajectory[] trajectory = new CPointsTrajectory[3];
        
        CJacoArm Jaco;
        class jacoTrainAngles 
        {
            public double shoulderAngle;
            public double elbowAngle;
            public double handRotationAngle;
            public jacoTrainAngles()
            {
                shoulderAngle = elbowAngle = handRotationAngle = 0;
            }
        };

        int count = 0;
        bool buttonOn = false;
        jaco_connect mainJaco;
        Log Logger;
        double[,] buffer = new double[200, 2];
        //buffer: column 0:ShoulderAngle column 1:ElbowAngle
        //buffer: row no. of frames
        int buffercount = 0;

        double[,] frame1Mem = new double[361, 361];
        double[,] frame2Mem = new double[361, 361];
        double[,] frame3Mem = new double[361, 361];
        char[] fbuffer = new char[10];
        public int noOfActions = 3;
        int detectedFrame = -1;
        int currentAction = -1;

        jacoTrainAngles anglesForJacoTraining = new jacoTrainAngles();

        double[,] weight = new double[3, 3] { { 10, 10, 10 }, { 10, 10, 10 }, { 10, 10, 10 } };//[frameNo actionNo];

        System.Windows.Threading.DispatcherTimer tmrKinect;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow(Log log)
        {
            InitializeComponent();
            Logger = log;
            tmrKinect = new System.Windows.Threading.DispatcherTimer();
            tmrKinect.Tick += kinectStatus;
            tmrKinect.Interval = new System.TimeSpan(0, 0, 10);
        }

        bool firstKinectError = true;

        void kinectStatus(object sender, System.EventArgs e)
        {
            if (firstKinectError & sensor.Status != KinectStatus.Connected)
            {
                firstKinectError = false;
                Logger.addLog("--------------------------------", true);
                Logger.addLog("ERROR in Kinect Input", true);
                Logger.addLog("--------------------------------", true);
            }
        }

        /// <summary>
        /// Draws indicators to show which edges are clipping skeleton data
        /// </summary>
        /// <param name="skeleton">skeleton to draw clipping information for</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private static void RenderClippedEdges(Skeleton skeleton, DrawingContext drawingContext)
        {
            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, RenderHeight - ClipBoundsThickness, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, RenderWidth, ClipBoundsThickness));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(0, 0, ClipBoundsThickness, RenderHeight));
            }

            if (skeleton.ClippedEdges.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(
                    Brushes.Red,
                    null,
                    new Rect(RenderWidth - ClipBoundsThickness, 0, ClipBoundsThickness, RenderHeight));
            }
        }

        public void updateMODE(MODE mode)
        {
            state = mode;
            changeMODE();
        }

        private void changeMODE()
        {
            //defaults for every mode
            if (state == MODE.DATASET_TRAIN)
            {
                btnSave.Visibility = System.Windows.Visibility.Visible;
                btnContinue.Visibility = System.Windows.Visibility.Visible;
                lblAction.Visibility = System.Windows.Visibility.Hidden;
                lblFrame.Visibility = System.Windows.Visibility.Hidden;
                btnYes.Visibility = System.Windows.Visibility.Hidden;
                btnNo.Visibility = System.Windows.Visibility.Hidden;
                btnTrajAdd.Visibility = System.Windows.Visibility.Hidden;
                cmbTrajectory.Visibility = System.Windows.Visibility.Hidden;
                btnTrajClear.Visibility = System.Windows.Visibility.Hidden;
                buffercount = 0;
            }
            else if (state == MODE.JACO_TASK_PLANNING)
            {
                btnSave.Visibility = System.Windows.Visibility.Hidden;
                btnContinue.Visibility = System.Windows.Visibility.Visible;
                lblAction.Visibility = System.Windows.Visibility.Hidden;
                lblFrame.Visibility = System.Windows.Visibility.Hidden;
                btnYes.Visibility = System.Windows.Visibility.Hidden;
                btnNo.Visibility = System.Windows.Visibility.Hidden;
                btnTrajAdd.Visibility = System.Windows.Visibility.Visible;
                cmbTrajectory.Visibility = System.Windows.Visibility.Visible;
                btnTrajClear.Visibility = System.Windows.Visibility.Visible;
            }
            else if (state == MODE.RUN_SYSTEM)
            {
                btnSave.Visibility = System.Windows.Visibility.Hidden;
                btnContinue.Visibility = System.Windows.Visibility.Visible;
                lblAction.Visibility = System.Windows.Visibility.Visible;
                lblFrame.Visibility = System.Windows.Visibility.Visible;
                btnYes.Visibility = System.Windows.Visibility.Visible;
                btnNo.Visibility = System.Windows.Visibility.Visible;
                btnTrajAdd.Visibility = System.Windows.Visibility.Hidden;
                cmbTrajectory.Visibility = System.Windows.Visibility.Hidden;
                btnTrajClear.Visibility = System.Windows.Visibility.Hidden;
                readTrainingData();
                Logger.addLog("Training Data Read");
            }
            else if (state == MODE.PAUSE)
            {
                btnSave.Visibility = System.Windows.Visibility.Hidden;
                btnContinue.Visibility = System.Windows.Visibility.Hidden;
                lblAction.Visibility = System.Windows.Visibility.Visible;
                lblFrame.Visibility = System.Windows.Visibility.Visible;
                btnYes.Visibility = System.Windows.Visibility.Hidden;
                btnNo.Visibility = System.Windows.Visibility.Hidden;
                btnTrajAdd.Visibility = System.Windows.Visibility.Hidden;
                cmbTrajectory.Visibility = System.Windows.Visibility.Hidden;
                btnTrajClear.Visibility = System.Windows.Visibility.Hidden;
            }

        }

        /// <summary>
        /// Execute startup tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            Logger.addLog("Main Window Loaded");
            mainJaco = new jaco_connect(Logger);
            Logger.addLog("Initializing Jaco Connection");
            Jaco = mainJaco.init();
            count = 0;
            //set initial position of jaco
            Logger.addLog("Setting Jaco Initial Position");
            mainJaco.resetPoint();
            mainJaco.UpdatePoint(7, 0);
            mainJaco.sendTraj();

            for (int i = 0; i < 3; i++)
                trajectory[i] = new CPointsTrajectory();

            // Create the drawing group we'll use for drawing
            this.drawingGroup = new DrawingGroup();

            // Create an image source that we can use in our image control
            this.imageSource = new DrawingImage(this.drawingGroup);

            // Display the drawing using our image control
            Image.Source = this.imageSource;

            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (null != this.sensor)
            {
                // Turn on the skeleton stream to receive skeleton frames
                this.sensor.SkeletonStream.Enable();

                // Add an event handler to be called whenever there is new color frame data
                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;

                // Start the sensor!
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }

            if (null == this.sensor)
            {
                this.statusBarText.Text = Properties.Resources.NoKinectReady;
                Logger.addLog("Initializing Kinect FAILED", true);
            }
            else
            {
                tmrKinect.Start();
                Logger.addLog("Kinect Initialized");
            }

            Logger.addLog("INITIALIZATION COMPLETE");
            Logger.addLog("-------------------------");

        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (destroywindow)
            {
                if (null != this.sensor)
                {
                    this.sensor.Stop();
                }
            }
            else
                e.Cancel = true;
        }

        private void run_system(object sender, SkeletonFrameReadyEventArgs e)
        {

            //buttonOn = false; // hard code to remove continue

            if (!buttonOn) count++;
            this.label2.Content = count.ToString();
            if (count < 5) return;
            if (!buttonOn)
            {
                btnContinue.IsEnabled = true; //uncomment to enable button
                buttonOn = true;
            }

            Logger.addLog("--------------------------------------------");
            Logger.addLog("New Frame obtained after skipping " + count.ToString() + " frames");
            firstKinectError = true;
            count = 0;
            Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }


            using (DrawingContext dc = this.drawingGroup.Open())
            {


                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                if (skeletons.Length != 0)
                {
                    int skelCount = 0;
                    foreach (Skeleton skel in skeletons)
                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                            skelCount++;
                    Logger.addLog(skelCount.ToString() + " Skeleton(s) detected");

                    foreach (Skeleton skel in skeletons)
                    {
                        RenderClippedEdges(skel, dc);

                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {

                            this.DrawBonesAndJoints(skel, dc);

                            //BEGIN OF CUSTOM CODE

                            currentAction = 0;

                            lblAction.Content = "ACTION " + (currentAction + 1).ToString();


                            //TODO change all cartesian to angular
                            //copy caretesian co-ordinates from curr to last (implement memory)
                            double handRotationAngle;

                            double shoulderAngle, elbowAngle;

                            //elbow joint angle
                            elbowAngle = getJointAngle(JointType.ShoulderRight, JointType.ElbowRight, JointType.WristRight, skel);
                            Logger.addLog("Elbow angle: " + elbowAngle.ToString() + " deg");

                            //shoulder joint angle(x-y direction)
                            shoulderAngle = getJointAngle(JointType.Spine, JointType.ShoulderRight, JointType.ElbowRight, skel);
                            Logger.addLog("Shoulder angle: " + shoulderAngle.ToString() + " deg");


                            //show modified point(obtained from shoulder and spine points) to display screen
                            SkeletonPoint xp = new SkeletonPoint();
                            xp.X = skel.Joints[JointType.ShoulderRight].Position.X;
                            xp.Y = skel.Joints[JointType.Spine].Position.Y;
                            xp.Z = skel.Joints[JointType.ShoulderRight].Position.Z;
                            dc.DrawEllipse(Brushes.Blue, null, this.SkeletonPointToScreen(xp), JointThickness, JointThickness);

                            //get hand rotation about Y-axis
                            handRotationAngle = getHandRotationAngle(skel);
                            Logger.addLog("Hand rotation about Y axis: " + handRotationAngle.ToString() + " deg");


                            Logger.addLog("Frame1 Membership : " + frame1Mem[(int)(shoulderAngle * 2), (int)(elbowAngle * 2)]);
                            Logger.addLog("Frame2 Membership : " + frame2Mem[(int)(shoulderAngle * 2), (int)(elbowAngle * 2)]);
                            Logger.addLog("Frame3 Membership : " + frame3Mem[(int)(shoulderAngle * 2), (int)(elbowAngle * 2)]);

                            double f1mem, f2mem, f3mem;
                            f1mem = frame1Mem[(int)(shoulderAngle * 2), (int)(elbowAngle * 2)];
                            f2mem = frame2Mem[(int)(shoulderAngle * 2), (int)(elbowAngle * 2)];
                            f3mem = frame3Mem[(int)(shoulderAngle * 2), (int)(elbowAngle * 2)];

                            if (f1mem >= f2mem && f1mem >= f3mem)
                            {
                                lblFrame.Content = "frame1";
                                detectedFrame = 1 - 1;
                            }
                            else if (f2mem >= f1mem && f2mem >= f3mem)
                            {
                                lblFrame.Content = "frame2";
                                detectedFrame = 2 - 1;
                            }
                            else
                            {
                                lblFrame.Content = "frame3";
                                detectedFrame = 3 - 1;
                            }


                            double w1 = weight[detectedFrame, 0];
                            double w2 = weight[detectedFrame, 1];
                            double w3 = weight[detectedFrame, 2];

                            if (w1 >= w2 && w1 >= w3)
                            {
                                lblAction.Content = "ACTION 1"; 
                                currentAction = 1 -1; 
                            }
                            else if (w2 >= w1 && w2 >= w3)
                            {
                                lblAction.Content = "ACTION 2"; 
                                currentAction = 2 -1;
                            }
                            else
                            {
                                lblAction.Content = "ACTION 3"; 
                                currentAction = 3 -1; 
                            }


                            Logger.addLog("Sending trajectory " + (currentAction + 1).ToString() + " to Jaco");
                            try
                            {
                                Jaco.ControlManager.EraseTrajectories();
                            }
                            catch (Exception)
                            {
                                Logger.addLog("Error in erasing Jaco trajectories!");
                            }
                            mainJaco.sendMultiTraj(trajectory[currentAction]);
                            //END OF CUSTOM CODE

                        }
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawEllipse(
                            this.centerPointBrush,
                            null,
                            this.SkeletonPointToScreen(skel.Position),
                            BodyCenterThickness,
                            BodyCenterThickness);
                        }
                    }
                }

                // prevent drawing outside of our render area
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
        }

        private void dataset_train(object sender, SkeletonFrameReadyEventArgs e)
        {
            buttonOn = false; // hard code to remove continue

            if (!buttonOn) count++;
            this.label2.Content = count.ToString();
            if (count < 5) return;
            if (!buttonOn)
            {
                btnContinue.IsEnabled = true; //uncomment to enable button
                buttonOn = true;
            }

            Logger.addLog("--------------------------------------------");
            Logger.addLog("New Frame obtained after skipping " + count.ToString() + " frames");
            firstKinectError = true;
            count = 0;
            Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }


            using (DrawingContext dc = this.drawingGroup.Open())
            {


                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                if (skeletons.Length != 0)
                {
                    int skelCount = 0;
                    foreach (Skeleton skel in skeletons)
                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                            skelCount++;
                    Logger.addLog(skelCount.ToString() + " Skeleton(s) detected");

                    foreach (Skeleton skel in skeletons)
                    {
                        RenderClippedEdges(skel, dc);

                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {

                            this.DrawBonesAndJoints(skel, dc);

                            //BEGIN OF CUSTOM CODE

                            //TODO change all cartesian to angular
                            //copy caretesian co-ordinates from curr to last (implement memory)
                            double angle;

                            double shoulderAngle, elbowAngle;

                            if (buffercount >= 200)
                            {
                                //buttonOn = true;
                                return;
                            }

                            //elbow joint angle
                            elbowAngle = angle = getJointAngle(JointType.ShoulderRight, JointType.ElbowRight, JointType.WristRight, skel);
                            buffer[buffercount, 1] = angle;
                            Logger.addLog("Elbow angle: " + angle.ToString() + " deg");

                            //shoulder joint angle(x-y direction)
                            shoulderAngle = angle = getJointAngle(JointType.Spine, JointType.ShoulderRight, JointType.ElbowRight, skel);
                            buffer[buffercount, 0] = angle;
                            Logger.addLog("Shoulder angle: " + angle.ToString() + " deg");


                            //show modified point(obtained from shoulder and spine points) to display screen
                            SkeletonPoint xp = new SkeletonPoint();
                            xp.X = skel.Joints[JointType.ShoulderRight].Position.X;
                            xp.Y = skel.Joints[JointType.Spine].Position.Y;
                            xp.Z = skel.Joints[JointType.ShoulderRight].Position.Z;
                            dc.DrawEllipse(Brushes.Blue, null, this.SkeletonPointToScreen(xp), JointThickness, JointThickness);

                            Logger.addLog("BufferCount: " + buffercount.ToString());

                            buffercount++;
                            //END OF CUSTOM CODE

                        }
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawEllipse(
                            this.centerPointBrush,
                            null,
                            this.SkeletonPointToScreen(skel.Position),
                            BodyCenterThickness,
                            BodyCenterThickness);
                        }
                    }
                }

                // prevent drawing outside of our render area
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
        }

        private void jaco_task_planning(object sender, SkeletonFrameReadyEventArgs e)
        {
            //buttonOn = false; // hard code to remove continue

            if (!buttonOn) count++;
            this.label2.Content = count.ToString();
            if (count < 5) return;
            if (!buttonOn)
            {
                btnContinue.IsEnabled = true; //uncomment to enable button
                buttonOn = true;
            }

            Logger.addLog("--------------------------------------------");
            Logger.addLog("New Frame obtained after skipping " + count.ToString() + " frames");
            firstKinectError = true;
            count = 0;
            Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }


            using (DrawingContext dc = this.drawingGroup.Open())
            {
                // Draw a transparent background to set the render size
                dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

                if (skeletons.Length != 0)
                {
                    int skelCount = 0;
                    foreach (Skeleton skel in skeletons)
                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                            skelCount++;
                    Logger.addLog(skelCount.ToString() + " Skeleton(s) detected");

                    foreach (Skeleton skel in skeletons)
                    {
                        RenderClippedEdges(skel, dc);

                        if (skel.TrackingState == SkeletonTrackingState.Tracked)
                        {

                            this.DrawBonesAndJoints(skel, dc);

                            //BEGIN OF CUSTOM CODE

                            //SEQUENCE: 1. resetPoint 2. get a single joint angle from kinect 3. update joint angle to jaco
                            mainJaco.resetPoint();

                            //elbow joint angle
                            anglesForJacoTraining.elbowAngle = getJointAngle(JointType.ShoulderRight, JointType.ElbowRight, JointType.WristRight, skel);
                            Logger.addLog("Elbow angle: " + anglesForJacoTraining.elbowAngle.ToString() + " deg");
                            mainJaco.UpdatePoint((int)JointType.ElbowRight, anglesForJacoTraining.elbowAngle);

                            //shoulder joint angle(x-y direction)
                            anglesForJacoTraining.shoulderAngle = getJointAngle(JointType.Spine, JointType.ShoulderRight, JointType.ElbowRight, skel);
                            Logger.addLog("Shoulder angle: " + anglesForJacoTraining.shoulderAngle.ToString() + " deg");
                            mainJaco.UpdatePoint((int)JointType.ShoulderRight, anglesForJacoTraining.shoulderAngle);


                            //show modified point(obtained from shoulder and spine points) to display screen
                            SkeletonPoint xp = new SkeletonPoint();
                            xp.X = skel.Joints[JointType.ShoulderRight].Position.X;
                            xp.Y = skel.Joints[JointType.Spine].Position.Y;
                            xp.Z = skel.Joints[JointType.ShoulderRight].Position.Z;
                            dc.DrawEllipse(Brushes.Blue, null, this.SkeletonPointToScreen(xp), JointThickness, JointThickness);

                            //get hand rotation about Y-axis
                            anglesForJacoTraining.handRotationAngle = getHandRotationAngle(skel);
                            Logger.addLog("Hand rotation about Y axis: " + anglesForJacoTraining.handRotationAngle.ToString() + " deg");
                            //7 -> 7-7=0, i.e. jaco joint 0
                            mainJaco.UpdatePoint(7, anglesForJacoTraining.handRotationAngle);

                            Logger.addLog("Sending angles to Jaco...");
                            mainJaco.sendTraj();

                            //END OF CUSTOM CODE

                        }
                        else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
                        {
                            dc.DrawEllipse(
                            this.centerPointBrush,
                            null,
                            this.SkeletonPointToScreen(skel.Position),
                            BodyCenterThickness,
                            BodyCenterThickness);
                        }
                    }
                }

                // prevent drawing outside of our render area
                this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            }
        }

        /// <summary>
        /// Event handler for Kinect sensor's SkeletonFrameReady event
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {

            switch (state)
            {
                case MODE.PAUSE: return;
                case MODE.DATASET_TRAIN: dataset_train(sender,e); break;
                case MODE.JACO_TASK_PLANNING: jaco_task_planning(sender, e); break;
                case MODE.RUN_SYSTEM: run_system(sender,e); break;
            }

 
            ////buttonOn = false; // hard code to remove continue

            //if(!buttonOn) count++;
            //this.label2.Content = count.ToString();
            //if (count < 5) return;
            ////readytoexecute = false;
            //this.btnContinue.IsEnabled = true; //uncomment to enable button
            //this.buttonOn = true;

            //Logger.addLog("--------------------------------------------");
            //Logger.addLog("New Frame obtained after skipping " + count.ToString() + " frames");
            //firstKinectError = true;
            //count = 0;
            //Skeleton[] skeletons = new Skeleton[0];

            //using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            //{
            //    if (skeletonFrame != null)
            //    {
            //        skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
            //        skeletonFrame.CopySkeletonDataTo(skeletons);
            //    }
            //}

            
            //using (DrawingContext dc = this.drawingGroup.Open())
            //{
                
                
            //    // Draw a transparent background to set the render size
            //    dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, RenderWidth, RenderHeight));

            //    if (skeletons.Length != 0)
            //    {
            //        int skelCount = 0;
            //        foreach (Skeleton skel in skeletons)
            //            if (skel.TrackingState == SkeletonTrackingState.Tracked)
            //                skelCount++;
            //        Logger.addLog(skelCount.ToString() + " Skeleton(s) detected");

            //        foreach (Skeleton skel in skeletons)
            //        {
            //            RenderClippedEdges(skel, dc);

            //            if (skel.TrackingState == SkeletonTrackingState.Tracked)
            //            {
                            
            //                this.DrawBonesAndJoints(skel, dc);

            //                //BEGIN OF CUSTOM CODE

            //                currentAction = 0;

            //                lblAction.Content = "ACTION " + (currentAction + 1).ToString();


            //                //TODO change all cartesian to angular
            //                //copy caretesian co-ordinates from curr to last (implement memory)
            //                double angle;

            //                double shoulderAngle,elbowAngle;

            //                //SEQUENCE: 1. resetPoint 2. get a single joint angle from kinect 3. update joint angle to jaco
            //                mainJaco.resetPoint();

            //                if (buffercount >= 200)
            //                {
            //                    buttonOn = true;
            //                    return;
            //                }

            //                //elbow joint angle
            //                elbowAngle = angle = getJointAngle(JointType.ShoulderRight, JointType.ElbowRight, JointType.WristRight, skel);
            //                buffer[buffercount, 1] = angle;
            //                Logger.addLog("Elbow angle: " + angle.ToString() + " deg");
            //                mainJaco.UpdatePoint((int)JointType.ElbowRight, angle);

            //                //shoulder joint angle(x-y direction)
            //                shoulderAngle =  angle = getJointAngle(JointType.Spine, JointType.ShoulderRight, JointType.ElbowRight, skel);
            //                buffer[buffercount, 0] = angle;
            //                Logger.addLog("Shoulder angle: " + angle.ToString() + " deg");
            //                mainJaco.UpdatePoint((int)JointType.ShoulderRight, angle);


            //                //show modified point(obtained from shoulder and spine points) to display screen
            //                SkeletonPoint xp = new SkeletonPoint();
            //                xp.X = skel.Joints[JointType.ShoulderRight].Position.X;
            //                xp.Y = skel.Joints[JointType.Spine].Position.Y;
            //                xp.Z = skel.Joints[JointType.ShoulderRight].Position.Z;
            //                dc.DrawEllipse(Brushes.Blue, null, this.SkeletonPointToScreen(xp), JointThickness, JointThickness);

            //                //get hand rotation about Y-axis
            //                angle = getHandRotationAngle(skel);
            //                Logger.addLog("Hand rotation about Y axis: " + angle.ToString() + " deg");
            //                //7 -> 7-7=0, i.e. jaco joint 0
            //                mainJaco.UpdatePoint(7,angle);


            //                Logger.addLog("Frame1 Membership : " + frame1Mem[(int)(shoulderAngle * 2), (int)(elbowAngle * 2)]);
            //                Logger.addLog("Frame2 Membership : " + frame2Mem[(int)(shoulderAngle * 2), (int)(elbowAngle * 2)]);
            //                Logger.addLog("Frame3 Membership : " + frame3Mem[(int)(shoulderAngle * 2), (int)(elbowAngle * 2)]);

            //                double f1mem, f2mem, f3mem;
            //                f1mem = frame1Mem[(int)(shoulderAngle * 2), (int)(elbowAngle * 2)];
            //                f2mem = frame2Mem[(int)(shoulderAngle * 2), (int)(elbowAngle * 2)];
            //                f3mem = frame3Mem[(int)(shoulderAngle * 2), (int)(elbowAngle * 2)];

            //                if (f1mem > f2mem && f1mem > f3mem)
            //                {
            //                        lblFrame.Content = "frame1";
            //                        detectedFrame = 1 -1;
            //                }
            //                else if (f2mem > f1mem && f2mem > f3mem)
            //                {
            //                    lblFrame.Content = "frame2";
            //                    detectedFrame = 2 -1;
            //                }
            //                else
            //                {
            //                    lblFrame.Content = "frame3";
            //                    detectedFrame = 3 -1;
            //                }

            //                Random random = new Random();  
            //                double randVal = random.Next(0,1);


            //                double w1 = weight[detectedFrame,0]/(weight[detectedFrame,0]+weight[detectedFrame,1]+weight[detectedFrame,2]);
            //                double w2 = weight[detectedFrame,1]/(weight[detectedFrame,0]+weight[detectedFrame,1]+weight[detectedFrame,2]);
            //                double w3 = weight[detectedFrame,2]/(weight[detectedFrame,0]+weight[detectedFrame,1]+weight[detectedFrame,2]);

            //                if (randVal < w1)
            //                { lblAction.Content = "ACTION 1"; currentAction = 0; }
            //                else if (randVal >= w1 && randVal < w2)
            //                { lblAction.Content = "ACTION 2"; currentAction = 1; }
            //                else
            //                { lblAction.Content = "ACTION 3"; currentAction = 2; }



            //                Logger.addLog("BufferCount: " + buffercount.ToString());

            //                buffercount++;
                            

            //                Logger.addLog("Sending angles to Jaco...");
            //                mainJaco.sendTraj();
            //                //END OF CUSTOM CODE

            //            }
            //            else if (skel.TrackingState == SkeletonTrackingState.PositionOnly)
            //            {
            //                dc.DrawEllipse(
            //                this.centerPointBrush,
            //                null,
            //                this.SkeletonPointToScreen(skel.Position),
            //                BodyCenterThickness,
            //                BodyCenterThickness);
            //            }
            //        }
            //    }

            //    // prevent drawing outside of our render area
            //    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, RenderWidth, RenderHeight));
            //}
        }

        /// <summary>
        /// Draws a skeleton's bones and joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawBonesAndJoints(Skeleton skeleton, DrawingContext drawingContext)
        {
            // Render Torso
            this.DrawBone(skeleton, drawingContext, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone(skeleton, drawingContext, JointType.Spine, JointType.HipCenter);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(skeleton, drawingContext, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(skeleton, drawingContext, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.DrawBone(skeleton, drawingContext, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(skeleton, drawingContext, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(skeleton, drawingContext, JointType.WristRight, JointType.HandRight);

            // Left Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(skeleton, drawingContext, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            this.DrawBone(skeleton, drawingContext, JointType.HipRight, JointType.KneeRight);
            this.DrawBone(skeleton, drawingContext, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone(skeleton, drawingContext, JointType.AnkleRight, JointType.FootRight);
 
            // Render Joints
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;                    
                }
                else if (joint.TrackingState == JointTrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;                    
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, this.SkeletonPointToScreen(joint.Position), JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Maps a SkeletonPoint to lie within our render space and converts to Point
        /// </summary>
        /// <param name="skelpoint">point to map</param>
        /// <returns>mapped point</returns>
        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }
        
        /// <summary>
        /// Draws a bone line between two joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start drawing from</param>
        /// <param name="jointType1">joint to end drawing at</param>
        private void DrawBone(Skeleton skeleton, DrawingContext drawingContext, JointType jointType0, JointType jointType1)
        {

            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];
        
            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == JointTrackingState.Tracked && joint1.TrackingState == JointTrackingState.Tracked)
            {
                drawPen = this.trackedBonePen;
            }

            drawingContext.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
        }

        /// <summary>
        /// Handles the checking or unchecking of the seated mode combo box
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void CheckBoxSeatedModeChanged(object sender, RoutedEventArgs e)
        {
            if (null != this.sensor)
            {
                if (this.checkBoxSeatedMode.IsChecked.GetValueOrDefault())
                {
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                }
                else
                {
                    this.sensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
                }
            }
        }

        private double sqrDistance2Dxy(point3D p1, point3D p2)
        {
            return (((p1.X - p2.X) * (p1.X - p2.X)) + ((p1.Y - p2.Y) * (p1.Y - p2.Y)));
        }

        //get hand rotation about y-axis
        private double getHandRotationAngle(Skeleton skel)
        {
            double angle;
            angle = System.Math.Abs((skel.Joints[JointType.ElbowRight].Position.Z - skel.Joints[JointType.ShoulderRight].Position.Z) / (skel.Joints[JointType.ElbowRight].Position.X - skel.Joints[JointType.ShoulderRight].Position.X));
            angle = System.Math.Atan(angle) * 180 / System.Math.PI;
            if (skel.Joints[JointType.ElbowRight].Position.Z < skel.Joints[JointType.ShoulderRight].Position.Z)
                angle = -angle;
            return angle;
        }

        private double getJointAngle(JointType linkLeft, JointType linkCenter, JointType linkRight, Skeleton skel)
        {
            point3D end1 = new point3D();
            point3D center = new point3D();
            point3D end2 = new point3D();
            if (linkLeft == JointType.Spine)
            {
                //condition to get new point from spine and shoulder points, only if input point is spine
                point3D shiftedSpineUnderShoulderRight = new point3D();
                shiftedSpineUnderShoulderRight.X = skel.Joints[JointType.ShoulderRight].Position.X;
                shiftedSpineUnderShoulderRight.Y = skel.Joints[JointType.Spine].Position.Y;
                shiftedSpineUnderShoulderRight.Z = skel.Joints[JointType.ShoulderRight].Position.Z;
                end1.store(shiftedSpineUnderShoulderRight.X, shiftedSpineUnderShoulderRight.Y, shiftedSpineUnderShoulderRight.Z);
            }
            else
            {
                end1.store(skel.Joints[linkLeft].Position.X, skel.Joints[linkLeft].Position.Y, skel.Joints[linkLeft].Position.Z);
            }

            center.store(skel.Joints[linkCenter].Position.X, skel.Joints[linkCenter].Position.Y, skel.Joints[linkCenter].Position.Z);
            end2.store(skel.Joints[linkRight].Position.X, skel.Joints[linkRight].Position.Y, skel.Joints[linkRight].Position.Z);

            point3D vec1 = new point3D();
            point3D vec2 = new point3D();

            vec1.X = end1.X - center.X;
            vec1.Y = end1.Y - center.Y;
            vec1.Z = end1.Z - center.Z;

            vec2.X = end2.X - center.X;
            vec2.Y = end2.Y - center.Y;
            vec2.Z = end2.Z - center.Z;

            double cosAngle;

            cosAngle = ( (vec1.X*vec2.X + vec1.Y*vec2.Y + vec1.Z*vec2.Z)/( vec1.distFromOrigin()*vec2.distFromOrigin() ) );

            double angle;
            angle = System.Math.Acos(cosAngle) * 180 / System.Math.PI;
            

            //double m1 = ((end1.Y - center.Y) / (end1.X - center.X));
            //double m2 = ((end2.Y - center.Y) / (end2.X - center.X));
            //double O1 = System.Math.Atan(m1) * 180 / System.Math.PI;
            //double O2 = System.Math.Atan(m2) * 180 / System.Math.PI;
            //double angle;

            //double temp1 = sqrDistance2Dxy(end1, end2);
            //double temp2 = sqrDistance2Dxy(end1, center);
            //double temp3 = sqrDistance2Dxy(center, end2);

            //if (sqrDistance2Dxy(end1, end2) > sqrDistance2Dxy(end1, center) + sqrDistance2Dxy(center, end2))
            //{
            //    angle = 180 - System.Math.Abs(O1 - O2);
            //}
            //else
            //{
            //    angle = System.Math.Abs(O1 - O2);
            //}

            return angle;
        }

        private void btnContinue_Click(object sender, RoutedEventArgs e)
        {
            buffercount = 0;
            buttonOn = false;
            this.btnContinue.IsEnabled = false;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {

            using (System.IO.StreamWriter stream = new System.IO.StreamWriter("D:\\Kaustuv\\datasets\\subject1_set3.txt", true))
            {
               for(int i=0;i< buffercount; i++)
                    stream.WriteLine( buffer[i,0].ToString() + ", " + buffer[i,1].ToString() + ";");
            }
        }

        private void readTrainingData()
        {
            try
            {
                using (System.IO.StreamReader stream = new System.IO.StreamReader("D:\\Kaustuv\\datasets\\trainedData\\frame1membership.txt"))
                {
                    for (int i = 0; i < 361; i++)
                        for (int j = 0; j < 361; j++)
                        {
                            fbuffer = new char[10];
                            stream.Read(fbuffer, 0, 10);
                            string fstring = new string(fbuffer);
                            frame1Mem[i, j] = System.Convert.ToDouble(fstring);
                        }
                }

                using (System.IO.StreamReader stream = new System.IO.StreamReader("D:\\Kaustuv\\datasets\\trainedData\\frame2membership.txt"))
                {
                    for (int i = 0; i < 361; i++)
                        for (int j = 0; j < 361; j++)
                        {
                            fbuffer = new char[10];
                            stream.Read(fbuffer, 0, 10);
                            string fstring = new string(fbuffer);
                            frame2Mem[i, j] = System.Convert.ToDouble(fstring);
                        }
                }

                using (System.IO.StreamReader stream = new System.IO.StreamReader("D:\\Kaustuv\\datasets\\trainedData\\frame3membership.txt"))
                {
                    for (int i = 0; i < 361; i++)
                        for (int j = 0; j < 361; j++)
                        {
                            fbuffer = new char[10];
                            stream.Read(fbuffer, 0, 10);
                            string fstring = new string(fbuffer);
                            frame3Mem[i, j] = System.Convert.ToDouble(fstring);
                        }
                }
            }
            catch (Exception ex)
            {
                Logger.addLog("Exception!\n" + ex.Message, true);
            }
        }

        private void btnYes_Click(object sender, RoutedEventArgs e)
        {
            weight[detectedFrame, currentAction] += 3;
            //for (int i = 0; i < 3; i++ )
            //{
            //    if (i != currentAction)
            //        weight[detectedFrame, i] -= 1;
            //}
            //currentAction++;
            //lblAction.Content = "ACTION " + (currentAction+1).ToString();
            Logger.addLog("----------------WEIGHT MATRIX----------------");
            for (int i = 0; i < 3; i++)
                Logger.addLog(weight[i, 0].ToString() + "(F" + (i + 1).ToString() + ",A1)" + "  " + weight[i, 1].ToString() + "(F" + (i + 1).ToString() + ",A2)" + "  " + weight[i, 2].ToString() + "(F" + (i + 1).ToString() + ",A3)");
            Logger.addLog("-------------------------------------------------");
        }

        private void btnNo_Click(object sender, RoutedEventArgs e)
        {
            weight[detectedFrame, currentAction] -= 2;
            if (weight[detectedFrame, currentAction] < 0)
                weight[detectedFrame, currentAction] = 0;
            //for (int i = 0; i < 3; i++)
            //{
            //    if (i != currentAction)
            //        weight[detectedFrame, i] += 1;
            //}
            currentAction = (currentAction + 1)%3;
            lblAction.Content = "ACTION " + (currentAction + 1).ToString();
            Logger.addLog("----------------WEIGHT MATRIX----------------");
            for (int i = 0; i < 3; i++)
                Logger.addLog(weight[i, 0].ToString() + "(F" + (i + 1).ToString() + ",A1)" + "  " + weight[i, 1].ToString() + "(F" + (i + 1).ToString() + ",A2)" + "  " + weight[i, 2].ToString() + "(F" + (i + 1).ToString() + ",A3)");
            Logger.addLog("-------------------------------------------------");
        }

        private void btnTrajAdd_Click(object sender, RoutedEventArgs e)
        {
            CTrajectoryInfo pointTraj = new CTrajectoryInfo();
            try
            {
                if (Jaco.JacoIsReady())
                {
                    pointTraj.UserPosition.AnglesJoints = Jaco.ConfigurationsManager.GetJointPositions();
                    pointTraj.UserPosition.HandMode = Kinova.DLL.Data.Jaco.CJacoStructures.HandMode.PositionMode;
                    pointTraj.UserPosition.PositionType = Kinova.DLL.Data.Jaco.CJacoStructures.PositionType.AngularPosition;
                }
                else
                {
                    Logger.addLog("Jaco not ready - unable to get joint positions", true);
                    return;
                }

            }
            catch (Exception)
            {
                Logger.addLog("Error in Jaco - unable to get joint positions", true);
                return;
            }

            //pointTraj.UserPosition.AnglesJoints.Angle[(int)JointType.ElbowRight - 7] = (float)anglesForJacoTraining.elbowAngle;
            //pointTraj.UserPosition.AnglesJoints.Angle[(int)JointType.ShoulderRight - 7] = (float)anglesForJacoTraining.shoulderAngle;
            //pointTraj.UserPosition.AnglesJoints.Angle[7 - 7] = (float)anglesForJacoTraining.handRotationAngle;

            trajectory[cmbTrajectory.SelectedIndex].Add(pointTraj);
            Logger.addLog("Point added in Trajectory " + (cmbTrajectory.SelectedIndex + 1).ToString());
        }

        private void btnTrajClear_Click(object sender, RoutedEventArgs e)
        {
            trajectory = new CPointsTrajectory[3];
            for (int i = 0; i < 3; i++)
                trajectory[i] = new CPointsTrajectory();
            Logger.addLog("All existing trajectories cleared!");
        }

    }
}

public class point3D
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }

    public void store(double pX, double pY, double pZ)
    {
        this.X = pX;
        this.Y = pY;
        this.Z = pZ;
    }
    public double distFromOrigin()
    {

        return System.Math.Sqrt(X * X + Y * Y + Z * Z);
    }

};