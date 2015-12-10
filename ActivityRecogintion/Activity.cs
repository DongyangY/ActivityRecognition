using System.Collections.Generic;
using System.Text;

namespace ActivityRecogintion
{
    public class Activity
    {
        public System.Windows.Rect Area;
        public BodyOrientation.Orientations BodyOrientations;
        public LinkedList<Object.Objects> Objects;
        public LinkedList<Requirement> Requirements;
        public string Name;
        public int MinPeopleCount;

        // For activity record
        public bool IsActive;
        public bool IsRecording;
        public int RecordRow;
        public string LastTime;

        public Activity(System.Windows.Rect area, BodyOrientation.Orientations bodyOrientations, LinkedList<Object.Objects> objects, LinkedList<Requirement> requirements, string name, int minPeopleCount)
        {
            Area = area;
            BodyOrientations = bodyOrientations;
            Objects = objects;
            Requirements = requirements;
            Name = name;
            MinPeopleCount = minPeopleCount;
            IsActive = false;
            IsRecording = false;
            RecordRow = 0;
            LastTime = System.DateTime.Now.ToString(@"HHmmss");
        }

        public bool isRequirementsSatisfied(Person[] persons, System.Windows.Controls.Canvas canvas)
        {
            foreach (Requirement req in Requirements)
            {
                if (!req.isSatisfied(Area, persons, canvas)) return false;
            }

            return true;
        }

        public bool IsObjectUseSatisfied()
        {
            bool isSatisfied = true;

            foreach (Object.Objects obj in this.Objects)
            {
                if (!RFID.Objects[obj].IsInUse) isSatisfied = false;
            }

            return isSatisfied;
        }

        public bool IsMoreThanMinPeopleCount(Person[] persons, System.Windows.Controls.Canvas canvas)
        {
            int peopleCount = 0;

            foreach (Person person in persons)
            {
                if (person.IsTracked && this.Area.Contains(Transformation.ConvertGroundPlaneToCanvas(person.Position, canvas)))
                {
                    peopleCount++;
                }
            }

            return (peopleCount >= this.MinPeopleCount);
        }

        public static void DecideActivityForPeople(LinkedList<Activity> activities, Person[] persons, System.Windows.Controls.Canvas canvas)
        {
            foreach (Person person in persons)
            {
                if (person.IsTracked)
                {
                    StringBuilder strBuilder = new StringBuilder();
                    foreach (Activity activity in activities)
                    {
                        if (activity.IsMoreThanMinPeopleCount(persons, canvas)
                            && activity.Area.Contains(Transformation.ConvertGroundPlaneToCanvas(person.Position, canvas))
                            && (activity.BodyOrientations & person.Orientation) != 0
                            && activity.IsObjectUseSatisfied()
                            && activity.isRequirementsSatisfied(persons, canvas))
                        {
                            strBuilder.Append("\n" + activity.Name);
                        }
                    }
                    person.Activities = strBuilder.ToString();
                }
            }
        }

        public static void DecideStatusOfActivity(LinkedList<Activity> activities, Person[] persons, System.Windows.Controls.Canvas canvas)
        {
            foreach (Activity activity in activities)
            {
                activity.IsActive = false;

                foreach (Person person in persons)
                {
                    if (person.IsTracked)
                    {
                        if (activity.IsMoreThanMinPeopleCount(persons, canvas)
                            && activity.Area.Contains(Transformation.ConvertGroundPlaneToCanvas(person.Position, canvas))
                            && (activity.BodyOrientations & person.Orientation) != 0
                            && activity.IsObjectUseSatisfied()
                            && activity.isRequirementsSatisfied(persons, canvas))
                        {
                            activity.IsActive = true;
                            break;
                        }
                    }
                }
            }
        }
    }
}
