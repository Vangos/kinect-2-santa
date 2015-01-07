using Microsoft.Kinect.Face;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WindowsPreview.Kinect;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Kinect2Santa
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // ATTENTION: These are the original image dimensions and may vary if you change the image!!!
        readonly double ORIGINAL_IMAGE_WIDTH = 632.0;
        readonly double ORIGINAL_IMAGE_HEIGHT = 988.0;
        readonly double ORIGINAL_DISTANCE_EYES = 282.0;
        readonly double ORIGINAL_DISTANCE_EYES_TOP = 272.0;

        KinectSensor _sensor = null;
        ColorFrameReader _colorReader = null;
        BodyFrameReader _bodyReader = null;
        IList<Body> _bodies = null;

        FaceFrameSource _faceSource = null;
        FaceFrameReader _faceReader = null;

        public MainPage()
        {
            InitializeComponent();

            _sensor = KinectSensor.GetDefault();

            if (_sensor != null)
            {
                _sensor.Open();

                _bodies = new Body[_sensor.BodyFrameSource.BodyCount];

                _colorReader = _sensor.ColorFrameSource.OpenReader();
                _colorReader.FrameArrived += ColorReader_FrameArrived;
                _bodyReader = _sensor.BodyFrameSource.OpenReader();
                _bodyReader.FrameArrived += BodyReader_FrameArrived;

                _faceSource = new FaceFrameSource(_sensor, 0, FaceFrameFeatures.PointsInColorSpace);
                _faceReader = _faceSource.OpenReader();
                _faceReader.FrameArrived += FaceReader_FrameArrived;
            }
        }

        void ColorReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            using (var frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    camera.Source = frame.ToBitmap();
                }
            }
        }

        void BodyReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            using (var frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    frame.GetAndRefreshBodyData(_bodies);

                    Body body = _bodies.Where(b => b.IsTracked).FirstOrDefault();

                    if (!_faceSource.IsTrackingIdValid)
                    {
                        if (body != null)
                        {
                            _faceSource.TrackingId = body.TrackingId;
                        }
                    }
                }
            }
        }

        void FaceReader_FrameArrived(object sender, FaceFrameArrivedEventArgs e)
        {
            using (var frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    FaceFrameResult result = frame.FaceFrameResult;

                    if (result != null)
                    {
                        // Get the face points, mapped in the color space.
                        Point eyeLeft = result.FacePointsInColorSpace[FacePointType.EyeLeft];
                        Point eyeRight = result.FacePointsInColorSpace[FacePointType.EyeRight];

                        // Find the middle point.
                        Point middle = new Point((eyeRight.X + eyeLeft.X) / 2.0, (eyeRight.Y + eyeLeft.Y) / 2.0);

                        // Calculate the distance between the eyes.
                        double distance = Math.Sqrt(Math.Pow(eyeLeft.X - eyeRight.X, 2) + Math.Pow(eyeLeft.Y - eyeRight.Y, 2));

                        // Calculate the new width and height of the image, to give the illusion of 3D.
                        double width = ORIGINAL_IMAGE_WIDTH * distance / ORIGINAL_DISTANCE_EYES;
                        double height = width * ORIGINAL_IMAGE_HEIGHT / ORIGINAL_IMAGE_WIDTH;

                        // Calculate the angle of the two points.
                        double angle = Math.Atan2(eyeRight.Y - eyeLeft.Y, eyeRight.X - eyeLeft.X) * 180.0 / Math.PI;

                        // Transform the image!
                        image.Width = width;
                        transform.Angle = angle;

                        // Position the image!
                        Canvas.SetLeft(image, middle.X - width / 2.0);
                        Canvas.SetTop(image, middle.Y - height / (ORIGINAL_IMAGE_HEIGHT / ORIGINAL_DISTANCE_EYES_TOP));
                    }
                }
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_colorReader != null)
            {
                _colorReader.Dispose();
                _colorReader = null;
            }

            if (_bodyReader != null)
            {
                _bodyReader.Dispose();
                _bodyReader = null;
            }

            if (_faceReader != null)
            {
                _faceReader.Dispose();
                _faceReader = null;
            }

            if (_faceSource != null)
            {
                _faceSource = null;
            }

            if (_sensor != null)
            {
                _sensor.Close();
            }
        }
    }
}