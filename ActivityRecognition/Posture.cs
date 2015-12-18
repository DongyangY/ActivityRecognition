//------------------------------------------------------------------------------
// <summary>
// Description of posture
// </summary>
// <author> Dongyang Yao (dongyang.yao@rutgers.edu) </author>
//------------------------------------------------------------------------------

namespace ActivityRecognition
{
    public class Posture
    {
        /// <summary>
        /// Posture name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="n"></param>
        public Posture(string n)
        {
            Name = n;
        }
    }
}
