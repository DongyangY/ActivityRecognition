using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace ActivityRecognition
{
    public class Activity
    {
        public bool IsDynamicArea;
        public string TemplateName;
        public System.Windows.Rect Area;
        public BodyOrientation.Orientations BodyOrientations;
        public LinkedList<Object.Objects> Objects;
        public LinkedList<Requirement> Requirements;
        public string Name;
        public int MinPeopleCount;
        public LinkedList<Posture> Postures;

        // For activity record
        public bool IsActive;
        public bool IsRecording;
        public int RecordRow;
        public string LastTime;

        public Activity(string templateName, BodyOrientation.Orientations bodyOrientations, LinkedList<Posture> postures, LinkedList<Object.Objects> objects, LinkedList<Requirement> requirements, string name, int minPeopleCount)
        {
            IsDynamicArea = true;
            TemplateName = templateName;
            BodyOrientations = bodyOrientations;
            Postures = postures;
            Objects = objects;
            Requirements = requirements;
            Name = name;
            MinPeopleCount = minPeopleCount;
            IsActive = false;
            IsRecording = false;
            RecordRow = 0;
            LastTime = System.DateTime.Now.ToString(@"HHmmss");
        }

        public Activity(System.Windows.Rect area, BodyOrientation.Orientations bodyOrientations, LinkedList<Posture> postures, LinkedList<Object.Objects> objects, LinkedList<Requirement> requirements, string name, int minPeopleCount)
        {
            Area = area;
            BodyOrientations = bodyOrientations;
            Postures = postures;
            Objects = objects;
            Requirements = requirements;
            Name = name;
            MinPeopleCount = minPeopleCount;
            IsActive = false;
            IsRecording = false;
            RecordRow = 0;
            LastTime = System.DateTime.Now.ToString(@"HHmmss");
        }

        public bool IsRequirementsSatisfied(Person[] persons, System.Windows.Controls.Canvas canvas)
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
                if (!ObjectDetector.Objects[obj].IsInUse) isSatisfied = false;
            }

            return isSatisfied;
        }

        public bool IsMoreThanMinPeopleCount(Person[] persons, System.Windows.Controls.Canvas canvas)
        {
            int peopleCount = 0;

            foreach (Person person in persons)
            {
                if (person.IsTracked && IsAreaSatisfied(person, canvas))
                {
                    peopleCount++;
                }
            }

            return (peopleCount >= this.MinPeopleCount);
        }

        public bool IsAreaSatisfied(Person person, System.Windows.Controls.Canvas canvas)
        {
            Point p = Transformation.ConvertGroundPlaneToCanvas(person.Position, canvas);

            if (this.IsDynamicArea)
            {
                Template template = null;
                foreach (Template t in TemplateDetector.templates)
                {
                    if (t.Name.Equals(this.TemplateName)) template = t;
                }     
                
                return (template != null) ? template.location.Contains(p) : false;        
            }
            else
            {
                return this.Area.Contains(p);
            }
        }

        public bool IsPostureSatisfied(Person person)
        {
            bool isSatisfied = false;
            foreach (Posture posture_activity in this.Postures)
            {
                foreach(Posture posture_person in person.postures)
                {
                    if (posture_activity.Name.Equals(posture_person.Name)) isSatisfied = true;
                }
            }
            return isSatisfied;
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
                            && activity.IsAreaSatisfied(person, canvas)
                            && (activity.BodyOrientations & person.Orientation) != 0
                            && activity.IsPostureSatisfied(person)
                            && activity.IsObjectUseSatisfied()
                            && activity.IsRequirementsSatisfied(persons, canvas))
                        {
                            strBuilder.Append("\n" + activity.Name);
                        }

                        System.Console.WriteLine("{0}, {1}, {2}. {3}, {4}, {5}, {6}, {7}", person.ID, activity.Name,
                            activity.IsMoreThanMinPeopleCount(persons, canvas),
                            activity.IsAreaSatisfied(person, canvas),
                            (activity.BodyOrientations & person.Orientation) != 0,
                            activity.IsPostureSatisfied(person),
                            activity.IsObjectUseSatisfied(),
                            activity.IsRequirementsSatisfied(persons, canvas));
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
                            && activity.IsAreaSatisfied(person, canvas)
                            && (activity.BodyOrientations & person.Orientation) != 0
                            && activity.IsPostureSatisfied(person)
                            && activity.IsObjectUseSatisfied()
                            && activity.IsRequirementsSatisfied(persons, canvas))
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
