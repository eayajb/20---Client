using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using _20__Client;
using System.Windows.Media.Media3D;
using System.ComponentModel;

namespace _20___Client
{
    public partial class MainWindow : Window
    {
        Client clientHelper;

        KinectSensor kinect = null;
        MultiSourceFrameReader msfr = null;
        FrameDescription colorFrameDescription = null;
        IList<Body> bodies = null;

        Canvas canvas;
        double canvasWidth, canvasHeight;
        double referenceFrameWidth, referenceFrameHeight, referenceFrameDepth;

        public MainWindow()
        {
            this.clientHelper = new Client();

            this.kinect = KinectSensor.GetDefault();
            this.msfr = this.kinect.OpenMultiSourceFrameReader( FrameSourceTypes.Body | FrameSourceTypes.Depth );

            msfr.MultiSourceFrameArrived += msfr_MultiSourceFrameArrived;

            this.colorFrameDescription = this.kinect.ColorFrameSource.FrameDescription;

            this.referenceFrameHeight = colorFrameDescription.Height;
            this.referenceFrameWidth = colorFrameDescription.Width;
            this.referenceFrameDepth = this.kinect.DepthFrameSource.DepthMaxReliableDistance;

            kinect.Open();

            Loaded += MainWindow_Loaded;

            this.DataContext = this;

            InitializeComponent();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.canvas = bodyPointsCanvas;
            this.canvasWidth = canvas.Width;
            this.canvasHeight = canvas.Height;

            double[] referenceData = new double[10];
            referenceData[0] = referenceFrameDepth;
            referenceData[1] = referenceFrameHeight;
            referenceData[2] = referenceFrameWidth;
            referenceData[3] = canvasHeight;
            referenceData[4] = canvasWidth;

            this.clientHelper.SendReferenceData(referenceData);
        }

        string inputState;

        private void msfr_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            ///READ INPUT STATE FROM CLIENT
            inputState = Client.inputState;

            ///DECLARE FRAMES
            BodyFrame bodyFrame = null;
            DepthFrame depthFrame = null;

            ///ACQUIRE AND VALIDATE FRAME
            MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();

            if (multiSourceFrame == null)
            {
                return;
            }

            try
            {
                depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame();

                if (inputState == "d")
                {
                    var depthDesc = depthFrame.FrameDescription;
                    ushort[] depthData = new ushort[depthDesc.LengthInPixels];
                    depthFrame.CopyFrameDataToArray(depthData);
                }


                bodyFrame = multiSourceFrame.BodyFrameReference.AcquireFrame();

                if ((bodyFrame == null))
                {
                    return;
                }

                ///PROCESS BODY DATA

                this.bodies = new Body[bodyFrame.BodyCount];

                ///REFRESH BODY DATA
                bodyFrame.GetAndRefreshBodyData(this.bodies);

                foreach (Body body in this.bodies)
                {
                    if (body != null)
                    {
                        if (body.IsTracked)
                        {
                            Dictionary<JointType, Point3D> tdPoints = new Dictionary<JointType, Point3D>();
                            List<ColorSpacePoint> csPoints = new List<ColorSpacePoint>();

                            foreach (JointType type in body.Joints.Keys)
                            {
                                Joint joint = body.Joints[type];
                                Point3D point = new Point3D(joint.Position.X, joint.Position.Y, joint.Position.Z);
                                ColorSpacePoint csp = this.kinect.CoordinateMapper.MapCameraPointToColorSpace(joint.Position);

                                ///GET LIST OF JOINT POSITIONS
                                tdPoints.Add(type, point);

                                ///CANNOT BE SURE THERE WILL BE DATA IF "TRACKED" IS USED
                                if (joint.TrackingState == TrackingState.Tracked)
                                {
                                    ///CALCULATE POSITION TO DRAW POINT
                                    csPoints.Add(csp);
                                }
                            }

                            DrawPoints(csPoints);

                            ///TRANSFER DATA TO SERVER
                            if (inputState == "t")
                                this.clientHelper.AddBodyData( BiometricID(tdPoints), tdPoints, csPoints );
                        }
                    }
                }
            }
            finally
            {
                if( inputState == "t")
                    this.clientHelper.SendBodyData();
                Client.inputState = "z";

                ///DISPOSE
                if (bodyFrame != null)
                {
                    bodyFrame.Dispose();
                }

                if (depthFrame != null)
                    depthFrame.Dispose();
            }
        }

        private double BiometricID(Dictionary<JointType, Point3D> bodyData)
        {
            double ID = 1;

            /// CONSTRUCT ALGORITHM TO FIND A UNIQUE IDENTIFIER

            return ID;
        }

        private void DrawPoints(List<ColorSpacePoint> bodyPoints)
        {
            canvas.Children.Clear();
            foreach (ColorSpacePoint point in bodyPoints)
            {
                ///NEW ELLIPSE REQUIRED FOR EACH CHILD ELEMENT ADDED
                Ellipse ellipse = new Ellipse
                {
                    Width = 20,
                    Height = 20,
                    Fill = Brushes.Red
                };

                if (point.X > 0 && point.Y > 0)
                {
                    ///CONVERT POSITION TO CANVAS
                    Double convX = this.canvasWidth * (point.X / this.referenceFrameWidth);
                    Double convY = this.canvasHeight * (point.Y / this.referenceFrameHeight);

                    ///SET POSITION AND ADD TO CANVAS
                    Canvas.SetLeft(ellipse, convX - (ellipse.Width / 2));
                    Canvas.SetTop(ellipse, convY - (ellipse.Height / 2));

                    canvas.Children.Add(ellipse);
                }
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.msfr != null)
            {
                // MultiSourceFrameReder is IDisposable
                this.msfr.Dispose();
                this.msfr = null;
            }

            if (this.kinect != null)
            {
                this.kinect.Close();
                this.kinect = null;
            }
        }
    }
}