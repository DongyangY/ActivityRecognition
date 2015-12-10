using System;
using System.Collections.Generic;
using System.IO;

namespace ActivityRecogintion
{
    class RecordTxt
    {
        public static string StartTime;

        public static void RecordActivity(LinkedList<Activity> activities)
        {
            string fileName = @"Records\Activity\" + StartTime + ".txt";
            string time = DateTime.Now.ToString(@"HH:mm:ss");

            using (StreamWriter writer = new StreamWriter(fileName, true))
            {
                foreach (Activity activity in activities)
                {
                    writer.WriteLine("{0}, {1}, {2}", activity.Name, time, activity.IsActive);
                }
            }
        }

        public static void RecordPosition(Person[] persons)
        {
            string fileName = @"Records\Position\" + StartTime + ".txt";
            string time = DateTime.Now.ToString(@"HH:mm:ss");

            using (StreamWriter writer = new StreamWriter(fileName, true))
            {
                foreach (Person person in persons)
                {
                    if (person.IsTracked)
                        writer.WriteLine("{0}, {1}, {2}, {3}", person.ID, time, person.Position.X, person.Position.Y);
                }
            }
        }
    }
}
