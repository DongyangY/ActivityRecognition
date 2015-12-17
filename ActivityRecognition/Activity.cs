//------------------------------------------------------------------------------
// <summary>
// The structure of a activity
// Methods for activity detection
// </summary>
// <author> Dongyang Yao (dongyang.yao@rutgers.edu) </author>
//------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace ActivityRecognition
{
    public class Activity
    {
        /// <summary>
        /// Selected area or template surrounding
        /// </summary>
        public bool IsDynamicArea;

        /// <summary>
        /// The template name if dynamic area
        /// </summary>
        public string TemplateName;

        /// <summary>
        /// The area if static area
        /// </summary>
        public System.Windows.Rect Area;

        /// <summary>
        /// The body oritentations required
        /// </summary>
        public BodyOrientation.Orientations BodyOrientations;

        /// <summary>
        /// The object use required
        /// </summary>
        public LinkedList<Object.Objects> Objects;

        /// <summary>
        /// Special requirements required
        /// </summary>
        public LinkedList<Requirement> Requirements;

        /// <summary>
        /// The activity name
        /// </summary>
        public string Name;

        /// <summary>
        /// The minimum number of people in the area required
        /// </summary>
        public int MinPeopleCount;

        /// <summary>
        /// The postures required
        /// </summary>
        public LinkedList<Posture> Postures;

        /// <summary>
        /// Is the activity performing
        /// </summary>
        public bool IsActive;

        /// <summary>
        /// Is the activity recording
        /// </summary>
        public bool IsRecording;

        /// <summary>
        /// The current recording row
        /// </summary>
        public int RecordRow;

        /// <summary>
        /// The start time of the activity
        /// </summary>
        public string LastTime;

        /// <summary>
        /// Construction for dynamic area activity
        /// </summary>
        /// <param name="templateName"></param>
        /// <param name="bodyOrientations"></param>
        /// <param name="postures"></param>
        /// <param name="objects"></param>
        /// <param name="requirements"></param>
        /// <param name="name"></param>
        /// <param name="minPeopleCount"></param>
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

        /// <summary>
        /// Construction for static area activity
        /// </summary>
        /// <param name="area"></param>
        /// <param name="bodyOrientations"></param>
        /// <param name="postures"></param>
        /// <param name="objects"></param>
        /// <param name="requirements"></param>
        /// <param name="name"></param>
        /// <param name="minPeopleCount"></param>
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

        /// <summary>
        /// Determine if special requirements satisfied
        /// </summary>
        /// <param name="persons"></param>
        /// <param name="canvas"></param>
        /// <returns></returns>
        public bool IsRequirementsSatisfied(Person[] persons, System.Windows.Controls.Canvas canvas)
        {
            foreach (Requirement req in Requirements)
            {
                if (!req.isSatisfied(Area, persons, canvas)) return false;
            }

            return true;
        }

        /// <summary>
        /// Determine if all required objects used
        /// </summary>
        /// <returns></returns>
        public bool IsObjectUseSatisfied()
        {
            bool isSatisfied = true;

            foreach (Object.Objects obj in this.Objects)
            {
                if (!ObjectDetector.Objects[obj].IsInUse) isSatisfied = false;
            }

            return isSatisfied;
        }

        /// <summary>
        /// Determine if minimum number of people in area satisfied
        /// </summary>
        /// <param name="persons"></param>
        /// <param name="canvas"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Determine if the person is in the activity area
        /// </summary>
        /// <param name="person"></param>
        /// <param name="canvas"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Determine if one posture is performed by person
        /// </summary>
        /// <param name="person"></param>
        /// <returns></returns>
        public bool IsPostureSatisfied(Person person)
        {
            bool isSatisfied = false;

            if (this.Postures.Count == 0) return true;

            foreach (Posture posture_activity in this.Postures)
            {
                foreach(Posture posture_person in person.postures)
                {
                    if (posture_activity.Name.Equals(posture_person.Name)) isSatisfied = true;
                }
            }
            return isSatisfied;
        }

        /// <summary>
        /// Decide activity for each person
        /// </summary>
        /// <param name="activities"></param>
        /// <param name="persons"></param>
        /// <param name="canvas"></param>
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
                    }
                    person.Activities = strBuilder.ToString();
                }
            }
        }

        /// <summary>
        /// Decide status of each defined activity - acted or not
        /// </summary>
        /// <param name="activities"></param>
        /// <param name="persons"></param>
        /// <param name="canvas"></param>
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
