using System.Collections.Generic;

namespace SixD
{
    public class PointCloud
    {
        private List<Point> cloud = new List<Point>();

        public Point[] Points => cloud.ToArray();

        public int NumberOfPoints => cloud.Count;

        public void Add(Point p) => cloud.Add(p);

        public void Add(double x, double y) => cloud.Add(new Point(x, y));

    }
}
