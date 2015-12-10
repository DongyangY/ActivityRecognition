/*
 * Become much more slower when number of records increased
*/

using Excel = Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.IO;

namespace ActivityRecogintion
{
    public class Record
    {
        public static string StartTime;
        public static int ActivityRecordLastLine;
        public static int PositionRecordLastLine;

        public static void RecordPosition(Person[] persons, int maxNumPeople, CSV recorder)
        {
            string fileName = @"Records\Position\" + StartTime + ".csv";

            if (!File.Exists(fileName))
            {
                using (FileStream fs = File.Create(fileName)) { }
                recorder.Open(new StreamReader(fileName));
                recorder[0, 0] = "Identity";
                recorder[1, 0] = "X";
                recorder[2, 0] = "Y";
                PositionRecordLastLine = 1;
            }
            else
            {
                recorder.Open(new StreamReader(fileName));
            }

            for (int i = 0; i < maxNumPeople; ++i)
            {
                if (persons[i].IsTracked)
                {
                    recorder[0, PositionRecordLastLine] = i.ToString();
                    recorder[1, PositionRecordLastLine] = persons[i].Position.X.ToString();
                    recorder[2, PositionRecordLastLine++] = persons[i].Position.Y.ToString();
                }
            }

            recorder.Save(new StreamWriter(fileName));
        }

        public static void RecordActivity(LinkedList<Activity> activities, CSV recorder)
        {
            string fileName = @"Records\Activity\" + StartTime + ".csv";

            if (!File.Exists(fileName))
            {
                using (FileStream fs = File.Create(fileName)) { }
                recorder.Open(new StreamReader(fileName));
                recorder[0, 0] = "Name";
                recorder[1, 0] = "Start";
                recorder[2, 0] = "End";
                ActivityRecordLastLine = 1;
            }
            else
            {
                recorder.Open(new StreamReader(fileName));
            }

            foreach (Activity activity in activities)
            {
                if (activity.IsActive)
                {
                    if (activity.IsRecording)
                    {
                        activity.LastTime = DateTime.Now.ToString(@"HHmmss");
                        recorder[2, activity.RecordRow] = DateTime.Now.ToString(@"HH:mm:ss");
                    }
                    else
                    {
                        activity.IsRecording = true;
                        activity.LastTime = DateTime.Now.ToString(@"HHmmss");
                        recorder[0, ActivityRecordLastLine] = activity.Name;
                        recorder[1, ActivityRecordLastLine] = DateTime.Now.ToString(@"HH:mm:ss");
                        recorder[2, ActivityRecordLastLine] = DateTime.Now.ToString(@"HH:mm:ss");
                        activity.RecordRow = ActivityRecordLastLine++;
                    }
                }
                else
                {
                    int now = int.Parse(DateTime.Now.ToString(@"HHmmss"));
                    if (now > int.Parse(activity.LastTime))
                    {
                        activity.IsRecording = false;
                    }
                }
            }

            recorder.Save(new StreamWriter(fileName));
        }

    }
}
