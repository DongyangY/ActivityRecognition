using Microsoft.Kinect;
using System;

namespace ActivityRecogintion
{
    public class BodyOrientation
    {
        public static readonly double ShoulderDistance = 0.4064; // Meter
        public static readonly double OrientationBorder = 2;
        public static double MaxShoulderZDifference = 0.274; // Defined

        public enum Orientations
        {
            Front = 0x01,  // 0001
            Left = 0x02,   // 0010
            Right = 0x04,  // 0100
            Back = 0x08    // 1000
        };

        public static Orientations ConvertIntToOrientations(int number)
        {
            Orientations orientations = 0;
            if ((number & (int)Orientations.Front) != 0) orientations |= Orientations.Front;
            if ((number & (int)Orientations.Left) != 0) orientations |= Orientations.Left;
            if ((number & (int)Orientations.Right) != 0) orientations |= Orientations.Right;
            if ((number & (int)Orientations.Back) != 0) orientations |= Orientations.Back;
            return orientations;
        }

        public static void DecideOrientation(CameraSpacePoint leftShoulder, CameraSpacePoint rightShoulder, Person person, int zeroCount, System.Windows.Controls.Canvas canvas)
        {
            //if (Math.Abs(leftShoulder.Z - rightShoulder.Z) > MaxShoulderZDifference) MaxShoulderZDifference = Math.Abs(leftShoulder.Z - rightShoulder.Z);
            if (Math.Abs(leftShoulder.Z - rightShoulder.Z) < (MaxShoulderZDifference / OrientationBorder))
            {
                double y = person.Position.Y;
                double length = (canvas.Height - Plot.MinReliableDistance) / 4;
                if (y >= 0 && y <= length + Plot.MinReliableDistance)
                {
                    person.Orientation = zeroCount >= 1 ? Orientations.Back : Orientations.Front;
                }
                if (y >= length + Plot.MinReliableDistance && y <= length * 2 + Plot.MinReliableDistance)
                {
                    person.Orientation = zeroCount >= 2 ? Orientations.Back : Orientations.Front;
                }
                if (y >= length * 2 + Plot.MinReliableDistance && y <= length * 3 + Plot.MinReliableDistance)
                {
                    person.Orientation = zeroCount >= 30 ? Orientations.Back : Orientations.Front;
                }
                if (y >= length * 3 + Plot.MinReliableDistance && y <= length * 4 + Plot.MinReliableDistance)
                {
                    person.Orientation = zeroCount >= 55 ? Orientations.Back : Orientations.Front;
                }
            }
            else person.Orientation = leftShoulder.Z > rightShoulder.Z ? Orientations.Right : Orientations.Left;
        }
    }
}
