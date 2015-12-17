using System;
using System.Xml;
using System.Collections.Generic;
using System.Windows;

namespace ActivityRecognition
{
    public class Settings
    {
        public static void Save(LinkedList<Activity> activites)
        {
            XmlWriter xmlWriter = XmlWriter.Create(@"Settings/Settings.xml");

            xmlWriter.WriteStartDocument();
            xmlWriter.WriteStartElement("Setting");
            String date = DateTime.Now.ToString("M-d-yyyy_HH-mm-ss");
            xmlWriter.WriteAttributeString("Date", date);

            foreach (Activity activity in activites)
            {
                xmlWriter.WriteStartElement("Activity");
                xmlWriter.WriteAttributeString("Name", activity.Name);
                xmlWriter.WriteAttributeString("Body-Orientation", ((int)activity.BodyOrientations).ToString());
                xmlWriter.WriteAttributeString("Minimal-People-Count", activity.MinPeopleCount.ToString());
                xmlWriter.WriteAttributeString("Template-Name", activity.TemplateName);
                xmlWriter.WriteAttributeString("Is-Dynamic-Area", activity.IsDynamicArea.ToString());

                xmlWriter.WriteStartElement("Area");
                xmlWriter.WriteAttributeString("X", activity.Area.X.ToString());
                xmlWriter.WriteAttributeString("Y", activity.Area.Y.ToString());
                xmlWriter.WriteAttributeString("Width", activity.Area.Width.ToString());
                xmlWriter.WriteAttributeString("Height", activity.Area.Height.ToString());
                xmlWriter.WriteEndElement();

                foreach (Posture pos in activity.Postures)
                {
                    xmlWriter.WriteStartElement("Posture");
                    xmlWriter.WriteAttributeString("Name", pos.Name);
                    xmlWriter.WriteEndElement();
                }

                foreach (Object.Objects obj in activity.Objects)
                {
                    xmlWriter.WriteStartElement("Object");
                    xmlWriter.WriteAttributeString("Name", obj.ToString());
                    xmlWriter.WriteAttributeString("ID", ObjectDetector.Objects[obj].ID);
                    xmlWriter.WriteEndElement();
                }

                foreach (Requirement req in activity.Requirements)
                {
                    xmlWriter.WriteStartElement("Requirement");
                    xmlWriter.WriteAttributeString("Name", req.Name.ToString());
                    xmlWriter.WriteEndElement();
                }

                // End activity
                xmlWriter.WriteEndElement();
            }

            // End setting
            xmlWriter.WriteEndElement();
            xmlWriter.WriteEndDocument();
            xmlWriter.Close();
        }

        public static void Load(LinkedList<Activity> activities)
        {
            XmlDocument doc = new XmlDocument();
            if (System.IO.File.Exists(@"Settings/Settings.xml"))
            {
                doc.Load(@"Settings/Settings.xml");

                XmlNodeList nodeListActivity = doc.SelectNodes("//Activity");

                foreach (XmlNode node in nodeListActivity)
                {
                    XmlNode nodeArea = node.SelectSingleNode("Area");
                    Rect rect = new Rect(new Point(double.Parse(nodeArea.Attributes["X"].Value), double.Parse(nodeArea.Attributes["Y"].Value)), new Size(double.Parse(nodeArea.Attributes["Width"].Value), double.Parse(nodeArea.Attributes["Height"].Value)));

                    XmlNodeList nodeListPos = node.SelectNodes("Posture");
                    LinkedList<Posture> postures = new LinkedList<Posture>();
                    foreach (XmlNode pos in nodeListPos)
                    {
                        postures.AddLast(new Posture(pos.Attributes["Name"].Value));
                    }

                    XmlNodeList nodeListObject = node.SelectNodes("Object");
                    LinkedList<Object.Objects> objects = new LinkedList<Object.Objects>();
                    foreach (XmlNode obj in nodeListObject)
                    {
                        objects.AddLast((Object.Objects)Enum.Parse(typeof(Object.Objects), obj.Attributes["Name"].Value));
                    }

                    XmlNodeList nodeListReq = node.SelectNodes("Requirement");
                    LinkedList<Requirement> requirements = new LinkedList<Requirement>();
                    foreach (XmlNode req in nodeListReq)
                    {
                        requirements.AddLast(Requirement.ConstructChild(req.Attributes["Name"].Value));
                    }

                    Activity activity = bool.Parse(node.Attributes["Is-Dynamic-Area"].Value) ?
                        new Activity(node.Attributes["Template-Name"].Value, BodyOrientation.ConvertIntToOrientations(int.Parse(node.Attributes["Body-Orientation"].Value)), postures, objects, requirements, node.Attributes["Name"].Value, int.Parse(node.Attributes["Minimal-People-Count"].Value)) :                       
                        new Activity(rect, BodyOrientation.ConvertIntToOrientations(int.Parse(node.Attributes["Body-Orientation"].Value)), postures, objects, requirements, node.Attributes["Name"].Value, int.Parse(node.Attributes["Minimal-People-Count"].Value));
                    
                    activities.AddLast(activity);
                }
            }
        }
    }
}
