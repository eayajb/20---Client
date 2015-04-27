using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace ServerData
{
    [Serializable]
    public class Person
    {
        public double ID;
        public List<ColorSpacePoint> csPoints;
        public Dictionary<JointType, Point3D> bodyPointsDict;

        public Person(double IDIn, List<ColorSpacePoint> csPointsIn, Dictionary<JointType, Point3D> bodyPointsDictIn)
        {
            this.ID = IDIn;
            this.csPoints = csPointsIn;
            this.bodyPointsDict = bodyPointsDictIn;
        }
    }
}
