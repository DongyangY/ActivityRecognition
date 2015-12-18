//------------------------------------------------------------------------------
// <summary>
// Collection of transformation methods
// </summary>
// <author> Dongyang Yao (dongyang.yao@rutgers.edu) </author>
//------------------------------------------------------------------------------

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;

namespace ActivityRecognition
{
    public static class Transformation
    {
        /// <summary>
        /// Convert from Kinect Gound coordinate to canvas coordinate
        /// </summary>
        /// <param name="point"></param>
        /// <param name="canvas"></param>
        /// <returns></returns>
        public static Point ConvertGroundPlaneToCanvas(Point point, System.Windows.Controls.Canvas canvas)
        {
            return new Point(point.X + canvas.Width / 2, canvas.Height - point.Y);
        }

        /// <summary>
        /// Convert from Kinect Gound coordinate to canvas coordinate
        /// </summary>
        /// <param name="point"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Point ConvertGroundPlaneToCanvas(Point point, double width, double height)
        {
            return new Point(point.X + width / 2, height - point.Y);
        }

        /// <summary>
        /// Convert location in a 2D bitmap to index in a array
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static int Convert2DToArray(int width, int height, float x, float y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return -1;
            return (int)(x + 0.5f) + (int)(y + 0.5f) * width;
        }

        /// <summary>
        /// Convert point from ground space to ground plane - centimeter
        /// </summary>
        /// <param name="headPositionGround"></param>
        /// <param name="person"></param>
        public static void ConvertGroundSpaceToPlane(CameraSpacePoint headPositionGround, Person person)
        {
            person.Position.X = - headPositionGround.X * 100;
            person.Position.Y = headPositionGround.Z * 100;
        }

        /// <summary>
        /// Convert point from ground space to ground plane - centimeter
        /// </summary>
        /// <param name="cameraPoint"></param>
        /// <returns></returns>
        public static Point ConvertGroundSpaceToPlane(CameraSpacePoint cameraPoint)
        {
            Point point = new Point();
            point.X = -cameraPoint.X * 100;
            point.Y = cameraPoint.Z * 100;
            return point;
        }

        /// <summary>
        /// Convert point from camera space to ground space
        /// </summary>
        /// <param name="tiltAngleDegree"></param>
        /// <param name="tiltDown"></param>
        /// <param name="headPositionCamera"></param>
        /// <returns></returns>
        public static CameraSpacePoint RotateBackFromTilt(double tiltAngleDegree, bool tiltDown, CameraSpacePoint headPositionCamera)
        {
            tiltAngleDegree = tiltDown ? -tiltAngleDegree : tiltAngleDegree;
            double tiltAngle = tiltAngleDegree / 180 * Math.PI;
            CameraSpacePoint headPositionGround = new CameraSpacePoint();
            headPositionGround.X = headPositionCamera.X;
            headPositionGround.Y = (float)(headPositionCamera.Z * Math.Sin(tiltAngle) + headPositionCamera.Y * Math.Cos(tiltAngle));
            headPositionGround.Z = (float)(headPositionCamera.Z * Math.Cos(tiltAngle) - headPositionCamera.Y * Math.Sin(tiltAngle));
            return headPositionGround;
        }

        /// <summary>
        /// Convert rotation quaternion to Euler angles in degrees
        /// </summary>
        /// <param name="rotQuaternion"></param>
        /// <param name="pitch"></param>
        /// <param name="yaw"></param>
        /// <param name="roll"></param>
        public static void QuaternionToRotationMatrix(Vector4 rotQuaternion, out double pitch, out double yaw, out double roll)
        {
            double x = rotQuaternion.X;
            double y = rotQuaternion.Y;
            double z = rotQuaternion.Z;
            double w = rotQuaternion.W;

            pitch = Math.Atan2(2 * ((y * z) + (w * x)), (w * w) - (x * x) - (y * y) + (z * z)) / Math.PI * 180.0;
            yaw = Math.Asin(2 * ((w * y) - (x * z))) / Math.PI * 180.0;
            roll = Math.Atan2(2 * ((x * y) + (w * z)), (w * w) + (x * x) - (y * y) - (z * z)) / Math.PI * 180.0;
        }

        /// <summary>
        /// Convert pixels to image source
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="depthPixels"></param>
        /// <returns></returns>
        public static ImageSource ToBitmap(int width, int height, byte[] depthPixels)
        {
            PixelFormat format = PixelFormats.Bgr32;

            byte[] pixels = new byte[width * height * (format.BitsPerPixel + 7) / 8];

            int colorIndex = 0;
            for (int depthIndex = 0; depthIndex < depthPixels.Length; ++depthIndex)
            {
                byte depth = depthPixels[depthIndex];

                byte intensity = depth;

                pixels[colorIndex++] = intensity;
                pixels[colorIndex++] = intensity;
                pixels[colorIndex++] = intensity;

                ++colorIndex;
            }

            int stride = width * format.BitsPerPixel / 8;

            return BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);
        }

        /// <summary>
        /// Convert depth frame to image source
        /// </summary>
        /// <param name="frame"></param>
        /// <param name="depthPixels"></param>
        /// <param name="doCopy"></param>
        /// <returns></returns>
        public static ImageSource ToBitmap(DepthFrame frame, ushort[] depthPixels, bool doCopy)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;
            PixelFormat format = PixelFormats.Bgr32;

            ushort minDepth = frame.DepthMinReliableDistance;
            ushort maxDepth = ushort.MaxValue;

            byte[] pixels = new byte[width * height * (format.BitsPerPixel + 7) / 8];

            if (doCopy) frame.CopyFrameDataToArray(depthPixels);

            int colorIndex = 0;
            for (int depthIndex = 0; depthIndex < depthPixels.Length; ++depthIndex)
            {
                ushort depth = depthPixels[depthIndex];

                byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? (depth / (8000 / 256)) : 0);

                pixels[colorIndex++] = intensity;
                pixels[colorIndex++] = intensity;
                pixels[colorIndex++] = intensity;

                ++colorIndex;
            }

            int stride = width * format.BitsPerPixel / 8;

            return BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);
        }

        /// <summary>
        /// Count number of undefined points in a rectangle
        /// </summary>
        /// <param name="depthPixels"></param>
        /// <param name="center"></param>
        /// <param name="sideLength"></param>
        /// <param name="displayWidth"></param>
        /// <returns></returns>
        public static int CountZeroInRec(ushort[] depthPixels, DepthSpacePoint center, int sideLength, int displayWidth)
        {
            int zeroCount = 0;
            for (int row = (int)(center.Y - sideLength / 2); row < (int)(center.Y + sideLength / 2); ++row)
            {
                for (int col = (int)(center.X - sideLength / 2); col < (int)(center.X + sideLength / 2); ++col)
                {
                    int index = col + row * displayWidth;
                    if (index >= 0 && index < depthPixels.Length)
                    {
                        if ((int)depthPixels[col + row * displayWidth] == 0) zeroCount++;
                    }                   
                }
            }
            return zeroCount;
        }

        /// <summary>
        /// Get number of tracked person
        /// </summary>
        /// <param name="persons"></param>
        /// <returns></returns>
        public static int GetNumberOfPeople(Person[] persons)
        {
            int num = 0;
            foreach (Person person in persons)
            {
                if (person.IsTracked) num++;
            }
            return num;
        }

    }
}
