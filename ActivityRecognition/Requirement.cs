//------------------------------------------------------------------------------
// <summary>
// Special requirements for activity definition
// </summary>
// <author> Dongyang Yao (dongyang.yao@rutgers.edu) </author>
//------------------------------------------------------------------------------

using System.Windows;
using System.Collections.Generic;

namespace ActivityRecognition
{
    abstract public class Requirement
    {
        /// <summary>
        /// List for special requirements defined
        /// </summary>
        public static LinkedList<Requirement> Requirements;

        /// <summary>
        /// Requirement name
        /// </summary>
        abstract public string Name { get; set; }

        /// <summary>
        /// Determine if the requirement is satisfied
        /// Need to be override
        /// </summary>
        /// <param name="area"></param>
        /// <param name="persons"></param>
        /// <param name="canvas"></param>
        /// <returns></returns>
        abstract public bool isSatisfied(Rect area, Person[] persons, System.Windows.Controls.Canvas canvas);

        /// <summary>
        /// Display requirements
        /// </summary>
        /// <param name="listBox"></param>
        public static void LoadRequirements(System.Windows.Controls.ListBox listBox){
            Requirements = new LinkedList<Requirement>();
            Requirements.AddLast(new Surround());
            listBox.ItemsSource = Requirements;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public static Requirement ConstructChild(string Name)
        {
            if (Name == "Surround") return new Surround();
            else return null;
        }
    }

    public class Surround : Requirement
    {
        /// <summary>
        /// Override name
        /// </summary>
        public override string Name
        {
            get { return "Surround"; }
            set { }
        }

        /// <summary>
        /// Override requirements for satisfaction
        /// </summary>
        /// <param name="area"></param>
        /// <param name="persons"></param>
        /// <param name="canvas"></param>
        /// <returns></returns>
        public override bool isSatisfied(Rect area, Person[] persons, System.Windows.Controls.Canvas canvas)
        {
            Rect top = new Rect(area.Location, new Size(area.Size.Width, area.Size.Height / 2));
            Rect bottom = new Rect(new Point(area.Location.X, area.Location.Y + area.Size.Height / 2), new Size(area.Size.Width, area.Size.Height / 2));
            Rect left = new Rect(area.Location, new Size(area.Size.Width / 2, area.Size.Height));
            Rect right = new Rect(new Point(area.Location.X + area.Size.Width / 2, area.Location.Y), new Size(area.Size.Width / 2, area.Size.Height));
            
            int count = 0;
            if (checkOneSide(top, persons, canvas, BodyOrientation.Orientations.Front)) count++;
            if (checkOneSide(bottom, persons, canvas, BodyOrientation.Orientations.Back)) count++;
            if (checkOneSide(left, persons, canvas, BodyOrientation.Orientations.Right)) count++;
            if (checkOneSide(right, persons, canvas, BodyOrientation.Orientations.Left)) count++;

            return (count >= 2);
        }
        
        /// <summary>
        /// Check if one side of an area containing a person
        /// </summary>
        /// <param name="area"></param>
        /// <param name="persons"></param>
        /// <param name="canvas"></param>
        /// <param name="orientation"></param>
        /// <returns></returns>
        public bool checkOneSide(Rect area, Person[] persons, System.Windows.Controls.Canvas canvas, BodyOrientation.Orientations orientation)
        {
            foreach (Person person in persons)
            {
                if (person.IsTracked && area.Contains(Transformation.ConvertGroundPlaneToCanvas(person.Position, canvas)) && person.Orientation == orientation)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
